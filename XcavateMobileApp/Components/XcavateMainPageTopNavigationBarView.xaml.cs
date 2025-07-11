namespace XcavateMobileApp.Components;

public partial class XcavateMainPageTopNavigationBarView : ContentView
{
    public XcavateMainPageTopNavigationBarView()
    {
        InitializeComponent();

        BindingContext = new XcavateMainPageTopNavigationBarViewModel();

    }
}