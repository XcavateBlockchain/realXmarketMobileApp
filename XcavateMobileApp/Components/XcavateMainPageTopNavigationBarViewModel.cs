
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Account;
using PlutoFramework.Components.Menu;
using PlutoFramework.Components.Messages;
using PlutoFramework.Model;
using PlutoFramework.Model.Xcavate;

namespace XcavateMobileApp.Components
{
    public partial class XcavateMainPageTopNavigationBarViewModel : ObservableObject
    {
        [RelayCommand]
        public async Task OpenMenuAsync()
        {
            if (OnboardingModel.IsOnboardingCompleted())
            {
                await Shell.Current.Navigation.PushAsync(new MainMenuPage());
            }
            else
            {
                var noAccountPopup = DependencyService.Get<NoAccountPopupViewModel>();
                noAccountPopup.IsVisible = true;
            }
        }

        [RelayCommand]
        public async Task OpenMessagingAsync()
        {
            await Shell.Current.Navigation.PushAsync(new MessageWebViewPage());
        }

        [RelayCommand]
        public Task OpenQrScannerAsync() => NavigationModel.NavigateToQrScannerPageAsync();
    }
}
