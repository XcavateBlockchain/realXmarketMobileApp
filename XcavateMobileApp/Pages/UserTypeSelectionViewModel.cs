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

        public int Step => OnboardingStepperViewModel.GetStep(OnboardingStage.SelectRole);

        public int Steps => OnboardingStepperViewModel.TotalSteps;

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

            await NavigateToSumsubAsync(
                userInfo.Role,
                userInfo.Email,
                userInfo.PhoneNumber
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
                OnboardingModel.SetOnboardingStage(OnboardingStage.Questionaire);

                var questions = await QuestionnaireModel.GetXcavateQuestionsAsync();

                if (questions.Count == 0)
                {
                    await new OnboardingAgreementCoordinator().StartAsync(
                        () => NavigateToSumsubAsync(role, email, phoneNumber)
                    );
                    return;
                }

                var questionnaireInfo = new QuestionnaireInfo
                {
                    Sections = questions,
                    Navigation = () => NavigateToSumsubAsync(role, email, phoneNumber)
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
            string phoneNumber
        )
        {
            await NavigateToSumsubAsync(userRole, email, phoneNumber);
        }

        private static async Task NavigateToSumsubAsync(
            UserRoleEnum role,
            string email,
            string phoneNumber
        )
        {
            var token = CancellationToken.None;

            try
            {
                OnboardingModel.SetOnboardingStage(OnboardingStage.KYC);

                await PermissionsModel.RequestCameraPermissionAsync();

                // Sumsub applicants are keyed by the Solana wallet address - never the
                // Polkadot/DID address - so registration and lookups agree on one id.
                string solanaAddress = KeysModel.GetSolanaAddress()
                    ?? throw new Exception("Solana key not found");

                var applicant = new Applicant
                {
                    ApplicantIdentifiers = new ApplicantIdentifiers
                    {
                        Email = email,
                        Phone = phoneNumber,
                        ExternalUserId = solanaAddress,
                    },
                    totalInSeconds = 600,
                    UserId = solanaAddress,
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
