using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Constants;
using PlutoFramework.Model;
using PlutoFrameworkCore.Xcavate;
using System.Collections.ObjectModel;
using UniqueryPlus.Nfts;
using XcavatePaseo.NetApi.Generated;
using NftKey = (UniqueryPlus.NftTypeEnum, System.Numerics.BigInteger, System.Numerics.BigInteger);
using PropertyWrapperModel = PlutoFramework.Components.XcavateProperty.XcavatePropertyModel;

namespace XcavateMobileApp.Pages;

public partial class InvestorMainPageViewModel : ObservableObject
{
    private const int PageSize = 20;

    private readonly Dictionary<NftKey, XcavateNftWrapper> ownedPropertiesDict = [];
    private SubstrateClientExt? substrateClient;
    private string ownerAddress = string.Empty;
    private int offset;
    private bool hasMore = true;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoOwnedProperties))]
    private ObservableCollection<XcavateNftWrapper> ownedProperties = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoOwnedProperties))]
    private bool ownedPropertiesLoading;

    public bool NoOwnedProperties => !OwnedPropertiesLoading && OwnedProperties.Count == 0;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            await SubstrateClientModel.ChangeConnectedClientsAsync(
                EndpointsModel.GetSelectedEndpointKeys(),
                CancellationToken.None);

            await LoadOwnedPropertiesForSelectedEndpointAsync(CancellationToken.None);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task LoadOwnedPropertiesForSelectedEndpointAsync(CancellationToken token)
    {
        if (!KeysModel.HasSubstrateKey())
        {
            ResetOwnedProperties();
            OwnedPropertiesLoading = false;
            return;
        }

        if (!SubstrateClientModel.Clients.TryGetValue(EndpointEnum.XcavatePaseo, out var clientTask))
        {
            ResetOwnedProperties();
            OwnedPropertiesLoading = false;
            return;
        }

        var client = await clientTask.ConfigureAwait(false);

        if (client.SubstrateClient is not SubstrateClientExt selectedClient)
        {
            ResetOwnedProperties();
            OwnedPropertiesLoading = false;
            return;
        }

        var selectedOwnerAddress = Substrate.NetApi.Utils.GetAddressFrom(Substrate.NetApi.Utils.GetPublicKeyFrom(KeysModel.GetSubstrateKey()), 0);
        var shouldReload = substrateClient is null ||
                           !ReferenceEquals(substrateClient, selectedClient) ||
                           !string.Equals(ownerAddress, selectedOwnerAddress, StringComparison.Ordinal);

        substrateClient = selectedClient;
        ownerAddress = selectedOwnerAddress;

        if (shouldReload)
        {
            ResetOwnedProperties();
            await LoadMoreOwnedPropertiesAsync(token).ConfigureAwait(false);
        }
        else if (OwnedProperties.Count == 0 && hasMore)
        {
            await LoadMoreOwnedPropertiesAsync(token).ConfigureAwait(false);
        }
    }

    public Task TryLoadMoreOwnedPropertiesAsync(CancellationToken token) => LoadMoreOwnedPropertiesAsync(token);

    private async Task LoadMoreOwnedPropertiesAsync(CancellationToken token)
    {
        if (OwnedPropertiesLoading || !hasMore || substrateClient is null || string.IsNullOrWhiteSpace(ownerAddress))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OwnedPropertiesLoading = true;
        });

        try
        {
            var page = await XcavateIndexerModel.GetOwnedAndBoughtPropertiesAsync(
                    substrateClient,
                    first: PageSize,
                    offset: offset,
                    tokenOwner: ownerAddress)
                .ConfigureAwait(false);

            if (page.Count == 0)
            {
                hasMore = false;
                return;
            }

            offset += page.Count;

            foreach (var property in page)
            {
                var wrappedProperty = await PropertyWrapperModel.ToXcavateNftWrapperAsync(property, token).ConfigureAwait(false);

                if (ownedPropertiesDict.ContainsKey(wrappedProperty.Key))
                {
                    continue;
                }

                ownedPropertiesDict[wrappedProperty.Key] = wrappedProperty;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OwnedProperties.Add(wrappedProperty);
                });
            }

            if (page.Count < PageSize)
            {
                hasMore = false;
            }
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
            });
        }
    }

    private void ResetOwnedProperties()
    {
        offset = 0;
        hasMore = true;
        ownedPropertiesDict.Clear();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OwnedProperties.Clear();
        });
    }
}
