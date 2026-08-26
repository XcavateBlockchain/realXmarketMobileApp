using PlutoFramework.Model;

namespace XcavateMobileApp.Pages;

public partial class NoAccountMainPage : ContentPage
{
    public NoAccountMainPage()
    {

        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        // A visible popup (e.g. the transfer or import popups) is dismissed before the
        // page itself goes back.
        return PopupManager.TryCloseTopPopup() || base.OnBackButtonPressed();
    }
}