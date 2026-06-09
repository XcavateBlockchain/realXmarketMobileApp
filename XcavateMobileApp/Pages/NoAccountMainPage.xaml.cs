namespace XcavateMobileApp.Pages;

public partial class NoAccountMainPage : ContentPage
{
    public NoAccountMainPage()
    {

        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();
    }
}