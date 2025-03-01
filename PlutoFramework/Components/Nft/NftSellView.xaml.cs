using PlutoFramework.Components.AssetSelect;
using PlutoFramework.Components.TransactionAnalyzer;
using PlutoFramework.Model;
using Substrate.NetApi.Model.Extrinsics;
using System.Numerics;
using UniqueryPlus.Nfts;

namespace PlutoFramework.Components.Nft;

public partial class NftSellView : ContentView
{
	public NftSellView()
	{
		InitializeComponent();

        var viewModel = DependencyService.Get<NftSellViewModel>();

        BindingContext = viewModel;
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        var viewModel = DependencyService.Get<NftTransferViewModel>();

        viewModel.SetToDefault();
    }
}