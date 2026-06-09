using PlutoFramework.Components.Xcavate;
using static PlutoFramework.Components.Xcavate.XcavateNavigationBarViewModel;

namespace XcavateMobileApp;

public partial class NoAccountShell : Shell
{
    public NoAccountShell()
    {
        InitializeComponent();

        var navigationViewModel = DependencyService.Get<XcavateNavigationBarViewModel>();
        navigationViewModel.Selected = XcavateNavigationBarSelection.Marketplace;
    }
}