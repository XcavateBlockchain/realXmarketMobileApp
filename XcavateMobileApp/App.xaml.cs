using PlutoFramework.Components.Account;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using XcavateMobileApp.Pages;

namespace XcavateMobileApp
{
    public partial class App : Application
    {
        public App()
        {
            var noAccountViewModel = DependencyService.Get<NoAccountPopupViewModel>();
            noAccountViewModel.AfterCreateAccountNavigation = () => Shell.Current.Navigation.PushAsync(
                new UserTypeSelectionPage()
            );

            NavigationModel.NavigateToSettingsPageAsync = () => Shell.Current.Navigation.PushAsync(new SettingsPage());

            NavigationModel.NavigateToUserPageAsync = NavigateToUserPageAsync;

            InitializeComponent();

            DependencyService.Register<ModifyUserProfilePopupViewModel>();

            DependencyService.Register<MainPageViewModel>();

            if (Preferences.Get(PreferencesModel.SHOW_WELCOME_SCREEN, true))
            {
                MainPage = new OnboardingShell();
            }
            else
            {
                // Set Account type if it did not exist
                if (!Preferences.ContainsKey(PreferencesModel.ACCOUNT_TYPE))
                {
                    Preferences.Set(
                        PreferencesModel.ACCOUNT_TYPE,
                        (Preferences.Get(PreferencesModel.USE_PRIVATE_KEY, false) ? AccountType.PrivateKey : AccountType.Mnemonic).ToString()
                    );
                }

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
    }
}