using PlutoFramework.Templates.PageTemplate;

namespace XcavateMobileApp.Pages;

public partial class ModifyUserProfilePage : PageTemplate
{
    public ModifyUserProfilePage(ModifyUserProfilePageViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}