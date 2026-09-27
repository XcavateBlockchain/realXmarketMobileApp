using PlutoFramework.Components.Keys;
using PlutoFramework.Components.Solana;
using PlutoFramework.Model;
using PlutoFrameworkCore.Solana;

namespace XcavateMobileApp.Pages;

public partial class UserProfilePage : ContentPage
{
    public static UserProfileViewModel? ViewModel;
    public UserProfilePage(UserProfileViewModel viewModel)
	{
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();

        this.BindingContext = viewModel;

        ViewModel = viewModel;

        ApplyDevnetBannerOffset();

        SolanaNetworkModel.ClusterChanged += OnClusterChanged;

        X25519WarningModel.AvailabilityChanged += OnX25519AvailabilityChanged;
	}

    private void OnClusterChanged(object? sender, SolanaCluster cluster)
    {
        // Same orphan guard as SolanaBalanceCellView.OnClusterChanged: a page left behind
        // when its parent was replaced stays subscribed to the static event forever.
        if (Handler is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(ApplyDevnetBannerOffset);
    }

    private void OnX25519AvailabilityChanged(object? sender, EventArgs e)
    {
        // Same orphan guard as OnClusterChanged above.
        if (Handler is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(ApplyDevnetBannerOffset);
    }

    /// <summary>
    /// The header grows by the warning strips' heights while they are showing, so the
    /// content below it must move down by the same amount.
    /// </summary>
    private void ApplyDevnetBannerOffset()
    {
        contentStack.Margin = new Thickness(0, 30 + SolanaDevnetWarningView.ExtraHeight + X25519MissingWarningView.ExtraHeight, 0, 0);
    }
}
