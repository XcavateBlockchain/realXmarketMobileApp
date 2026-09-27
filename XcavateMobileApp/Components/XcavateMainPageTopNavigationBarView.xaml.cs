using Microsoft.Maui.Layouts;
using PlutoFramework.Components.Keys;
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

        X25519WarningModel.AvailabilityChanged += OnX25519AvailabilityChanged;
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

    private void OnX25519AvailabilityChanged(object? sender, EventArgs e)
    {
        // Same orphan guard as OnClusterChanged above.
        if (Handler is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(UpdateBounds);
    }

    private void UpdateBounds() =>
        AbsoluteLayout.SetLayoutBounds(this, new Rect(0.5, 0, 1, BarHeight + SolanaDevnetWarningView.ExtraHeight + X25519MissingWarningView.ExtraHeight));
}
