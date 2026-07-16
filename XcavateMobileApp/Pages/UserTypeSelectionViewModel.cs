using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Components.Error;
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Components.Sumsub;
using PlutoFramework.Components.Xcavate;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using PlutoFramework.Model.Sumsub;
using PlutoFramework.Model.Xcavate;
using XcavateMobileApp.Components.Account;

namespace XcavateMobileApp.Pages
{
    public partial class UserTypeSelectionViewModel : ObservableObject
    {
        private UserRoleEnum userRole;

        public static Task NavigateToUserDetailsAsync()
        {
            OnboardingModel.SetOnboardingStage(OnboardingStage.SelectRole);

            return Shell.Current.Navigation.PushAsync(new UserTypeSelectionPage());
        }

        public static async Task ResumeKycFromSavedProfileAsync()
        {
            var userInfo = await XcavateUserDatabase.GetUserInformationAsync();

            if (userInfo is null)
            {
                await NavigateToUserDetailsAsync();
                return;
            }

            string address = KeysModel.GetSubstrateKey();
            string didAddress = await KeysModel.GetDidAddressAsync(CancellationToken.None);

            await NavigateToSumsubAsync(
                userInfo.Role,
                userInfo.Email,
                userInfo.PhoneNumber,
                address,
                didAddress
            );
        }

        public async Task ContinueAsync(
            string firstName,
            string lastName,
            string email,
            string phoneNumber
        )
        {
            await NavigateToQuestionnaireAsync(userRole, email, phoneNumber);
        }

        public static async Task NavigateToQuestionnaireFromSavedProfileAsync()
        {
            var userInfo = await XcavateUserDatabase.GetUserInformationAsync();

            if (userInfo is null)
            {
                await NavigateToUserDetailsAsync();
                return;
            }

            await NavigateToQuestionnaireAsync(userInfo.Role, userInfo.Email, userInfo.PhoneNumber);
        }

        private static async Task NavigateToQuestionnaireAsync(
            UserRoleEnum role,
            string email,
            string phoneNumber
        )
        {
            try
            {
                string address = KeysModel.GetSubstrateKey();
                string didAddress = await KeysModel.GetDidAddressAsync(CancellationToken.None);

                OnboardingModel.SetOnboardingStage(OnboardingStage.Questionaire);

                var questions = await QuestionnaireModel.GetXcavateQuestionsAsync();

                if (questions.Count == 0)
                {
                    await new OnboardingAgreementCoordinator().StartAsync(
                        () => NavigateToSumsubAsync(role, email, phoneNumber, address, didAddress)
                    );
                    return;
                }

                var questionnaireInfo = new QuestionnaireInfo
                {
                    Sections = questions,
                    Navigation = () => NavigateToSumsubAsync(role, email, phoneNumber, address, didAddress)
                };

                await Shell.Current.Navigation.PushAsync(new QuestionnaireV2QuestionsPage(questionnaireInfo));
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
            await NavigateToSumsubAsync(userRole, email, phoneNumber, address, didAddress);
        }

        private static async Task NavigateToSumsubAsync(
            UserRoleEnum role,
            string email,
            string phoneNumber,
            string address,
            string didAddress
        )
        {
            var token = CancellationToken.None;

            try
            {
                OnboardingModel.SetOnboardingStage(OnboardingStage.KYC);

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
                    UserId = address,
                    LevelName = role.ToSumsubVerificationLevel(),
                };

                var secrets = SumsubSecretModel.GetSecrets();

                var accessToken = await SumsubModel.GenerateWebSDKAccessTokenAsync(applicant, secrets.SecretKey, secrets.AppToken, token);

                await Shell.Current.Navigation.PushAsync(new SumsubWebSDKPage(
                    accessToken ?? "",
                    applicant,
                    navigation: () =>
                    {
                        OnboardingModel.SetOnboardingStage(OnboardingStage.ProfileRegistration);

                        return ImportAccountCoordinator.NavigateToProfileRegistrationAsync();
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
            SelectRole(UserRoleEnum.Developer);
        }

        [RelayCommand]
        public void SelectInvestor()
        {
            SelectRole(UserRoleEnum.Investor);
        }

        [RelayCommand]
        public void SelectLettingAgent()
        {
            SelectRole(UserRoleEnum.LettingAgent);
        }

        [RelayCommand]
        public void SelectLawyer()
        {
            SelectRole(UserRoleEnum.Lawyer);
        }

        private void SelectRole(UserRoleEnum role)
        {
            userRole = role;
            OnboardingModel.SetOnboardingStage(OnboardingStage.EnterUserDetails);

            var modifyUserProfileViewModel = DependencyService.Get<ModifyUserProfilePopupViewModel>();
            modifyUserProfileViewModel.UserRole = role;
            modifyUserProfileViewModel.IsVisible = true;
            modifyUserProfileViewModel.ContinueFunction = ContinueAsync;
        }
    }
}
