using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        private bool loading = false;

        public Func<string, string, string, string, Task>? ContinueFunction { get; set; } = null;

        [RelayCommand]
        public async Task ContinueAsync()
        {
            if (ContinueFunction == null)
            {
                return;
            }

            if (loading)
            {
                return;
            }

            loading = true;

            await ContinueFunction.Invoke(FirstName, LastName, Email, PhoneNumber);

            loading = false;
        }
    }
}
