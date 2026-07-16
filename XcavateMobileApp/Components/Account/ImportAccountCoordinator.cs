using PlutoFramework.Components.Account;
using PlutoFramework.Components.Mnemonics;
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Components.Password;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Xcavate;
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
            Navigation = () =>
            {
                var mnemonics = MnemonicsModel.GenerateMnemonics();

                return OnPasswordSetAsync(mnemonics);
            },
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
                Navigation = () =>
                {
                    var mnemonics = MnemonicsModel.GenerateMnemonics();

                    return OnPasswordSetAsync(mnemonics);
                },
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

    private async Task OnMnemonicsEnteredAsync(string mnemonics)
    {
        var accountLocked = await KeysDatabase.GetAllKeysOfTypeAsync(KeyTypeEnum.PolkadotJson);

        var importWarningPopupViewModel = DependencyService.Get<ImportWarningPopupViewModel>();

        if (accountLocked.Count() > 0)
        {
            importWarningPopupViewModel.WarningText = "JSON importing unfortunately does not support importing of DID and X25519 Encryption key that are derived from the account. New DID and Encryption key were created. If you wish to import your existing keys, you can do so later in the setting of the app.";
            importWarningPopupViewModel.IsVisible = true;

            await OnJsonImportedAsync(mnemonics);

            return;
        }

        importWarningPopupViewModel.WarningText = "DID and X25519 Encryption key were derived from the entered mnemonics. If you wish it import other keys, you can do so later in the setting of the app.";
        importWarningPopupViewModel.IsVisible = true;

        await _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = () => OnPasswordSetAsync(mnemonics),
        });


    }

    private async Task OnJsonImportedAsync(string mnemonics)
    {

        string didMnemonics = $"{mnemonics}//did";
        string x25519Mnemonics = $"{mnemonics}//x25519";
        await KeysModel.SaveDidKeyAsync(didMnemonics);
        await KeysModel.SaveEncryptionX25519KeyAsync(x25519Mnemonics);

        OnboardingModel.SetOnboardingStage(OnboardingStage.SelectRole);

        await NavigationModel.NavigateAfterAccountCreation.Invoke();
    }

    private async Task OnPasswordSetAsync(string mnemonics)
    {
        string didMnemonics = $"{mnemonics}//did";
        string x25519Mnemonics = $"{mnemonics}//x25519";

        await KeysModel.SaveSr25519KeyAsync(mnemonics);
        await KeysModel.SaveDidKeyAsync(didMnemonics);
        await KeysModel.SaveEncryptionX25519KeyAsync(x25519Mnemonics);


        OnboardingModel.SetOnboardingStage(OnboardingStage.SelectRole);

        await NavigationModel.NavigateAfterAccountCreation.Invoke();
    }
}