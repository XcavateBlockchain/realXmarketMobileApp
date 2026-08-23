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
    /// <summary>
    /// What is known about the nickname currently in the field.
    /// </summary>
    public enum NicknameAvailability
    {
        /// <summary>Nothing to check - the field is empty - so nothing is claimed either way.</summary>
        Unknown,
        Checking,
        Available,
        Taken,
        /// <summary>The lookup could not be completed. Not the same as taken: the save tries again.</summary>
        Unverified,
    }

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

        /// <summary>
        /// How long typing has to stop before the nickname is looked up. Long enough that a
        /// name typed straight through costs one request rather than one per letter.
        /// </summary>
        private static readonly TimeSpan NICKNAME_CHECK_DEBOUNCE = TimeSpan.FromMilliseconds(500);

        private CancellationTokenSource? nicknameCheckCancellation;

        [ObservableProperty]
        private string nickname = "";

        [ObservableProperty]
        private string bio = "";

        /// <summary>
        /// What the last finished lookup said about the nickname on screen. Kept current as
        /// the user types, so a name someone else already publishes is refused here rather
        /// than after they have been asked to sign for it.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SaveButtonState))]
        [NotifyPropertyChangedFor(nameof(NicknameStatusMessage))]
        [NotifyPropertyChangedFor(nameof(NicknameStatusColor))]
        [NotifyPropertyChangedFor(nameof(NicknameStatusIsVisible))]
        private NicknameAvailability nicknameStatus = NicknameAvailability.Unknown;

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

        public bool NicknameStatusIsVisible => NicknameStatus != NicknameAvailability.Unknown;

        public string NicknameStatusMessage => NicknameStatus switch
        {
            NicknameAvailability.Checking => "Checking availability...",
            NicknameAvailability.Available => "This nickname is available",
            NicknameAvailability.Taken => "This nickname is already taken",
            NicknameAvailability.Unverified => "Could not check this nickname right now",
            _ => "",
        };

        public Color NicknameStatusColor => NicknameStatus switch
        {
            NicknameAvailability.Available => (Color)Application.Current!.Resources["Positive"],
            NicknameAvailability.Taken => (Color)Application.Current!.Resources["DangerousRed"],
            _ => (Color)Application.Current!.Resources["Gray500"],
        };

        /// <summary>
        /// Starts a fresh, debounced lookup for what is now in the field. Every keystroke
        /// lands here, which is why the previous one's lookup is abandoned first: its answer
        /// is about text that is no longer on screen.
        /// </summary>
        partial void OnNicknameChanged(string value)
        {
            nicknameCheckCancellation?.Cancel();
            nicknameCheckCancellation = null;

            var nickname = value.Trim();

            if (nickname == "")
            {
                // A nickname is optional, so an empty field is not a problem to report.
                NicknameStatus = NicknameAvailability.Unknown;

                return;
            }

            NicknameStatus = NicknameAvailability.Checking;

            var cancellation = new CancellationTokenSource();

            nicknameCheckCancellation = cancellation;

            _ = CheckNicknameAsync(nickname, cancellation);
        }

        private async Task CheckNicknameAsync(string nickname, CancellationTokenSource cancellation)
        {
            try
            {
                await Task.Delay(NICKNAME_CHECK_DEBOUNCE, cancellation.Token);

                var profileService = new XcavateProfileService();

                var available = await profileService.IsNicknameAvailableAsync(nickname, cancellation.Token);

                SetNicknameStatus(nickname, available ? NicknameAvailability.Available : NicknameAvailability.Taken);
            }
            catch (OperationCanceledException)
            {
                // Another keystroke arrived, and whichever verdict this was heading for is
                // about text the user has already moved on from.
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // Not knowing is not the same as being taken, so the button stays live and
                // the check at save time gets one more attempt before anything is published.
                SetNicknameStatus(nickname, NicknameAvailability.Unverified);
            }
            finally
            {
                // Disposed here rather than where it is cancelled: this is the only code that
                // knows the request holding the token has finished with it. The field is
                // released first - Cancel on an already-disposed source throws, and that
                // throw out of the next keystroke's setter is what used to freeze the status
                // on whatever the last completed check said, keeping "taken" up forever.
                if (ReferenceEquals(nicknameCheckCancellation, cancellation))
                {
                    nicknameCheckCancellation = null;
                }

                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Applies a verdict only while it is still about the text on screen, on the UI
        /// thread. Cancelling cannot recall a request that had already come back, so a slow
        /// answer can arrive after the user has typed on.
        /// </summary>
        private void SetNicknameStatus(string nickname, NicknameAvailability availability) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Nickname.Trim() != nickname)
                {
                    return;
                }

                NicknameStatus = availability;
            });

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
                    NicknameStatus = NicknameAvailability.Available;

                    return true;
                }

                // Claimed since the field last said otherwise, or claimed all along and the
                // typed check never completed. Either way the field should now say so.
                NicknameStatus = NicknameAvailability.Taken;

                message = $"\"{Nickname}\" is already taken. Please pick a different nickname.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                NicknameStatus = NicknameAvailability.Unverified;

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

        // Checking and Unverified deliberately leave the button live: the user should not be
        // held up by a lookup they cannot see the result of, and the save re-checks anyway.
        public ButtonStateEnum SaveButtonState =>
            IsSaving || NicknameStatus == NicknameAvailability.Taken
                ? ButtonStateEnum.Disabled
                : ButtonStateEnum.Enabled;
    }
}
