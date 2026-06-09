using PlutoFramework.Components.Account;
using PlutoFramework.Components.Mnemonics;
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Components.Password;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Xcavate;
using PlutoFrameworkCore;
using PlutoFrameworkCore.Keys;
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
            OnboardingStage.Finished => NavigateToAppShellAsync(),
            _ => UserTypeSelectionViewModel.NavigateToUserDetailsAsync(),
        };
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
            Navigation = OnPasswordSetAsync,
        });
    }

    public async Task StartAsync(ImportAccountFlowMode flowMode)
    {
        _flowMode = flowMode;

        OnboardingModel.SetOnboardingStage(OnboardingStage.SetupPassword);

        if (flowMode == ImportAccountFlowMode.Create)
        {
            await PlutoConfigurationModel.GenerateNewAccountAsync();
        }

        var nextNavigation = flowMode switch
        {
            ImportAccountFlowMode.Create => _navigationService.NavigateToAsync(new SetupPasswordPage
            {
                Navigation = OnPasswordSetAsync,
            }),
            ImportAccountFlowMode.Import => _navigationService.NavigateToAsync(new EnterMnemonicsPage(
                new EnterMnemonicsViewModel
                {
                    Navigation = OnMnemonicsEnteredAsync,
                })),
            _ => throw new Exception("Unsupported flow mode"),
        };

        await nextNavigation;
    }

    private async Task OnMnemonicsEnteredAsync()
    {
        OnboardingModel.SetOnboardingStage(OnboardingStage.SetupPassword);

        var accountLocked = await KeysDatabase.GetAllKeysOfTypeAsync(KeyTypeEnum.PolkadotJson);

        var importWarningPopupViewModel = DependencyService.Get<ImportWarningPopupViewModel>();

        if (accountLocked.Count() > 0)
        {
            importWarningPopupViewModel.WarningText = "JSON importing unfortunately does not support importing of DID and X25519 Encryption key that are derived from the account. New DID and Encryption key were created. If you wish it import your existing keys, you can do so later in the setting of the app.";
            importWarningPopupViewModel.IsVisible = true;

            await OnPasswordSetAsync();

            return;
        }

        importWarningPopupViewModel.WarningText = "DID and X25519 Encryption key were derived from the entered mnemonics. If you wish it import other keys, you can do so later in the setting of the app.";
        importWarningPopupViewModel.IsVisible = true;

        await _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = OnPasswordSetAsync,
        });
    }

    private async Task OnPasswordSetAsync()
    {
        OnboardingModel.SetOnboardingStage(OnboardingStage.SelectRole);

        await NavigationModel.NavigateAfterAccountCreation.Invoke();
    }
}