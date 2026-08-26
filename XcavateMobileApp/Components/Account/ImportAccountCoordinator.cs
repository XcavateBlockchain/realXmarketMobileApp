using CommunityToolkit.Maui.Alerts;
using PlutoFramework.Components.Account;
using PlutoFramework.Components.Loading;
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Components.Password;
using PlutoFramework.Components.Solana;
using PlutoFramework.Model;
using PlutoFramework.Model.Xcavate;
using XcavateMobileApp.Pages;

namespace XcavateMobileApp.Components.Account;

public class ImportAccountCoordinator : IImportAccountCoordinator
{
    /// <summary>
    /// The flow the user picked on the welcome page, stored next to the onboarding stage.
    /// </summary>
    /// <remarks>
    /// Both flows sit at <see cref="OnboardingStage.SetupPassword"/> until onboarding finishes,
    /// so the stage alone cannot tell them apart on resume. Without this, a user who chose
    /// Import, set a password and was interrupted on the seed-phrase page came back to a
    /// freshly generated wallet while believing their funded one had been imported.
    /// </remarks>
    private const string FLOW_MODE_KEY = "OnboardingImportAccountFlowMode";

    private readonly INavigationService _navigationService;

    public ImportAccountCoordinator()
        : this(new MauiNavigationService())
    {
    }

    public ImportAccountCoordinator(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public static Task ContinueAsync(OnboardingStage stage)
    {
        return stage switch
        {
            OnboardingStage.None => Task.CompletedTask,
            OnboardingStage.SetupPassword => new ImportAccountCoordinator().ContinueSetupPasswordAsync(),
            OnboardingStage.SelectRole => UserTypeSelectionViewModel.NavigateToUserDetailsAsync(),
            OnboardingStage.EnterUserDetails => UserTypeSelectionViewModel.NavigateToUserDetailsAsync(),
            OnboardingStage.Questionaire => UserTypeSelectionViewModel.NavigateToQuestionnaireFromSavedProfileAsync(),
            OnboardingStage.AgreeTerms or OnboardingStage.AgreeAgreement or OnboardingStage.AgreePrivacy =>
                new OnboardingAgreementCoordinator().ContinueFromStageAsync(stage, UserTypeSelectionViewModel.ResumeKycFromSavedProfileAsync),
            OnboardingStage.KYC => UserTypeSelectionViewModel.ResumeKycFromSavedProfileAsync(),
            OnboardingStage.ProfileRegistration => NavigateToProfileRegistrationAsync(),
            OnboardingStage.Finished => NavigateToAppShellAsync(),
            _ => UserTypeSelectionViewModel.NavigateToUserDetailsAsync(),
        };
    }

    public static Task NavigateToProfileRegistrationAsync()
    {
        var viewModel = new ModifyUserProfilePageViewModel
        {
            Title = "Register public profile",
            FirstSetup = true,
        };

        return Shell.Current.Navigation.PushAsync(new ModifyUserProfilePage(viewModel));
    }

    private static Task NavigateToAppShellAsync()
    {
        Application.Current!.MainPage = new XcavateAppShell();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resumes onboarding into the flow the user originally chose. Create re-runs the
    /// password page; Import re-runs the method popup, which is where its password step now
    /// lives - on the import page for a phrase, after the wallet for MWA.
    /// </summary>
    private Task ContinueSetupPasswordAsync()
    {
        if (GetSavedFlowMode() == ImportAccountFlowMode.Import)
        {
            return ShowImportMethodPopupAsync();
        }

        return _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = CreateSolanaAccountAsync,
        });
    }

    /// <summary>
    /// Defaults to <see cref="ImportAccountFlowMode.Create"/> so a stage written before this
    /// preference existed resumes exactly as it used to. A stale value cannot be read: the
    /// only reader runs at <see cref="OnboardingStage.SetupPassword"/>, and only
    /// <see cref="StartAsync"/> sets that stage - after writing the preference.
    /// </summary>
    private static ImportAccountFlowMode GetSavedFlowMode() =>
        (ImportAccountFlowMode)Preferences.Get(FLOW_MODE_KEY, (int)ImportAccountFlowMode.Create);

    public async Task StartAsync(ImportAccountFlowMode flowMode)
    {
        Preferences.Set(FLOW_MODE_KEY, (int)flowMode);

        OnboardingModel.SetOnboardingStage(OnboardingStage.SetupPassword);

        var nextNavigation = flowMode switch
        {
            ImportAccountFlowMode.Create => _navigationService.NavigateToAsync(new SetupPasswordPage
            {
                Navigation = CreateSolanaAccountAsync,
            }),
            ImportAccountFlowMode.Import => ShowImportMethodPopupAsync(),
            _ => throw new Exception("Unsupported flow mode"),
        };

        await nextNavigation;
    }

    /// <summary>
    /// Asks how the account arrives, then continues into whichever flow can ask for a
    /// password at the right moment for it.
    /// </summary>
    /// <remarks>
    /// Saving any key - a phrase or an MWA auth token - reads the stored password to encrypt
    /// it, so a password still has to exist before the save. What differs is where it is
    /// asked for: a phrase import collects it on the same screen as the phrase, and a wallet
    /// connection collects it after the wallet has approved.
    /// </remarks>
    private Task ShowImportMethodPopupAsync()
    {
        var popup = DependencyService.Get<ImportMethodPopupViewModel>();

        popup.SeedPhraseChosen = () => _navigationService.NavigateToAsync(new ImportSolanaWalletPage
        {
            // The page saves the password and the phrase itself, in that order, then hands
            // the phrase back so the Substrate identity comes off the same backup.
            Navigation = (mnemonics) => ContinueAfterAccountCreatedAsync(mnemonics),

            // Onboarding shows the stepper bar, like the other onboarding pages.
            FirstSetup = true,
        });

        popup.MwaChosen = ShowConnectMwaPopupAsync;

        popup.IsVisible = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens over whatever page raised the import-method popup - the page template hosts it,
    /// and <see cref="WelcomePage"/> declares its own - so the wallet is connected before
    /// anything is asked of the user.
    /// </summary>
    private Task ShowConnectMwaPopupAsync()
    {
        var popup = DependencyService.Get<ConnectMwaPopupViewModel>();

        // The popup does not persist the authorization. It is held here, in memory only,
        // until the password that encrypts it exists. Abandoning the flow at the password
        // page therefore saves nothing, and the user reconnects on the next attempt.
        popup.Completed = (key) => _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = async () =>
            {
                try
                {
                    // The save clears any existing Solana key before it can fail, so a silent
                    // failure would leave no account at all.
                    await KeysModel.SaveSolanaMwaKeyAsync(key);
                }
                catch (Exception ex)
                {
                    await Toast.Make($"Could not save your wallet: {ex.Message}").Show();

                    return;
                }

                // No phrase to pass on: the wallet app keeps it. The Substrate identity is
                // generated independently, as the X25519 key already is on this path.
                await ContinueAfterAccountCreatedAsync();
            },
        });

        popup.IsVisible = true;

        return Task.CompletedTask;
    }

    private static async Task CreateSolanaAccountAsync()
    {
        // Generated after the password step, never before: SaveSolanaMnemonicKeyAsync reads
        // the stored password to encrypt the phrase.
        var mnemonics = SolanaMnemonicsModel.GenerateMnemonics();

        await KeysModel.SaveSolanaMnemonicKeyAsync(mnemonics);

        await ContinueAfterAccountCreatedAsync(mnemonics);
    }

    /// <summary>
    /// Hands off from "the wallet exists" to the rest of onboarding: role, user details,
    /// questionnaire, agreements, KYC and finally profile registration, which is what sets
    /// <see cref="OnboardingStage.Finished"/>.
    /// </summary>
    /// <remarks>
    /// The Substrate identity is written first because every step after this one is keyed to
    /// it - the questionnaire submits an SS58 address, the Sumsub applicant is created under
    /// one plus a DID, and roles are granted against one in the XcavatePaseo whitelist
    /// pallet. Writing it before the stage advances also means a user interrupted here
    /// resumes into a role selection that has the keys it needs.
    /// </remarks>
    private static async Task ContinueAfterAccountCreatedAsync(string? mnemonics = null)
    {
        var loadingViewModel = DependencyService.Get<FullPageLoadingViewModel>();

        loadingViewModel.IsVisible = true;
        loadingViewModel.Message = "Setting up your account";

        try
        {
            await KeysModel.EnsureSubstrateIdentityAsync(mnemonics);
        }
        catch (Exception ex)
        {
            // The wallet is saved by this point, so this is recoverable - but silently
            // continuing would strand the user at a questionnaire that throws on a key that
            // is not there.
            await Toast.Make($"Could not finish setting up your account: {ex.Message}").Show();

            return;
        }
        finally
        {
            loadingViewModel.IsVisible = false;
        }

        OnboardingModel.SetOnboardingStage(OnboardingStage.SelectRole);

        await NavigationModel.NavigateAfterAccountCreation.Invoke();
    }
}