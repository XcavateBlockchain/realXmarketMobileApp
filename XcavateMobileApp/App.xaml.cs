using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Sumsub;
using PlutoFrameworkCore;
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

            _ = Task.Run(PlutoFramework.MauiAppBuilderExtensions.InitializePlutoFrameworkFull);

            NavigationModel.NavigateToKYC = () => Shell.Current.Navigation.PushAsync(
                new UserTypeSelectionPage()
            );

            NavigationModel.NavigateAfterAccountCreation = () =>
            {
                // TODO: Verify if user has KYC

                return Shell.Current.Navigation.PushAsync(
                    new UserTypeSelectionPage()
                );
            };

            NavigationModel.NavigateToSettingsPageAsync = () => Shell.Current.Navigation.PushAsync(new SettingsPage());

            NavigationModel.NavigateToUserPageAsync = NavigateToUserPageAsync;

            PlutoConfigurationModel.GenerateNewAccountAsync = GenerateNewAccountAsync;

            PlutoConfigurationModel.AfterAccountImportAsync = AfterAccountImportAsync;

            NavigationModel.SetWelcomeShell = () =>
            {
                Application.Current.MainPage = new OnboardingShell();
            };

            DependencyService.Register<ModifyUserProfilePopupViewModel>();

            DependencyService.Register<InverstorMainPageViewModel>();

            if (Preferences.Get(PreferencesModel.SHOW_WELCOME_SCREEN, true) || !KeysModel.HasSubstrateKey())
            {
                Preferences.Set(PreferencesModel.SHOW_WELCOME_SCREEN, true);
                MainPage = new OnboardingShell();
            }
            else
            {
                MainPage = new XcavateAppShell();
            }
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
            Preferences.Clear(PreferencesModel.PUBLIC_KEY);
            await KeysDatabase.DeleteAllAsync();

            await Task.WhenAll(
                KeysModel.GenerateNewAccountAsync(),
                KeysModel.GenerateNewDidAsync(),
                KeysModel.GenerateNewEncryptionX25519KeyAsync()
            );
        }

        public static Task AfterAccountImportAsync()
        {
            return SumsubUserModel.LoadAndSaveUserInfoAsync(CancellationToken.None);
        }
    }
}