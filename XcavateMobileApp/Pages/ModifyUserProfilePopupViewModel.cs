using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Xcavate;

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
                
            var newUserInfo = new XcavateUser
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                Role = UserRole,
                DeveloperStats = null,
                AccountCreatedAt = DateTime.Now,
                ProfilePicture = XcavateFileModel.GetSavedProfilePicture(),
                ProfileBackground = XcavateFileModel.GetSavedProfileBackground(),
            };

            await XcavateUserDatabase.SaveUserInformationAsync(newUserInfo);

            await ContinueFunction.Invoke(FirstName, LastName, Email, PhoneNumber);

            loading = false;
        }
    }
}
