
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Messaging;
using PlutoFramework.Components.UniversalScannerView;
using PlutoFramework.Model;
using XcavateMobileApp.Pages;

namespace XcavateMobileApp.Components
{
    public partial class XcavateMainPageTopNavigationBarViewModel : ObservableObject
    {
        [RelayCommand]
        public async Task OpenSettingsAsync()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new SettingsPage());
        }

        [RelayCommand]
        public async Task OpenMessagingAsync()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new MessagingOverviewPage());
        }

        [RelayCommand]
        public async Task OpenQrScannerAsync()
        {
            if (!AccountModel.CheckRequirements())
            {
                return;
            }

            await Application.Current.MainPage.Navigation.PushAsync(new UniversalScannerPage
            {
                OnScannedMethod = MainPage.OnScanned
            });
        }
    }
}
