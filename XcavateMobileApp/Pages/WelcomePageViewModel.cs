using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Account;
using PlutoFramework.Model;
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
        public WelcomePageViewModel()
        {
        }

        public ObservableCollection<WelcomeSplash> Splashes { get; } = new ObservableCollection<WelcomeSplash>();

        public void LoadSplashes()
        {
            if (Splashes.Count > 0)
            {
                return;
            }

            Splashes.Add(new WelcomeSplash { Image = "xcavatelaunchbg1.jpg", Description = "Fractional real estate investment made simple and secure" });
            Splashes.Add(new WelcomeSplash { Image = "xcavatelaunchbg2.png", Description = "Browse the marketplace to find your ideal property investment" });
            Splashes.Add(new WelcomeSplash { Image = "xcavatelaunchbg3.png", Description = "Unlock the future of real assets with secure, shared ownership" });
        }

        [RelayCommand]
        public void BrowseProperties()
        {
            Application.Current.MainPage = new NoAccountShell();
        }

        [RelayCommand]
        public Task ImportAccountAsync()
        {
            return NavigationModel.StartImportAccount(ImportAccountFlowMode.Import);
        }

        [RelayCommand]
        public async Task CreateAccountAsync()
        {
            try
            {
                await NavigationModel.StartImportAccount(ImportAccountFlowMode.Create);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

    }
}
