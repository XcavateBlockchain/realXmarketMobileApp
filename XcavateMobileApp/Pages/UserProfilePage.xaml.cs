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

    /// <summary>
    /// The header grows by the devnet warning strip's height while it is showing, so the
    /// content below it must move down by the same amount.
    /// </summary>
    private void ApplyDevnetBannerOffset()
    {
        contentStack.Margin = new Thickness(0, 30 + SolanaDevnetWarningView.ExtraHeight, 0, 0);
    }
}
