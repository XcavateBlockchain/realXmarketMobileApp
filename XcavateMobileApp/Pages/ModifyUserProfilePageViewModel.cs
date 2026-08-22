using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Buttons;
using PlutoFramework.Components.Loading;
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Model.Xcavate;
using PlutoFramework.Model.Xcavate.Profile;

namespace XcavateMobileApp.Pages
{
    public partial class ModifyUserProfilePageViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CancelButtonText))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        private bool firstSetup = false;

        [ObservableProperty]
        private string? title;

        [ObservableProperty]
        private ImageSource? profilePicture;

        [ObservableProperty]
        private ImageSource? profileBackground;

        private Stream? profilePictureStream;

        [ObservableProperty]
        private string nickname = "";

        [ObservableProperty]
        private string bio = "";

        /// <summary>
        /// Held while a save or a skip is in flight, so a second tap cannot start a second
        /// registration over the first, or skip past one that is still running.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SaveButtonState))]
        [NotifyPropertyChangedFor(nameof(CancelButtonState))]
        private bool isSaving = false;

        public string CancelButtonText => FirstSetup ? "Skip" : "Cancel";

        public ButtonStateEnum CancelButtonState => IsSaving ? ButtonStateEnum.Disabled : ButtonStateEnum.Enabled;

        public bool IsEditMode => !FirstSetup;

        public int Step => OnboardingStepperViewModel.GetStep(OnboardingStage.ProfileRegistration);

        public int Steps => OnboardingStepperViewModel.TotalSteps;

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

        /// <summary>
        /// Cancel when editing, Skip during onboarding. Skipping still registers a profile -
        /// the Solana address and the X25519 key under it are what let anyone reach this
        /// wallet - and leaves out only the nickname, bio and picture the user declined to
        /// give. Onboarding therefore finishes only once that write has gone through.
        /// </summary>
        [RelayCommand]
        public async Task CancelAsync()
        {
            if (!FirstSetup)
            {
                await Application.Current.MainPage.Navigation.PopAsync();

                return;
            }

            if (IsSaving)
            {
                return;
            }

            IsSaving = true;

            try
            {
                if (!await TryRegisterProfileAsync())
                {
                    return;
                }
            }
            finally
            {
                IsSaving = false;
            }

            finishFirstSetup();
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (IsSaving)
            {
                return;
            }

            IsSaving = true;

            // What is checked for uniqueness has to be what is stored, or a nickname padded
            // with spaces would pass the check as blank and then be published anyway.
            Nickname = Nickname.Trim();

            var loadingViewModel = DependencyService.Get<FullPageLoadingViewModel>();

            try
            {
                if (!await NicknameIsAvailableAsync())
                {
                    return;
                }

                loadingViewModel.IsVisible = true;
                loadingViewModel.Message = "Moving image data";

                MoveImages();

                if (!await TryRegisterProfileAsync(nickname: Nickname, profilePictureStream: profilePictureStream, bio: Bio))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                loadingViewModel.IsVisible = false;

                Console.WriteLine(ex);

                await Toast.Make($"Could not save your profile: {ex.Message}").Show();

                return;
            }
            finally
            {
                loadingViewModel.IsVisible = false;

                IsSaving = false;
            }

            if (!FirstSetup)
            {
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            else
            {
                finishFirstSetup();
            }
        }

        /// <summary>
        /// Whether the entered nickname can be published. Nicknames are optional, so an empty
        /// one always passes; anything else has to be free. A check that could not be
        /// completed blocks the save as well: the server refuses a duplicate anyway, and a
        /// message the user can act on beats a signature prompt that ends in a rejection.
        /// </summary>
        private async Task<bool> NicknameIsAvailableAsync()
        {
            if (string.IsNullOrEmpty(Nickname))
            {
                return true;
            }

            var loadingViewModel = DependencyService.Get<FullPageLoadingViewModel>();

            loadingViewModel.IsVisible = true;
            loadingViewModel.Message = "Checking nickname";

            string message;

            try
            {
                var profileService = new XcavateProfileService();

                if (await profileService.IsNicknameAvailableAsync(Nickname))
                {
                    return true;
                }

                message = $"\"{Nickname}\" is already taken. Please pick a different nickname.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                message = $"Could not check whether that nickname is free: {ex.Message}";
            }
            finally
            {
                loadingViewModel.IsVisible = false;
            }

            await Toast.Make(message).Show();

            return false;
        }

        /// <summary>
        /// Registers the profile and reports whether it went through, putting the reason in
        /// front of the user when it did not. Callers must not navigate on false: onboarding
        /// would be marked finished, or the edit page closed, over a profile never stored.
        /// </summary>
        private static async Task<bool> TryRegisterProfileAsync(
            string? nickname = null,
            Stream? profilePictureStream = null,
            string? bio = null)
        {
            try
            {
                var profileService = new XcavateProfileService();

                if (await profileService.RegisterProfileAsync(nickname: nickname, profilePictureStream: profilePictureStream, bio: bio))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                await Toast.Make($"Could not save your profile: {ex.Message}").Show();

                return false;
            }

            // Nothing failed on the wire: there was no key to sign with, or the password
            // prompt was dismissed.
            await Toast.Make("Your profile was not saved. Unlock your account and try again.").Show();

            return false;
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

        public ButtonStateEnum SaveButtonState => IsSaving ? ButtonStateEnum.Disabled : ButtonStateEnum.Enabled;
    }
}
