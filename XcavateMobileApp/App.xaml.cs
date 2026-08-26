using Microsoft.Extensions.Configuration;
using PlutoFramework.Components.Account;
using PlutoFramework.Components.Loading;
using PlutoFramework.Components.Notifications;
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Constants;
using PlutoFramework.Model;
using PlutoFramework.Model.Initializers;
using PlutoFramework.Model.Xcavate;
using PlutoFramework.Model.Xcavate.Profile;
using PlutoFrameworkCore;
using PlutoFrameworkCore.Solana;
using XcavateMobileApp.Components.Account;
using XcavateMobileApp.Components.Sumsub;
using XcavateMobileApp.Pages;

namespace XcavateMobileApp
{
    public partial class App : Application
    {
        private bool _isInitialized;

        public App()
        {
            InitializeComponent();

            MainPage = new ContentPage
            {
                Content = new Grid
                {
                    Children =
                    {
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                        },
                    },
                },
            };

            Dispatcher.Dispatch(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            // Let the first frame render before doing heavier setup.
            await Task.Yield();

            PlutoFramework.MauiAppBuilderExtensions.InitializePlutoFrameworkFull();

            NavigationModel.NavigateToKYC = UserTypeSelectionViewModel.ResumeKycFromSavedProfileAsync;

            NavigationModel.NavigateAfterAccountCreation = () =>
            {
                // TODO: Verify if user has KYC
                return UserTypeSelectionViewModel.NavigateToUserDetailsAsync();
            };

            // Register app-level import account coordinator as a delegate for framework components
            NavigationModel.StartImportAccount = async (ImportAccountFlowMode flowMode) =>
            {
                var coordinator = new ImportAccountCoordinator();
                await coordinator.StartAsync(flowMode);
            };

            // Sumsub applicants are keyed by the Solana wallet address. Without one there is
            // nothing to look up.
            NavigationModel.NavigateToKYCUserPage = () =>
            {
                var solanaAddress = KeysModel.GetSolanaAddress();

                if (solanaAddress is null)
                {
                    DependencyService.Get<NoAccountPopupViewModel>().IsVisible = true;

                    return Task.CompletedTask;
                }

                return Shell.Current.Navigation.PushAsync(new SumsubUserPage(solanaAddress));
            };

            NavigationModel.NavigateToSettingsPageAsync = () => Shell.Current.Navigation.PushAsync(new SettingsPage());

            NavigationModel.NavigateToUserPageAsync = NavigateToUserPageAsync;

            PlutoConfigurationModel.GenerateNewAccountAsync = GenerateNewAccountAsync;
            PlutoConfigurationModel.WhitelistedTokens = [
                // XCAV
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Native, 0),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.NativeFrozen, 0),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.NativeReserved, 0),

                // tGBP
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Assets, 10),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsFrozen, 10),
                (EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsReserved, 10),

                // USDC
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Assets, 1337),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsFrozen, 1337),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsReserved, 1337),

                // USDT
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.Assets, 1984),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsFrozen, 1984),
                //(EndpointEnum.XcavatePaseo, PlutoFramework.Types.AssetPallet.AssetsReserved, 1984),
            ];

            // Mint addresses are cluster-specific; the same token has a different one on each.
            // Both verified live on 2026-07-25.
            PlutoConfigurationModel.WhitelistedSolanaTokens = [
                new SolanaTokenWhitelistEntry
                {
                    Cluster = SolanaCluster.Mainnet,
                    Mint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
                    Symbol = "USDC",
                    Decimals = 6,
                    PinnedUsdPrice = 1.00,
                },
                new SolanaTokenWhitelistEntry
                {
                    Cluster = SolanaCluster.Devnet,
                    Mint = "4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU",
                    Symbol = "USDC",
                    Decimals = 6,
                    PinnedUsdPrice = 1.00,
                },
                new SolanaTokenWhitelistEntry {
                    Cluster = SolanaCluster.Devnet,
                    Mint = "8umv4NXybZFGiT3tQb1DqJ6DXxLa3rLNhPbcqbQsjXxW",
                    Symbol = "tUSDC",
                    Decimals = 6,
                    PinnedUsdPrice = 1.00,
                },
                new SolanaTokenWhitelistEntry {
                    Cluster = SolanaCluster.Devnet,
                    Mint = "8dW943dozaNPdRRaW6xpV2vxFv1Kcpz3z63Nji3VLups",
                    Symbol = "XCAV",
                    Decimals = 9,
                    PinnedUsdPrice = 1.00,
                },
                new SolanaTokenWhitelistEntry {
                    Cluster = SolanaCluster.Devnet,
                    Mint = "71G3dc4B9p9QBosLx3XhWY3ULRPAxjopngsin66M9HUb",
                    Symbol = "tGBP",
                    Decimals = 9,
                    PinnedUsdPrice = 1.00,
                }
            ];

            PlutoConfigurationModel.WhitelistedDApps = [
                "realxmessenger.xcavate.io",
            ];

            NavigationModel.SetWelcomeShell = () =>
            {
                Application.Current.MainPage = new OnboardingShell();
            };

            DependencyService.Register<ModifyUserProfilePopupViewModel>();

            // Same popup tracking as the framework's own popups (see PopupManager).
            PopupManager.TrackPopup(DependencyService.Get<ModifyUserProfilePopupViewModel>());

            DependencyService.Register<InvestorMainPageViewModel>();

            var onboardingPopupViewModel = DependencyService.Get<OnboardingInProgressPopupViewModel>();
            onboardingPopupViewModel.ContinueRequested = ContinueOnboardingAsync;

            // Either key counts. New accounts are Solana-only; users onboarded before that
            // change still hold a Substrate key and must not be pushed back into onboarding.
            MainPage = OnboardingModel.IsOnboardingCompleted() switch
            {
                true when KeysModel.HasSolanaKey() || KeysModel.HasSubstrateKey() => new XcavateAppShell(),
                _ => new OnboardingShell(),
            };

            StartNotificationServices();

            // A cold-start notification tap stashed its deep link in MainActivity
            // before any shell existed. Deferred one dispatcher loop so the fresh
            // shell's handlers are attached before a page is pushed onto it.
            Dispatcher.Dispatch(() => _ = NotificationDeepLinkModel.TryOpenPendingAsync());
        }

        /// <summary>
        /// Registers this device on the notifications API and links the user's wallet
        /// addresses to it, so wallet-targeted notifications reach this device. Runs in
        /// the background; called after the shell is set so the notification permission
        /// prompt appears over real UI rather than the loading spinner.
        /// </summary>
        private static void StartNotificationServices()
        {
            var configuration = PlutoFramework.MauiAppBuilderExtensions.Services.GetService<IConfiguration>();
            var notificationsApiUrl = configuration?["NOTIFICATIONS_API_URL"];

            if (string.IsNullOrWhiteSpace(notificationsApiUrl))
            {
                Console.WriteLine("[PlutoNotifications] NOTIFICATIONS_API_URL is not configured, notifications stay disabled.");

                return;
            }

            PushNotificationsAppInitializer.Initialize(notificationsApiUrl);
        }


        private static Task ContinueOnboardingAsync()
        {
            var stage = OnboardingModel.GetOnboardingStage();

            return ImportAccountCoordinator.ContinueAsync(stage);
        }

        public static async Task NavigateToUserPageAsync()
        {
            var loadingViewModel = DependencyService.Get<FullPageLoadingViewModel>();
            loadingViewModel.IsVisible = true;
            loadingViewModel.Message = "Finding profile";

            var profileService = new XcavateProfileService();
            var profile = await profileService.GetProfileAsync();

            var viewModel = new ModifyUserProfilePageViewModel()
            {
                Title = "Edit public profile",
                FirstSetup = false,
                Nickname = profile?.Nickname ?? string.Empty,
                Bio = profile?.Bio ?? string.Empty,
                ProfilePicture = ProfilePictureImageSourceModel.Create(profile?.ProfilePicture),
            };

            loadingViewModel.IsVisible = false;

            await Shell.Current.Navigation.PushAsync(new ModifyUserProfilePage(viewModel));
        }

        public static async Task GenerateNewAccountAsync()
        {
            await KeysModel.ClearAsync();

            await KeysModel.GenerateNewSolanaAccountAsync();
        }
    }
}