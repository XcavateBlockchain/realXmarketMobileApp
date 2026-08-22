namespace XcavateMobileApp.Pages
{
    public partial class ModifyUserProfilePopupView : ContentView
    {
        public ModifyUserProfilePopupView()
        {
            InitializeComponent();

            BindingContext = DependencyService.Get<ModifyUserProfilePopupViewModel>();

            // Enter moves to the next field. The phone number is deliberately left without a
            // next view, so Enter there only closes the keyboard - the popup is submitted by
            // its Continue button and nothing else.
            firstNameInput.NextView = lastNameInput;
            lastNameInput.NextView = emailInput;
            emailInput.NextView = phoneInput;
        }
    }
}