using PlutoFramework.Components.Account;
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
    /// Resumes onboarding into the flow the user originally chose. Both flows re-run the
    /// password step, which is what the Create path already did before it resumed.
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
    /// Asks how the account arrives. Both answers end at the password page, because saving
    /// any key — a phrase or an MWA auth token — needs the stored password to encrypt it.
    /// </summary>
    private Task ShowImportMethodPopupAsync()
    {
        var popup = DependencyService.Get<ImportMethodPopupViewModel>();

        // Both branches set the password first, then open a popup that saves the key itself.
        // EnterSolanaMnemonicsPopupViewModel.ContinueWithMnemonicsAsync calls
        // SaveSolanaMnemonicKeyAsync before invoking Completed, and that save reads the
        // stored password — reaching it without one throws, and the view model's catch
        // reports a valid phrase as invalid, dead-ending onboarding.
        popup.SeedPhraseChosen = () => _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = ShowEnterSolanaMnemonicsPopupAsync,
        });

        popup.MwaChosen = () => _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = ShowConnectMwaPopupAsync,
        });

        popup.IsVisible = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Both popups open over <see cref="SetupPasswordPage"/>, which hosts them, rather than
    /// pushing a further page onto a stack the user never gets to walk back through.
    /// </summary>
    private static Task ShowEnterSolanaMnemonicsPopupAsync()
    {
        var popup = DependencyService.Get<EnterSolanaMnemonicsPopupViewModel>();

        popup.Completed = (mnemonics) => FinishOnboardingAsync();

        popup.IsVisible = true;

        return Task.CompletedTask;
    }

    private static Task ShowConnectMwaPopupAsync()
    {
        var popup = DependencyService.Get<ConnectMwaPopupViewModel>();

        popup.Completed = async (key) =>
        {
            await KeysModel.SaveSolanaMwaKeyAsync(key);

            await FinishOnboardingAsync();
        };

        popup.IsVisible = true;

        return Task.CompletedTask;
    }

    private static async Task CreateSolanaAccountAsync()
    {
        // Generated after the password step, never before: SaveSolanaMnemonicKeyAsync reads
        // the stored password to encrypt the phrase.
        var mnemonics = SolanaMnemonicsModel.GenerateMnemonics();

        await KeysModel.SaveSolanaMnemonicKeyAsync(mnemonics);

        await FinishOnboardingAsync();
    }

    /// <summary>
    /// Ends onboarding. This flow no longer reaches profile registration, which is where
    /// <see cref="OnboardingStage.Finished"/> used to be set, so setting it here is what
    /// stops App.xaml.cs routing the user back into onboarding on every launch.
    /// </summary>
    private static Task FinishOnboardingAsync()
    {
        OnboardingModel.SetOnboardingStage(OnboardingStage.Finished);

        return NavigateToAppShellAsync();
    }
}