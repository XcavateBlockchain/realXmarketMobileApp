using PlutoFramework.Components.Account;
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Constants;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Xcavate;
using PlutoFrameworkCore;
using XcavateMobileApp.Components.Account;
using XcavateMobileApp.Pages;

namespace XcavateMobileApp
{
    public partial class App : Application
    {
        private bool _isInitialized;

        public App()
        {
            InitializeComponent();

            MainPage = new ContentPage
            {
                Content = new Grid
                {
                    Children =
                    {
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                        },
                    },
                },
            };

            Dispatcher.Dispatch(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            // Let the first frame render before doing heavier setup.
            await Task.Yield();

            // Run full framework initialization on a background thread and await it
            // so registrations are available before we access them below.
            await Task.Run(() => PlutoFramework.MauiAppBuilderExtensions.InitializePlutoFrameworkFull());

            NavigationModel.NavigateToKYC = UserTypeSelectionViewModel.ResumeKycFromSavedProfileAsync;

            NavigationModel.NavigateAfterAccountCreation = () =>
            {
                // TODO: Verify if user has KYC
                return UserTypeSelectionViewModel.NavigateToUserDetailsAsync();
            };

            // Register app-level import account coordinator as a delegate for framework components
            NavigationModel.StartImportAccount = async (ImportAccountFlowMode flowMode) =>
            {
                var coordinator = new ImportAccountCoordinator();
                await coordinator.StartAsync(flowMode);
            };

            NavigationModel.NavigateToSettingsPageAsync = () => Shell.Current.Navigation.PushAsync(new SettingsPage());

            NavigationModel.NavigateToUserPageAsync = NavigateToUserPageAsync;

            PlutoConfigurationModel.GenerateNewAccountAsync = GenerateNewAccountAsync;
            PlutoConfigurationModel.WhitelistedTokens = [
                // XCAV
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Native, 0),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.NativeFrozen, 0),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.NativeReserved, 0),

                // tGBP
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Assets, 10),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsFrozen, 10),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsReserved, 10),

                // USDC
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Assets, 1337),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsFrozen, 1337),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsReserved, 1337),

                // USDT
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Assets, 1984),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsFrozen, 1984),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsReserved, 1984),
            ];

            NavigationModel.SetWelcomeShell = () =>
            {
                Application.Current.MainPage = new OnboardingShell();
            };

            DependencyService.Register<ModifyUserProfilePopupViewModel>();

            DependencyService.Register<InvestorMainPageViewModel>();

            var onboardingPopupViewModel = DependencyService.Get<OnboardingInProgressPopupViewModel>();
            onboardingPopupViewModel.ContinueRequested = ContinueOnboardingAsync;

            MainPage = OnboardingModel.IsOnboardingCompleted() switch
            {
                true when KeysModel.HasSubstrateKey() => new XcavateAppShell(),
                _ => new OnboardingShell(),
            };
        }


        private static Task ContinueOnboardingAsync()
        {
            var stage = OnboardingModel.GetOnboardingStage();

            return ImportAccountCoordinator.ContinueAsync(stage);
        }

        public static async Task NavigateToUserPageAsync()
        {
            var userInfo = await XcavateUserDatabase.GetUserInformationAsync();

            if (userInfo is null)
            {
                return;
            }

            var viewModel = new UserProfileViewModel
            {
                CanEdit = true,
                User = userInfo,
            };

            // Clean temporary files
            string tempProfileBackgroundPath = Path.Combine(FileSystem.Current.AppDataDirectory, "temporaryprofilebackground");

            if (File.Exists(tempProfileBackgroundPath))
            {
                File.Delete(tempProfileBackgroundPath);
            }

            string tempProfilePicturePath = Path.Combine(FileSystem.Current.AppDataDirectory, "temporaryprofilepicture");

            if (File.Exists(tempProfilePicturePath))
            {
                File.Delete(tempProfilePicturePath);
            }

            await Shell.Current.Navigation.PushAsync(new UserProfilePage(viewModel));
        }

        public static async Task GenerateNewAccountAsync()
        {
            await KeysModel.ClearAsync();

            string mnemonics = MnemonicsModel.GenerateMnemonics();
            string didMnemonics = $"{mnemonics}//did";
            string x25519Mnemonics = $"{mnemonics}//x25519";

            await Task.WhenAll(
                KeysModel.SaveSr25519KeyAsync(mnemonics),
                KeysModel.SaveDidKeyAsync(didMnemonics),
                KeysModel.GenerateNewEncryptionX25519KeyAsync()
            );
        }
    }
}