using PlutoFramework.Components.Onboarding;
using PlutoFramework.Model;
using PlutoFramework.Model.Xcavate;

namespace XcavateMobileApp.Pages;

public partial class WelcomePage : ContentPage
{
    private readonly WelcomePageViewModel _viewModel;

    public WelcomePage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();

        _viewModel = new WelcomePageViewModel();
        BindingContext = _viewModel;

        Loaded += OnLoaded;

        if (OnboardingModel.IsOnboardingInProgress())
        {
            var onboardingInProgressPopupViewModel = DependencyService.Get<OnboardingInProgressPopupViewModel>();

            onboardingInProgressPopupViewModel.IsVisible = true;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        // A visible popup (e.g. the onboarding/import-method/connect-wallet stack) is
        // dismissed before the page itself goes back.
        return PopupManager.TryCloseTopPopup() || base.OnBackButtonPressed();
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;

        // Let the first frame render before loading large images.
        await Task.Yield();

        _viewModel.LoadSplashes();
    }
}