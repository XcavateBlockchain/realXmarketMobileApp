namespace XcavateMobileApp.Pages
{
    public partial class UserTypeSelectionPage : ContentPage
    {
        public UserTypeSelectionPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            Shell.SetNavBarIsVisible(this, false);

            InitializeComponent();

            BindingContext = new UserTypeSelectionViewModel();
        }
    }
}