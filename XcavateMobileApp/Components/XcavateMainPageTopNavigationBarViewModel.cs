
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Menu;
using PlutoFramework.Components.Notifications;
using PlutoFramework.Model;

namespace XcavateMobileApp.Components
{
    public partial class XcavateMainPageTopNavigationBarViewModel : ObservableObject
    {
        [RelayCommand]
        public async Task OpenMenuAsync()
        {
            await Shell.Current.Navigation.PushAsync(new MainMenuPage());
        }

        [RelayCommand]
        public async Task OpenMessagingAsync()
        {
            await Shell.Current.Navigation.PushAsync(new NotificationsPage());
        }

        [RelayCommand]
        public Task OpenQrScannerAsync() => NavigationModel.NavigateToQrScannerPageAsync();
    }
}
