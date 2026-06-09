using PlutoFramework.Components.Onboarding;
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

    private async void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;

        // Let the first frame render before loading large images.
        await Task.Yield();

        _viewModel.LoadSplashes();
    }
}