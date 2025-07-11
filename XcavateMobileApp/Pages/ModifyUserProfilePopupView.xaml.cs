namespace XcavateMobileApp.Pages
{
    public partial class ModifyUserProfilePopupView : ContentView
    {
        public ModifyUserProfilePopupView()
        {
            InitializeComponent();

            BindingContext = DependencyService.Get<ModifyUserProfilePopupViewModel>();
        }
    }
}