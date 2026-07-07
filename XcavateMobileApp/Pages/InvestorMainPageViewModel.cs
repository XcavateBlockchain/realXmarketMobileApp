using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.XcavateProperty;
using PlutoFramework.Constants;
using PlutoFramework.Model;
using PlutoFramework.Model.Currency;
using PlutoFrameworkCore.Xcavate;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UniqueryPlus.Nfts;
using XcavatePaseo.NetApi.Generated;
using NftKey = (UniqueryPlus.NftTypeEnum, System.Numerics.BigInteger, System.Numerics.BigInteger);
using PropertyWrapperModel = PlutoFramework.Components.XcavateProperty.XcavatePropertyModel;

namespace XcavateMobileApp.Pages;

public partial class InvestorMainPageViewModel : ObservableObject
{
    private const int PageSize = 20;

    private readonly Dictionary<NftKey, XcavateNftWrapper> ownedPropertiesDict = [];
    private readonly object loadingLock = new();
    private readonly SemaphoreSlim loadMoreSemaphore = new(1, 1);
    private CancellationTokenSource? loadingCts;
    private SubstrateClientExt? substrateClient;
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
    public InvestorMainPageViewModel()
    {
        filterPopupViewModel = DependencyService.Get<PropertyMarketplaceFilterPopupViewModel>();
        filterPopupViewModel.ApplyRequested = ApplyFiltersAsync;
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

    private async Task ApplyFiltersAsync()
    {
        includesTownCity = NormalizeFilterValue(filterPopupViewModel.SelectedTownCity);
        includesPropertyType = NormalizeFilterValue(filterPopupViewModel.SelectedPropertyType);
        includesPropertyName = filterPopupViewModel.SearchText?.Trim() ?? string.Empty;

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
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            var token = ReplaceLoadingToken(externalToken);

            await LoadOwnedPropertiesForSelectedEndpointAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when a newer refresh starts.
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task LoadOwnedPropertiesForSelectedEndpointAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        if (!KeysModel.HasSubstrateKey())
        {
            ResetOwnedProperties();
            OwnedPropertiesLoading = false;
            return;
        }

        var client = await SubstrateClientModel.GetOrAddSubstrateClientAsync(EndpointEnum.XcavatePaseo, token);

        if (client.SubstrateClient is not SubstrateClientExt selectedClient)
        {
            ResetOwnedProperties();
            OwnedPropertiesLoading = false;
            return;
        }

        var selectedOwnerAddress = KeysModel.GetSubstrateKey(0);

        var shouldReload = substrateClient is null ||
                           !ReferenceEquals(substrateClient, selectedClient) ||
                           !string.Equals(ownerAddress, selectedOwnerAddress, StringComparison.Ordinal) ||
                           !IsSameLoadedQuery(includesPropertyName, includesTownCity, includesPropertyType);

        substrateClient = selectedClient;
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
        if (!hasMore || substrateClient is null || string.IsNullOrWhiteSpace(ownerAddress))
        {
            return;
        }

        token.ThrowIfCancellationRequested();

        await loadMoreSemaphore.WaitAsync(token).ConfigureAwait(false);

        try
        {
            if (OwnedPropertiesLoading || !hasMore || substrateClient is null || string.IsNullOrWhiteSpace(ownerAddress))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OwnedPropertiesLoading = true;
            });

            IReadOnlyList<XcavatePaseoNftsPalletNft> page;

            if (OwnedActive)
            {
                page = await XcavateIndexerModel.GetOwnedPropertiesAsync(substrateClient, first: PageSize, offset: offset, tokenOwner: ownerAddress).ConfigureAwait(false);
            }
            else if (BoughtActive)
            {
                page = await XcavateIndexerModel.GetBoughtPropertiesAsync(substrateClient, first: PageSize, offset: offset, tokenOwner: ownerAddress).ConfigureAwait(false);
            }
            else if (filterActive)
            {
                page = await XcavateIndexerModel.GetOwnedAndBoughtPropertiesWithFilterAsync(substrateClient, first: PageSize, offset: offset, tokenOwner: ownerAddress, includesTownCity: includesTownCity, includesPropertyType: includesPropertyType, includesPropertyName: includesPropertyName).ConfigureAwait(false);
            }
            else
            {
                page = await XcavateIndexerModel.GetOwnedAndBoughtPropertiesAsync(substrateClient, first: PageSize, offset: offset, tokenOwner: ownerAddress).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();

            if (page.Count == 0)
            {
                hasMore = false;
                return;
            }

            offset += page.Count;

            var wrappedBatch = await Task.WhenAll(
                page.Select(property => PropertyWrapperModel.ToXcavateNftWrapperAsync(property, token)))
                .ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            var toAdd = new List<XcavateNftWrapper>(wrappedBatch.Length);
            foreach (var wrappedProperty in wrappedBatch)
            {
                if (ownedPropertiesDict.ContainsKey(wrappedProperty.Key))
                {
                    continue;
                }

                ownedPropertiesDict[wrappedProperty.Key] = wrappedProperty;
                toAdd.Add(wrappedProperty);
            }

            if (toAdd.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var wrappedProperty in toAdd)
                    {
                        OwnedProperties.Add(wrappedProperty);
                    }

                    RecalculatePortfolioMetrics();
                });
            }

            if (page.Count < PageSize)
            {
                hasMore = false;
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
