using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Error;
using PlutoFramework.Components.Sumsub;
using PlutoFramework.Components.Xcavate;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Sumsub;
using PlutoFramework.Model.Xcavate;

namespace XcavateMobileApp.Pages
{
    public partial class UserTypeSelectionViewModel : ObservableObject
    {
        private UserRoleEnum userRole;

        public async Task ContinueAsync(
            string firstName,
            string lastName,
            string email,
            string phoneNumber
        )
        {
            //MoveImages();

            var newUserInfo = new XcavateUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = userRole,
                DeveloperStats = null,
                AccountCreatedAt = DateTime.Now,
                ProfilePicture = XcavateFileModel.GetSavedProfilePicture(),
                ProfileBackground = XcavateFileModel.GetSavedProfileBackground(),
            };

            await XcavateUserDatabase.SaveUserInformationAsync(newUserInfo);

            var questions = QuestionaireModel.GetXcavateQuestions();
            var questionaireInfo = new QuestionaireInfo
            {
                QuestionId = 0,
                Questions = questions,
                Navigation = () => SumsubVerificationAsync(email, phoneNumber)
            };

            await Shell.Current.Navigation.PushAsync(new QuestionairePage(questionaireInfo));
        }

        public async Task SumsubVerificationAsync(
            string email,
            string phoneNumber
        )
        {
            var token = CancellationToken.None;

            await PermissionsModel.RequestCameraPermissionAsync();

            var applicant = new Applicant
            {
                ApplicantIdentifiers = new ApplicantIdentifiers
                {
                    Email = email,
                    Phone = phoneNumber,
                },
                totalInSeconds = 600,
                UserId = $"USER_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                LevelName = "csharp-verification-test"
            };

            try
            {
                var accessToken = await SumsubModel.GenerateWebSDKAccessTokenAsync(applicant, token);

                await Application.Current.MainPage.Navigation.PushAsync(new SumsubWebSDKPage(
                    accessToken ?? "",
                    applicant,
                    navigation: () =>
                    {
                        Application.Current.MainPage = new XcavateAppShell();

                        return Task.FromResult(0);
                    }
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine("UserTypeSelectionPage error:");

                Console.WriteLine(ex);

                // Most likely bad internet connection

                await Application.Current.MainPage.Navigation.PushAsync(new BadInternetConnectionPage());
            }
        }

        [RelayCommand]
        public void SelectDeveloper()
        {
            userRole = UserRoleEnum.Developer;

            var modifyUserProfileViewModel = DependencyService.Get<ModifyUserProfilePopupViewModel>();
            modifyUserProfileViewModel.IsVisible = true;
            modifyUserProfileViewModel.ContinueFunction = ContinueAsync;
        }
    }
}
