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


            string address = Preferences.Get(PreferencesModel.PUBLIC_KEY, "None");
            string didAddress = Preferences.Get($"{PreferencesModel.PUBLIC_KEY}kilt1", "None");

            try
            {
                var questions = await QuestionnaireModel.GetXcavateQuestionsAsync();
                var questionnaireInfo = new QuestionnaireInfo
                {
                    QuestionId = 0,
                    Questions = questions,
                    Navigation = () => SumsubVerificationAsync(email, phoneNumber, address, didAddress)
                };

                if (questions.Count == 0)
                {
                    return;
                }

                await Shell.Current.Navigation.PushAsync(new QuestionnairePage(questionnaireInfo));
            }
            catch (Exception ex)
            {
                Console.WriteLine("UserTypeSelectionPage error:");
                Console.WriteLine(ex);

                await Shell.Current.Navigation.PushAsync(new BadInternetConnectionPage());
            }
        }

        public async Task SumsubVerificationAsync(
            string email,
            string phoneNumber,
            string address,
            string didAddress
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
                    ExternalUserId = didAddress,
                },
                totalInSeconds = 600,
                UserId = $"{address}",
                LevelName = userRole.ToSumsubVerificationLevel()
            };

            var secrets = SumsubSecretModel.GetSecrets();

            try
            {
                var accessToken = await SumsubModel.GenerateWebSDKAccessTokenAsync(applicant, secrets.SecretKey, secrets.AppToken, token);

                await Shell.Current.Navigation.PushAsync(new SumsubWebSDKPage(
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

                await Shell.Current.Navigation.PushAsync(new BadInternetConnectionPage());
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

        [RelayCommand]
        public void SelectInvestor()
        {
            userRole = UserRoleEnum.Investor;

            var modifyUserProfileViewModel = DependencyService.Get<ModifyUserProfilePopupViewModel>();
            modifyUserProfileViewModel.IsVisible = true;
            modifyUserProfileViewModel.ContinueFunction = ContinueAsync;
        }

        [RelayCommand]
        public void SelectLettingAgent()
        {
            userRole = UserRoleEnum.LettingAgent;

            var modifyUserProfileViewModel = DependencyService.Get<ModifyUserProfilePopupViewModel>();
            modifyUserProfileViewModel.IsVisible = true;
            modifyUserProfileViewModel.ContinueFunction = ContinueAsync;
        }

        [RelayCommand]
        public void SelectLawyer()
        {
            userRole = UserRoleEnum.Lawyer;

            var modifyUserProfileViewModel = DependencyService.Get<ModifyUserProfilePopupViewModel>();
            modifyUserProfileViewModel.IsVisible = true;
            modifyUserProfileViewModel.ContinueFunction = ContinueAsync;
        }
    }
}
