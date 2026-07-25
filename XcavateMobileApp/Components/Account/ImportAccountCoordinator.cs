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
    private readonly INavigationService _navigationService;
    private ImportAccountFlowMode _flowMode;

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

    private Task ContinueSetupPasswordAsync()
    {
        return _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = CreateSolanaAccountAsync,
        });
    }

    public async Task StartAsync(ImportAccountFlowMode flowMode)
    {
        _flowMode = flowMode;

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

        // Both branches set the password first, then open a page that saves the key itself.
        // EnterSolanaMnemonicsViewModel.ContinueWithMnemonicsAsync calls
        // SaveSolanaMnemonicKeyAsync before invoking Navigation, and that save reads the
        // stored password — reaching it without one throws, and the view model's catch
        // reports a valid phrase as invalid, dead-ending onboarding.
        popup.SeedPhraseChosen = () => _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = () => _navigationService.NavigateToAsync(new EnterSolanaMnemonicsPage(
                new EnterSolanaMnemonicsViewModel
                {
                    Navigation = (mnemonics) => FinishOnboardingAsync(),
                })),
        });

        popup.MwaChosen = () => _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = () => _navigationService.NavigateToAsync(new ConnectMwaPage(
                new ConnectMwaPageViewModel
                {
                    Navigation = FinishOnboardingAsync,
                })),
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