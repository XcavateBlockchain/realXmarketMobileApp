
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Keys;
using PlutoFramework.Components.Kilt;
using PlutoFramework.Components.Mnemonics;
using PlutoFramework.Components.Password;
using PlutoFramework.Model;
using PlutoFrameworkCore;
using System.Collections.ObjectModel;

namespace XcavateMobileApp.Pages
{
    public class WelcomeSplash
    {
        public required string Image { get; set; }
        public required string Description { get; set; }
    }

    public partial class WelcomePageViewModel : ObservableObject
    {
        public ObservableCollection<WelcomeSplash> Splashes => new ObservableCollection<WelcomeSplash>
        {
            new WelcomeSplash{ Image = "xcavatelaunchbg1.jpg", Description = "Fractional real estate investment made simple and secure" },
            new WelcomeSplash{ Image = "xcavatelaunchbg2.png", Description = "Browse the marketplace to find your ideal property investment" },
            new WelcomeSplash{ Image = "xcavatelaunchbg3.png", Description = "Unlock the future of real estate with secure, tokenized ownership" },
        };

        [RelayCommand]
        public void BrowseProperties()
        {
            Application.Current.MainPage = new XcavateAppShell();
        }

        // Lets go pyramid code
        [RelayCommand]
        public Task ImportAccountAsync() => Shell.Current.Navigation.PushAsync(
            new SetupPasswordPage()
            {
                Navigation = () => Shell.Current.Navigation.PushAsync(
                    new EnterMnemonicsPage(
                        new EnterMnemonicsViewModel
                        {
                            Navigation = () => Shell.Current.Navigation.PushAsync(
                                new ImportDidPage(
                                    new ImportDidViewModel
                                    {
                                        Navigation = () => Shell.Current.Navigation.PushAsync(
                                            new ImportEncryptionX25519KeyPage(
                                                new ImportEncryptionX25519KeyPageViewModel
                                                {
                                                    Navigation = NavigationModel.NavigateAfterAccountCreation
                                                }
                                            )
                                        )
                                    }
                                )
                            )
                        }
                    )
                )
            }
        );

        [RelayCommand]
        public Task CreateAccountAsync() => Shell.Current.Navigation.PushAsync(
            new SetupPasswordPage()
            {
                Navigation = CreateAccountNavigationAsync
            }
        );
        public async Task CreateAccountNavigationAsync()
        {
            await PlutoConfigurationModel.GenerateNewAccountAsync();

            await Shell.Current.Navigation.PushAsync(
                new UserTypeSelectionPage()
            );
        }
    }
}
