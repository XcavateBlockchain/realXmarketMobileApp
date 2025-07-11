namespace XcavateMobileApp.Pages;

public partial class ModifyUserProfilePage : ContentPage
{
	public ModifyUserProfilePage(ModifyUserProfilePageViewModel viewModel)
	{
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();

		BindingContext = viewModel;
	}
}