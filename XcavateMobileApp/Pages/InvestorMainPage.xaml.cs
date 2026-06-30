using PlutoFramework;
using PlutoFramework.Components.NetworkSelect;
using PlutoFramework.Components.Sumsub;
using PlutoFramework.Model;
using PlutoFramework.Model.Sumsub;

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

            // Prioritize the page's own data path so first content appears quickly.
            await viewModel.RefreshAsync(cancellationToken);

            await LoadSumsubStatusAsync(cancellationToken);

            _ = WarmupConnectedClientsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Page disappeared while initialization work was in flight.
        }
    }

    private async Task LoadSumsubStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await SumsubUserModel.GetCurrentStatusAsync(cancellationToken);

            if (status == null)
            {
                return;
            }

            switch (status.StatusType)
            {
                case SumsubStatusType.Approved:
                    SumsubApprovedPopup.Bind(status);
                    SumsubApprovedPopup.IsVisible = true;
                    break;

                case SumsubStatusType.Rejected:
                    SumsubRejectedPopup.Bind(status);
                    SumsubRejectedPopup.IsVisible = true;
                    break;

                case SumsubStatusType.NeedsResubmit:
                    SumsubNeedsResubmitPopup.Bind(status);
                    SumsubNeedsResubmitPopup.IsVisible = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load Sumsub status: {ex}");
        }
    }

    private static async Task WarmupConnectedClientsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SubstrateClientModel.ChangeConnectedClientsAsync(
                EndpointsModel.GetSelectedEndpointKeys(),
                cancellationToken,
                reload: true).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Page was closed before warmup completed.
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
