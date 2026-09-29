
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
            // The gate (MessengerAccessModel) raises NoAccountPopup or the create/import
            // X25519 popup instead of opening the page when the account is not ready.
            await MessengerAccessModel.TryOpenMessagesAsync();
        }

        [RelayCommand]
        public Task OpenQrScannerAsync() => NavigationModel.NavigateToQrScannerPageAsync();
    }
}
