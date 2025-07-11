using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.NetworkSelect;
using PlutoFramework.Model;

namespace XcavateMobileApp.Pages
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isRefreshing = false;

        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsRefreshing = true;

            _ = SubstrateClientModel.ChangeConnectedClientsAsync(EndpointsModel.GetSelectedEndpointKeys(), CancellationToken.None);

            await Task.Delay(5000);

            IsRefreshing = false;
        }
    }
}
