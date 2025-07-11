using PlutoFramework.Components.Account;
using PlutoFramework.Model;
using XcavateMobileApp.Pages;

namespace XcavateMobileApp
{
    public partial class App : Application
    {
        public App()
        {
            var noAccountViewModel = DependencyService.Get<NoAccountPopupViewModel>();
            noAccountViewModel.AfterCreateAccountNavigation = () => Application.Current.MainPage.Navigation.PushAsync(
                new UserTypeSelectionPage()
            );

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
                ;

                Console.WriteLine("Loading app shell");

                MainPage = new XcavateAppShell();
            }
        }
    }
}