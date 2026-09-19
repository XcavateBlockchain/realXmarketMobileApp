using Microsoft.Maui.Layouts;
using PlutoFramework.Components.Solana;
using PlutoFramework.Model;
using PlutoFrameworkCore.Solana;

namespace XcavateMobileApp.Components;

public partial class XcavateMainPageTopNavigationBarView : ContentView
{
    private const double BarHeight = 65;

    public XcavateMainPageTopNavigationBarView()
    {
        InitializeComponent();

        BindingContext = new XcavateMainPageTopNavigationBarViewModel();

        UpdateBounds();

        SolanaNetworkModel.ClusterChanged += OnClusterChanged;
    }

    private void OnClusterChanged(object? sender, SolanaCluster cluster)
    {
        // Same orphan guard as SolanaBalanceCellView.OnClusterChanged: a view left behind
        // when its page was replaced stays subscribed to the static event forever.
        if (Handler is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(UpdateBounds);
    }

    private void UpdateBounds() =>
        AbsoluteLayout.SetLayoutBounds(this, new Rect(0.5, 0, 1, BarHeight + SolanaDevnetWarningView.ExtraHeight));
}
