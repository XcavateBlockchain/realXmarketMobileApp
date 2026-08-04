using PlutoFramework.Components.Credits;
using PlutoFramework.Components.CustomLayouts;
using PlutoFramework.Components.Settings;
using PlutoFramework.Components.Xcavate;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Xcavate;
using PlutoFramework.Templates.PageTemplate;

namespace XcavateMobileApp.Pages;

public partial class SettingsPage : PageTemplate
{
    public SettingsPage()
    {
        InitializeComponent();

        BindingContext = new SettingsViewModel();
    }

    async void OnPredefinedLayoutsClicked(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await Navigation.PushAsync(new PredefinedLayoutsPage());
    }

    async void OnLogOutClicked(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        var popupViewModel = DependencyService.Get<LogOutPopupViewModel>();

        popupViewModel.ContinueRequested = async () =>
        {
            // GetAccountAsync only looks at Sr25519/PolkadotJson keys, so it returns null for a
            // Solana-only user and the old unconditional early return made this button a silent
            // no-op for them - confirmed, then nothing, with no way to remove the wallet from
            // the device. The decrypt is still the ownership check on the Substrate path.
            if (KeysModel.HasSubstrateKey())
            {
                var account = await KeysModel.GetAccountAsync();

                if (account is null)
                {
                    return;
                }
            }
            else if (!KeysModel.HasSolanaKey())
            {
                return;
            }

            ClearStateModel.Clear();

            await SQLiteModel.DeleteAllDatabasesAsync();

            await Shell.Current.GoToAsync("//LoggedOutPage");
        };

        popupViewModel.IsVisible = true;
    }
    async void OnDeveloperSettingsClicked(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await Navigation.PushAsync(new DeveloperSettingsPage());
    }

    async void OnNotificationTestingClicked(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await Navigation.PushAsync(new NotificationTestingPage());
    }

    async void OnXcavateProfileClicked(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await NavigationModel.NavigateToUserPageAsync();
    }
    private async void OnXcavateCompanyClicked(object sender, TappedEventArgs e)
    {
        var viewModel = new CompanyViewModel();
        viewModel.Company = await XcavateCompanyModel.GetMockCompanyAsync();

        await Navigation.PushAsync(new CompanyPage(viewModel));

    }

    private async void OnPropertyClicked(object sender, TappedEventArgs e)
    {
        /*var viewModel = new PropertyDetailViewModel
        {
            AreaPricesPercentage = 0.7,

            RentalDemandPercentage = 0.3,

            CompanyName = "Gade homes",

            CompanyImage = "xcavate.png",

            LocationName = "Herford, Hertfordshire UK",

            PropertyName = "Plot 1 - Plea Wharf",

            ListingPrice = "�200,000",
            Apy = "5%",
            Tokens = 15,
            MaxTokens = 100,
            PropertyType = "Appartment / Flat",

            PropertyDescription = "XCAV is the native token of the Xcavate ecosystem and has several utilities. XCAV will be used in on-chain governance, allowing holders to decide on protocol changes and parameters (such as the protocol fee). This also gives holders the power to allocate funds from the treasury to further the growth and development of the network. The treasury will get an initial allocation of XCAV tokens (see below) and acquire further inflows by collecting protocol fees",

            Blocks = "10",
            Bedrooms = "3",
            Bathrooms = "2",
            Type = "Appartment",
            LocationShortName = "Herford UK",

            UsdtPricePerToken = 2300.0,

            RentalIncome = "�1,000 pcm",

            Images = [
                "https://www.nintendo.com/eu/media/images/assets/nintendo_switch_games/xenobladechroniclesxdefinitiveedition/nswitch_xenobladechroniclesxdefinitiveedition/XenobladeChroniclesXDefinitiveEdition_27.png",
                "https://www.nintendo.com/eu/media/images/assets/nintendo_switch_games/xenobladechroniclesxdefinitiveedition/nswitch_xenobladechroniclesxdefinitiveedition/XenobladeChroniclesXDefinitiveEdition_GP_19.png",
                "xcavatergb.png",
            ]
        };
        await Navigation.PushAsync(new PropertyDetailPage(viewModel));*/
    }

    private async void OnCreateNewPropertyClicked(object sender, TappedEventArgs e)
    {
        /*var property = await XcavatePropertyDatabase.GetPropertyAsync() ?? new UniqueryPlus.Metadata.XcavateMetadata
        {
            Images = [],
            PropertyName = ""
        };

        await Navigation.PushAsync(new ModifyPropertyPage(property));*/
    }

    async void OnCreditsClicked(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await Navigation.PushAsync(new CreditsPage());
    }
}