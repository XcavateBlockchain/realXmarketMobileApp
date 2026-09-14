using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework;
using PlutoFramework.Components.XcavateProperty;
using PlutoFramework.Model;
using PlutoFramework.Model.Currency;
using PlutoFramework.Model.Xcavate;
using PlutoFrameworkCore.Xcavate;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UniqueryPlus.Nfts;
using NftKey = (UniqueryPlus.NftTypeEnum, System.Numerics.BigInteger, System.Numerics.BigInteger);
using XcavatePropertyModel = PlutoFramework.Components.XcavateProperty.XcavatePropertyModel;

namespace XcavateMobileApp.Pages;

public partial class InvestorMainPageViewModel : ObservableObject
{
    private const int PageSize = 20;

    private readonly Dictionary<NftKey, XcavateNftWrapper> ownedPropertiesDict = [];
    private readonly object loadingLock = new();
    private readonly SemaphoreSlim loadMoreSemaphore = new(1, 1);
    private CancellationTokenSource? loadingCts;
    private bool clientLoaded;
    private string ownerAddress = string.Empty;
    private int offset;
    private bool hasMore = true;
    private bool isBackgroundHydrationRunning;
    private readonly PropertyMarketplaceFilterPopupViewModel filterPopupViewModel;
    private string includesTownCity = string.Empty;
    private string includesPropertyType = string.Empty;
    private string includesPropertyName = string.Empty;
    private bool filterActive = false;
    private string lastLoadedTownCity = string.Empty;
    private string lastLoadedPropertyType = string.Empty;
    private string lastLoadedPropertyName = string.Empty;
    private bool hasLoadedQuery;
    private readonly object searchDebounceLock = new();
    private CancellationTokenSource? searchDebounceCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalTokensText))]
    private uint totalTokens;

    public string TotalTokensText => TotalTokens.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalInvestedText))]
    private double totalInvested;
    public string TotalInvestedText => ((double)TotalInvested).ToCurrencyString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoiText))]
    private double roi;
    public string RoiText => $"{Roi:P1}";

    [ObservableProperty]
    private string searchText = string.Empty;

    public InvestorMainPageViewModel()
    {
        filterPopupViewModel = DependencyService.Get<PropertyMarketplaceFilterPopupViewModel>();
        filterPopupViewModel.ApplyRequested = ApplyFiltersAsync;
        filterPopupViewModel.CancelRequested = async () => await HandleFilterCancelAsync().ConfigureAwait(false);
        OwnedProperties.CollectionChanged += OnOwnedPropertiesCollectionChanged;
    }

    partial void OnOwnedActiveChanged(bool value)
    {
        OwnedButtonState = value ? PlutoFramework.Components.Buttons.ButtonStateEnum.Enabled : PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;
        if (value)
        {
            filterActive = false;
            hasLoadedQuery = false;
            filterPopupViewModel.SetToDefault();
            BoughtActive = false;
            BoughtButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;
            _ = RestartOwnedPropertiesLoadAsync(CancellationToken.None);
        }
    }

    partial void OnBoughtActiveChanged(bool value)
    {
        BoughtButtonState = value ? PlutoFramework.Components.Buttons.ButtonStateEnum.Enabled : PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;
        if (value)
        {
            filterActive = false;
            hasLoadedQuery = false;
            filterPopupViewModel.SetToDefault();
            OwnedActive = false;
            OwnedButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;
            _ = RestartOwnedPropertiesLoadAsync(CancellationToken.None);
        }
    }

    [RelayCommand]
    private void ToggleOwned()
    {
        OwnedActive = !OwnedActive;
    }

    [RelayCommand]
    private void ToggleBought()
    {
        BoughtActive = !BoughtActive;
    }

    [RelayCommand]
    private void OpenFilter()
    {
        OwnedActive = false;
        BoughtActive = false;
        OwnedButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;
        BoughtButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;

        filterPopupViewModel.IsVisible = true;
    }

    private async Task HandleFilterCancelAsync()
    {
        // When filter is cancelled, reset to default state and reload
        filterActive = false;
        hasLoadedQuery = false;
        filterPopupViewModel.SetToDefault();
        OwnedActive = false;
        BoughtActive = false;
        OwnedButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;
        BoughtButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;

        await RestartOwnedPropertiesLoadAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task ApplyFiltersAsync()
    {
        includesTownCity = NormalizeFilterValue(filterPopupViewModel.SelectedTownCity);
        includesPropertyType = NormalizeFilterValue(filterPopupViewModel.SelectedPropertyType);
        includesPropertyName = filterPopupViewModel.SearchText?.Trim() ?? string.Empty;
        SearchText = includesPropertyName;

        OwnedActive = false;
        BoughtActive = false;
        OwnedButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;
        BoughtButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;

        filterActive = true;

        if (!IsSameLoadedQuery(includesPropertyName, includesTownCity, includesPropertyType))
        {
            await RestartOwnedPropertiesLoadAsync(CancellationToken.None).ConfigureAwait(false);
            RememberLoadedQuery();
        }

        filterPopupViewModel.IsVisible = false;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CancelPendingDebouncedSearch();
        await ExecuteSearchAsync(SearchText).ConfigureAwait(false);
    }

    partial void OnSearchTextChanged(string value)
    {
        filterPopupViewModel.SearchText = value ?? string.Empty;
        _ = DebouncedSearchAsync(value ?? string.Empty);
    }

    private async Task DebouncedSearchAsync(string currentSearchText)
    {
        var token = CreateDebounceToken();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
            await ExecuteSearchAsync(currentSearchText, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user keeps typing.
        }
    }

    private async Task ExecuteSearchAsync(string currentSearchText, CancellationToken token = default)
    {
        var normalizedSearchText = currentSearchText?.Trim() ?? string.Empty;

        if (IsSameLoadedQuery(normalizedSearchText, includesTownCity, includesPropertyType))
        {
            return;
        }

        includesPropertyName = normalizedSearchText;
        filterPopupViewModel.SearchText = normalizedSearchText;

        await RestartOwnedPropertiesLoadAsync(token).ConfigureAwait(false);
        RememberLoadedQuery();
    }

    private CancellationToken CreateDebounceToken()
    {
        CancellationTokenSource newDebounceCts;

        lock (searchDebounceLock)
        {
            searchDebounceCts?.Cancel();
            searchDebounceCts?.Dispose();

            newDebounceCts = new CancellationTokenSource();
            searchDebounceCts = newDebounceCts;
        }

        return newDebounceCts.Token;
    }

    private void CancelPendingDebouncedSearch()
    {
        lock (searchDebounceLock)
        {
            searchDebounceCts?.Cancel();
            searchDebounceCts?.Dispose();
            searchDebounceCts = null;
        }
    }

    private async Task RestartOwnedPropertiesLoadAsync(CancellationToken externalToken)
    {
        try
        {
            var token = ReplaceLoadingToken(externalToken);
            await LoadOwnedPropertiesForSelectedEndpointAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when user refreshes or leaves the page.
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static string NormalizeFilterValue(string value)
    {
        return string.Equals(value, "All", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
    }

    [ObservableProperty]
    private bool isRefreshing;

    private bool isRefreshInProgress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoOwnedProperties))]
    private ObservableCollection<XcavateNftWrapper> ownedProperties = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoOwnedProperties))]
    private bool ownedPropertiesLoading;

    public bool NoOwnedProperties => !OwnedPropertiesLoading && OwnedProperties.Count == 0;

    [ObservableProperty]
    private bool ownedActive = false;

    [ObservableProperty]
    private bool boughtActive = false;

    [ObservableProperty]
    private PlutoFramework.Components.Buttons.ButtonStateEnum ownedButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;

    [ObservableProperty]
    private PlutoFramework.Components.Buttons.ButtonStateEnum boughtButtonState = PlutoFramework.Components.Buttons.ButtonStateEnum.GrayEnabled;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await RefreshAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task RefreshAsync(CancellationToken externalToken)
    {
        // RefreshView sets IsRefreshing to true (TwoWay binding) before executing
        // RefreshCommand, so IsRefreshing cannot be used as a re-entrancy guard here.
        if (isRefreshInProgress)
        {
            return;
        }

        isRefreshInProgress = true;
        IsRefreshing = true;

        try
        {
            var token = ReplaceLoadingToken(externalToken);

            // A user-initiated refresh should always re-fetch, even for the same query.
            hasLoadedQuery = false;

            await LoadOwnedPropertiesForSelectedEndpointAsync(token).ConfigureAwait(false);

            await MainPageLayoutUpdater.ViewLocalLoadAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when a newer refresh starts.
        }
        finally
        {
            IsRefreshing = false;
            isRefreshInProgress = false;
        }
    }

    public async Task LoadOwnedPropertiesForSelectedEndpointAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        // The investor's positions live in the Xcavate Solana marketplace, indexed at
        // indexer-devnet.xcavate.io - the wallet that signs there is the Solana key, not
        // the Substrate one.
        var selectedOwnerAddress = KeysModel.GetSolanaAddress();

        if (string.IsNullOrWhiteSpace(selectedOwnerAddress))
        {
            ResetOwnedProperties();
            OwnedPropertiesLoading = false;
            return;
        }

        var shouldReload = !clientLoaded ||
                           !string.Equals(ownerAddress, selectedOwnerAddress, StringComparison.Ordinal) ||
                           !IsSameLoadedQuery(includesPropertyName, includesTownCity, includesPropertyType);

        clientLoaded = true;
        ownerAddress = selectedOwnerAddress;

        if (shouldReload)
        {
            ResetOwnedProperties();
            RememberLoadedQuery();
            await LoadMoreOwnedPropertiesAsync(token).ConfigureAwait(false);
            _ = HydrateRemainingOwnedPropertiesAsync(token);
        }
        else if (OwnedProperties.Count == 0 && hasMore)
        {
            await LoadMoreOwnedPropertiesAsync(token).ConfigureAwait(false);
            _ = HydrateRemainingOwnedPropertiesAsync(token);
        }
    }

    public Task TryLoadMoreOwnedPropertiesAsync(CancellationToken token) => LoadMoreOwnedPropertiesAsync(token);

    public void CancelOngoingLoading()
    {
        CancelPendingDebouncedSearch();

        lock (loadingLock)
        {
            loadingCts?.Cancel();
        }
    }

    private CancellationToken ReplaceLoadingToken(CancellationToken externalToken)
    {
        CancellationTokenSource? previousCts;
        CancellationTokenSource newCts;

        lock (loadingLock)
        {
            previousCts = loadingCts;
            newCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            loadingCts = newCts;
        }

        previousCts?.Cancel();
        previousCts?.Dispose();

        return newCts.Token;
    }

    private async Task HydrateRemainingOwnedPropertiesAsync(CancellationToken token)
    {
        if (isBackgroundHydrationRunning)
        {
            return;
        }

        isBackgroundHydrationRunning = true;

        try
        {
            while (hasMore)
            {
                token.ThrowIfCancellationRequested();
                await LoadMoreOwnedPropertiesAsync(token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when refresh starts again or page disappears.
        }
        finally
        {
            isBackgroundHydrationRunning = false;
        }
    }

    private async Task LoadMoreOwnedPropertiesAsync(CancellationToken token)
    {
        if (!hasMore || !clientLoaded || string.IsNullOrWhiteSpace(ownerAddress))
        {
            return;
        }

        token.ThrowIfCancellationRequested();

        await loadMoreSemaphore.WaitAsync(token).ConfigureAwait(false);

        try
        {
            if (OwnedPropertiesLoading || !hasMore || !clientLoaded || string.IsNullOrWhiteSpace(ownerAddress))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OwnedPropertiesLoading = true;
            });

            // The devnet indexer answers the town/type/name filters client-side, and the
            // Purchased/Reserved toggles select a subset of the investor's positions: a raw
            // page can filter down to nothing while deeper pages still match, and a scroll
            // that appends nothing never re-fires the page's load-more - so keep fetching
            // raw pages until something passes or the position feed ends.
            var newItems = new List<XcavateNftWrapper>();

            while (newItems.Count == 0 && hasMore)
            {
                var page = await XcavateMarketplaceIndexerModel.GetInvestorPropertiesAsync(
                        ownerAddress,
                        first: PageSize,
                        offset: offset,
                        token: token)
                    .ConfigureAwait(false);

                token.ThrowIfCancellationRequested();

                if (page.Count == 0)
                {
                    hasMore = false;
                    break;
                }

                offset += page.Count;

                if (page.Count < PageSize)
                {
                    hasMore = false;
                }

                var matchingProperties = page
                    .Where(property => MatchesMode(property) && XcavateMarketplaceIndexerModel.MatchesFilter(
                        property.Listing,
                        includesTownCity,
                        includesPropertyType,
                        includesPropertyName))
                    .ToList();

                var wrappedBatch = await Task.WhenAll(
                        matchingProperties.Select(property => XcavatePropertyModel.ToXcavateNftWrapperAsync(property.Listing, token)))
                    .ConfigureAwait(false);

                token.ThrowIfCancellationRequested();

                foreach (var wrappedProperty in wrappedBatch)
                {
                    if (ownedPropertiesDict.ContainsKey(wrappedProperty.Key))
                    {
                        continue;
                    }

                    ownedPropertiesDict[wrappedProperty.Key] = wrappedProperty;
                    newItems.Add(wrappedProperty);
                }
            }

            if (newItems.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    foreach (var wrappedProperty in newItems)
                    {
                        OwnedProperties.Add(wrappedProperty);
                    }

                    RecalculatePortfolioMetrics();
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when user refreshes or leaves the page.
        }
        catch (Exception ex)
        {
            Console.WriteLine("Owned indexed properties list error:");
            Console.WriteLine(ex);
            hasMore = false;
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OwnedPropertiesLoading = false;
                OnPropertyChanged(nameof(NoOwnedProperties));
            });

            loadMoreSemaphore.Release();
        }
    }

    /// <summary>
    /// The mode toggle applied to a raw position before anything is shown: "Purchased"
    /// keeps the properties with bought shares, "Reserved" the ones with reserved shares,
    /// and no toggle keeps everything the investor holds.
    /// </summary>
    private bool MatchesMode(XcavateSolanaInvestorProperty property)
    {
        if (OwnedActive)
        {
            return property.BoughtShares > 0;
        }

        if (BoughtActive)
        {
            return property.ReservedShares > 0;
        }

        return true;
    }

    private void ResetOwnedProperties()
    {
        offset = 0;
        hasMore = true;
        isBackgroundHydrationRunning = false;
        ownedPropertiesDict.Clear();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OwnedProperties.Clear();
            RecalculatePortfolioMetrics();
            OnPropertyChanged(nameof(NoOwnedProperties));
        });
    }

    private void OnOwnedPropertiesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(NoOwnedProperties));
    }

    private void RecalculatePortfolioMetrics()
    {
        TotalTokens = (uint)OwnedProperties.Sum(x => x.TokensBought + x.TokensOwned);
        var totalInvested = OwnedProperties.Sum(x => (long)((x.TokensBought + x.TokensOwned) * ((INftXcavateMetadata)x.NftBase).XcavateMetadata?.Financials.PricePerToken ?? 0));
        TotalInvested = totalInvested;
        decimal totalIncome = OwnedProperties.Sum(x =>
        {
            decimal rentalIncome = ((INftXcavateMetadata)x.NftBase).XcavateMetadata?.Financials.EstimatedRentalIncome ?? 0;
            decimal tokens = ((INftXcavateMetadata)x.NftBase).XcavateMetadata?.Financials.NumberOfTokens ?? 0;

            if (tokens == 0)
            {
                return 0;
            }

            return (x.TokensBought + x.TokensOwned) * (rentalIncome / tokens);
        });
        Roi = totalInvested > 0 ? ((double)totalIncome / totalInvested) * 12 : 0;
    }

    private bool IsSameLoadedQuery(string searchText, string townCity, string propertyType)
    {
        return hasLoadedQuery
            && string.Equals(lastLoadedPropertyName, searchText ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(lastLoadedTownCity, townCity ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(lastLoadedPropertyType, propertyType ?? string.Empty, StringComparison.Ordinal);
    }

    private void RememberLoadedQuery()
    {
        lastLoadedPropertyName = includesPropertyName;
        lastLoadedTownCity = includesTownCity;
        lastLoadedPropertyType = includesPropertyType;
        hasLoadedQuery = true;
    }

}
