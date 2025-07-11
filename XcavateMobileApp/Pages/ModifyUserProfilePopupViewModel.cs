using CommunityToolkit.Mvvm.ComponentModel;
using PlutoFramework.Model;

namespace XcavateMobileApp.Pages
{
    public partial class ModifyUserProfilePopupViewModel : ModifyUserProfilePageViewModel, IPopup, ISetToDefault
    {
        [ObservableProperty]
        private bool isVisible = false;

        public void SetToDefault()
        {
            IsVisible = false;

            FirstName = "";
            LastName = "";
            Email = "";
            PhoneNumber = "";
        }
    }
}
