using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Buttons;
using PlutoFramework.Components.Loading;
using PlutoFramework.Model.Xcavate;
using PlutoFramework.Model.Xcavate.Profile;

namespace XcavateMobileApp.Pages
{
    public partial class ModifyUserProfilePageViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CancelButtonText))]
        private bool firstSetup = false;

        [ObservableProperty]
        private string? title;

        [ObservableProperty]
        private ImageSource? profilePicture;

        [ObservableProperty]
        private ImageSource? profileBackground;

        private Stream? profilePictureStream;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SaveButtonState))]
        private string nickname = "";

        [ObservableProperty]
        private string bio = "";

        public string CancelButtonText => FirstSetup ? "Skip" : "Cancel";

        [RelayCommand]
        public async Task PickProfilePictureAsync()
        {
            var result = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Select a profile picture",
                SelectionLimit = 1,
            });

            if (result == null || result.Count() != 1)
            {
                return;
            }

            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, "temporaryprofilepicture");

            // Release the handle from a previous pick, otherwise recreating the file throws IOException
            profilePictureStream?.Dispose();
            profilePictureStream = null;

            byte[] imageBytes;

            using (var inputStream = await result.First().OpenReadAsync())
            using (var memoryStream = new MemoryStream())
            {
                await inputStream.CopyToAsync(memoryStream);

                imageBytes = memoryStream.ToArray();
            }

            await File.WriteAllBytesAsync(targetFile, imageBytes);

            ProfilePicture = ImageSource.FromStream(() => new MemoryStream(imageBytes));

            profilePictureStream = new MemoryStream(imageBytes);
        }


        [RelayCommand]
        public async Task PickProfileBackgroundAsync()
        {
            var result = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Select a profile background",
                SelectionLimit = 1,
            });

            if (result == null || result.Count() != 1)
            {
                return;
            }

            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, "temporaryprofilebackground");

            byte[] imageBytes;

            using (var inputStream = await result.First().OpenReadAsync())
            using (var memoryStream = new MemoryStream())
            {
                await inputStream.CopyToAsync(memoryStream);

                imageBytes = memoryStream.ToArray();
            }

            await File.WriteAllBytesAsync(targetFile, imageBytes);

            ProfileBackground = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            if (!FirstSetup)
            {
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            else
            {
                var profileService = new XcavateProfileService();
                await profileService.RegisterProfileAsync();

                finishFirstSetup();
            }
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            var loadingViewModel = DependencyService.Get<FullPageLoadingViewModel>();
            loadingViewModel.IsVisible = true;
            loadingViewModel.Message = "Moving image data";

            MoveImages();

            var profileService = new XcavateProfileService();


            await profileService.RegisterProfileAsync(nickname: Nickname, profilePictureStream: profilePictureStream, bio: Bio);

            if (!FirstSetup)
            {
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            else
            {
                finishFirstSetup();
            }
        }

        private void finishFirstSetup()
        {
            OnboardingModel.SetOnboardingStage(OnboardingStage.Finished);
            Application.Current.MainPage = new XcavateAppShell();
        }

        private void MoveImages()
        {
            string tempProfilePicturePath = Path.Combine(FileSystem.Current.AppDataDirectory, "temporaryprofilepicture");
            string profilePicturePath = Path.Combine(FileSystem.Current.AppDataDirectory, XcavateConstants.PROFILE_PICTURE_FILE_NAME);

            if (File.Exists(tempProfilePicturePath))
            {
                File.Move(tempProfilePicturePath, profilePicturePath, true);
            }

            string tempProfileBackgroundPath = Path.Combine(FileSystem.Current.AppDataDirectory, "temporaryprofilebackground");
            string profileBackgroundPath = Path.Combine(FileSystem.Current.AppDataDirectory, XcavateConstants.PROFILE_BACKGROUND_FILE_NAME);

            if (File.Exists(tempProfileBackgroundPath))
            {
                File.Move(tempProfileBackgroundPath, profileBackgroundPath, true);
            }
        }

        public ButtonStateEnum SaveButtonState => ButtonStateEnum.Enabled;
    }
}
