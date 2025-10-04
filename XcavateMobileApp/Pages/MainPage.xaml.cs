using PlutoFramework;
using PlutoFramework.Components.MessagePopup;
using PlutoFramework.Components.NetworkSelect;
using PlutoFramework.Components.TransferView;
using PlutoFramework.Components.UniversalScannerView;
using PlutoFramework.Components.Vault;
using PlutoFramework.Model;
using Plutonication;

namespace XcavateMobileApp.Pages;

public partial class MainPage : ContentPage, IPlutoFrameworkMainPage
{
    public IList<IView> Views => StackLayout?.Children ?? [];
    public static VerticalStackLayout? StackLayout { get; set; }
    public static MultiNetworkSelectView? NetworksView { get; set; }
    public MainPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();

        var viewModel = DependencyService.Get<MainPageViewModel>();
        BindingContext = viewModel;

        networksView.IsVisible = Preferences.Get(PreferencesModel.SETTINGS_DISPLAY_NETWORKS, (bool)Application.Current.Resources["DisplayNetworks"]);
        NetworksView = networksView;

        StackLayout = stackLayout;

        MainPageLayoutUpdater.MainPage = this;

        SetupLayout();

        _ = KeysModel.TempConvertMainKeysIntoDbAsync();
    }

    public static void SetupLayout()
    {
        if (StackLayout is null)
        {
            return;
        }

        if (StackLayout.Children.Count() != 0)
        {
            StackLayout.Children.Clear();
        }

        List<IView> views = [];
        try
        {
            views = CustomLayoutModel.ParsePlutoLayout(CustomLayoutModel.DEFAULT_PLUTO_LAYOUT);
        }
        catch
        {
            views = CustomLayoutModel.ParsePlutoLayout(CustomLayoutModel.DEFAULT_PLUTO_LAYOUT);
        }

        foreach (IView view in views)
        {
            ((ContentView)view).Parent = null;
            StackLayout.Children.Add(view);
        }

        // Load
        try
        {
            _ = SubstrateClientModel.ChangeConnectedClientsAsync(EndpointsModel.GetSelectedEndpointKeys(), CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    async void OnQRClicked(System.Object sender, System.EventArgs e)
    {
        await NavigationModel.NavigateToQrScannerPageAsync();
    }

    async void OnSettingsClicked(System.Object sender, System.EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }
}