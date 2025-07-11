using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XcavateMobileApp.Pages
{
    public partial class UserTypeSelectionViewModel : ObservableObject
    {
        [RelayCommand]
        public void SelectDeveloper()
        {
            var modifyUserProfileViewModel = DependencyService.Get<ModifyUserProfilePopupViewModel>();
            modifyUserProfileViewModel.IsVisible = true;
        }
    }
}
