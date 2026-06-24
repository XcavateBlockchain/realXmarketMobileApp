using PlutoFramework;
using PlutoFramework.Components.NetworkSelect;
using PlutoFramework.Model;

namespace XcavateMobileApp.Pages;

public partial class InvestorMainPage : ContentPage, IPlutoFrameworkMainPage
{
    public IList<IView> Views => [balanceCellView];
    public static MultiNetworkSelectView? NetworksView { get; set; }

    private readonly InvestorMainPageViewModel viewModel;
    private CancellationTokenSource? _initializationCts;
    private bool _isInitialized;

    public InvestorMainPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();

        viewModel = DependencyService.Get<InvestorMainPageViewModel>();
        BindingContext = viewModel;

        NetworksView = networksView;

        MainPageLayoutUpdater.MainPage = this;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        _initializationCts?.Cancel();
        _initializationCts?.Dispose();
        _initializationCts = new CancellationTokenSource();
        var cancellationToken = _initializationCts.Token;

        // Let the page complete the first layout pass before loading remote data.
        try
        {
            await Task.Delay(100, cancellationToken);

            await Task.WhenAll(
                viewModel.RefreshAsync(cancellationToken),
                SubstrateClientModel.ChangeConnectedClientsAsync(
                    EndpointsModel.GetSelectedEndpointKeys(),
                    cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Page disappeared while initialization work was in flight.
        }
    }

    protected override void OnDisappearing()
    {
        _initializationCts?.Cancel();
        _initializationCts?.Dispose();
        _initializationCts = null;

        viewModel.CancelOngoingLoading();
        base.OnDisappearing();
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
