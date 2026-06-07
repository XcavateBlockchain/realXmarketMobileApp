using PlutoFramework;
using PlutoFramework.Components.Faucet;
using PlutoFramework.Components.NetworkSelect;
using PlutoFramework.Components.Table;
using PlutoFramework.Components.XcavateProperty.Cells;
using PlutoFramework.Constants;
using PlutoFramework.Model;

namespace XcavateMobileApp.Pages;

public partial class InvestorMainPage : ContentPage, IPlutoFrameworkMainPage
{
    public IList<IView> Views => stackLayout?.Children ?? [];
    public static MultiNetworkSelectView? NetworksView { get; set; }

    private readonly InvestorMainPageViewModel viewModel;
    private bool _isInitialized;

    public InvestorMainPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();

        InsertStaticHeaderWidgets();

        viewModel = DependencyService.Get<InvestorMainPageViewModel>();
        BindingContext = viewModel;

        networksView.IsVisible = Preferences.Get(
            PreferencesModel.SETTINGS_DISPLAY_NETWORKS,
            (bool)Application.Current.Resources["DisplayNetworks"]);
        NetworksView = networksView;

        MainPageLayoutUpdater.MainPage = this;

        Loaded += OnLoaded;
    }

    private void InsertStaticHeaderWidgets()
    {
        stackLayout.Children.Insert(2, new FaucetButtonView(EndpointEnum.XcavatePaseo));

        stackLayout.Children.Insert(3, new TwoCellTableView(
            new PropertyTokensBoughtCellView(),
            new TotalInvestedCellView()));

        stackLayout.Children.Insert(4, new TwoCellTableView(
            new ROICellView(),
            new BalanceCellView()));
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        // Let the page complete the first layout pass before loading remote data.
        await Task.Delay(100);

        await SetupLayoutAsync();
    }

    private async Task SetupLayoutAsync()
    {
        try
        {
            await SubstrateClientModel.ChangeConnectedClientsAsync(
                EndpointsModel.GetSelectedEndpointKeys(),
                CancellationToken.None);

            await viewModel.LoadOwnedPropertiesForSelectedEndpointAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void OnMainScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (sender is not ScrollView scrollView)
        {
            return;
        }

        var remainingHeight = scrollView.ContentSize.Height - (scrollView.ScrollY + scrollView.Height);

        if (remainingHeight > 280)
        {
            return;
        }

        await viewModel.TryLoadMoreOwnedPropertiesAsync(CancellationToken.None);
    }
}
