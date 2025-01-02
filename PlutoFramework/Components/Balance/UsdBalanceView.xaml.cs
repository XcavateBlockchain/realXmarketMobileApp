﻿
using PlutoFramework.Model;
using PlutoFramework.Model.HydraDX;
using Substrate.NetApi;

namespace PlutoFramework.Components.Balance;

public partial class UsdBalanceView : ContentView, ISubstrateClientLoadableAsyncView, ISetEmptyView
{
	public UsdBalanceView()
	{
		InitializeComponent();

        BindingContext = new UsdBalanceViewModel();
    }

    public async Task LoadAsync(PlutoFrameworkSubstrateClient client, CancellationToken token)
    {
        if (client is not null && client.Endpoint.Key == Constants.EndpointEnum.Hydration && client.SubstrateClient.IsConnected && Sdk.AssetsById.Count() == 0)
        {
            await Sdk.GetAssetsAsync((Hydration.NetApi.Generated.SubstrateClientExt)client.SubstrateClient, token);
        }

        await Model.AssetsModel.GetBalanceAsync(client, KeysModel.GetSubstrateKey(), token, false);

        var viewModel = (UsdBalanceViewModel)BindingContext;
        viewModel.UpdateBalances();
    }

    public void SetEmpty()
    {
        var viewModel = (UsdBalanceViewModel)BindingContext;
        viewModel.ReloadIsVisible = true;
    }
}
