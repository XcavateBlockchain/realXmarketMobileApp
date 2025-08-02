using PlutoFramework.Templates.PageTemplate;

namespace XcavateMobileApp.Pages
{
    public partial class UserTypeSelectionPage : PageTemplate
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