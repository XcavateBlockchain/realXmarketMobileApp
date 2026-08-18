# Onboarding Stepper on Password, Role and Profile Pages — Design

**Date:** 2026-07-29
**Status:** Approved, ready for implementation planning

## Goal

The onboarding stepper bar (`TopNavigationStepperBar`: back arrow + `ProgressStepperView`)
appears only from the questionnaire onward. The first two onboarding screens — password
setup and role selection — have no top bar at all, and the final, optional profile
registration screen shows a plain back-arrow overlay with no progress. The result is that
a new user sees no sense of progress until the third screen, and loses it again on the
last one.

Three pages change:

1. **`SetupPasswordPage`** gains the stepper bar.
2. **`UserTypeSelectionPage`** gains the stepper bar.
3. **`ModifyUserProfilePage`** gains the stepper bar **only when creating** the profile
   during onboarding (`FirstSetup`). When editing an existing profile the page looks
   exactly as it does today — no stepper.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Step count | 8 steps, one per stage with a screen | Chosen by the product owner. Password=1, Role=2, Questionnaire=3, Terms=4, Agreement=5, Privacy=6, KYC=7, Profile=8. Existing pages shift from x/5 to x/8. |
| User-details popup | Not a step | It opens over the role-selection page; the stepper underneath stays at step 2. |
| Profile page bar style (FirstSetup) | Solid `TopNavigationStepperBar`, same as questionnaire/KYC pages | Chosen by the product owner. The hero image moves down 55px during onboarding; edit mode keeps the current full-bleed overlay. |
| Back arrow on role selection | Kept, default pop behaviour | Chosen by the product owner. Matches every other stepper page, even though back lands on the password page after the wallet already exists. |
| Wiring pattern | Extend `OnboardingStepperViewModel` | The mapping already exists and every stepper page reads it; the three new pages follow the same pattern instead of hardcoding numbers. |

## Rejected alternatives

**Hardcoded `Step`/`Steps` literals per page.** No shared mapping change, but the numbers
drift the next time a stage is added or removed — which is exactly what this change is.

**A shared onboarding page template with the bar baked in.** Cleaner long-term, but it
means restructuring the six existing onboarding pages for no behavioural gain now.

## Components

### Changed — `PlutoFramework`

**`OnboardingStepperViewModel`** (`Components/Onboarding/`)
`TotalSteps` becomes 8. `GetStep` maps: `SetupPassword`=0, `SelectRole`=1,
`EnterUserDetails`=1 (same screen), `Questionaire`=2, `AgreeTerms`=3, `AgreeAgreement`=4,
`AgreePrivacy`=5, `KYC`=6, `ProfileRegistration`=7. Every existing stepper page picks the
new totals up automatically.

**`SetupPasswordPage`** (`Components/Password/`)
Root content becomes `Grid RowDefinitions="55,*"` with `TopNavigationStepperBar` in row 0
and the existing `ScrollView` in row 1 — the structure `QuestionnaireV2QuestionsPage`
already uses. The constructor sets
`BindingContext = new OnboardingStepperViewModel(OnboardingStage.SetupPassword)`, the
pattern `SumsubWebSDKPage` uses. `ImportWarningPopup` in the page's `PopupContent` sets
its own `BindingContext` in its constructor, so the page-level context does not disturb
it. Every use of this page — create flow, MWA import, `NoAccountPopupViewModel` fallback —
is a password setup at `OnboardingStage.SetupPassword`, so the bar is always correct.

### Changed — `XcavateMobileApp`

**`UserTypeSelectionPage`**
The existing padded grid is wrapped in an outer `Grid RowDefinitions="55,*"`; the stepper
bar sits full-width in row 0, above the padding. `UserTypeSelectionViewModel` gains
`Step => OnboardingStepperViewModel.GetStep(OnboardingStage.SelectRole)` and
`Steps => OnboardingStepperViewModel.TotalSteps`, mirroring the questionnaire view models.

**`ModifyUserProfilePage` + `ModifyUserProfilePageViewModel`**
Root becomes `Grid RowDefinitions="Auto,*"`:

- Row 0: `TopNavigationStepperBar` with `IsVisible="{Binding FirstSetup}"` — the `Auto`
  row collapses to nothing in edit mode. `Step`/`Steps` come from new view-model
  properties mapping `OnboardingStage.ProfileRegistration`.
- Row 1: the existing `AbsoluteLayout` (hero image, form, save bar) unchanged, except the
  translucent overlay `TopNavigationBar` becomes `IsVisible="{Binding IsEditMode}"`.

The view model gains `IsEditMode => !FirstSetup`, notified alongside the existing
`CancelButtonText` when `FirstSetup` changes. No converter dependency is introduced.

The back arrow behaves as the bar's default in both modes: pop the navigation stack. In
`FirstSetup` that lands on the Sumsub page, the same place the current overlay's back
arrow already goes.

## Failure and interruption

No new failure paths. The change is static layout plus a pure `switch` mapping. Onboarding
resume (`ImportAccountCoordinator.ContinueAsync`) re-creates each page the same way the
forward flow does, so a resumed page shows the same step it would have shown originally.

## Verification

The stepper mapping lives in the MAUI `PlutoFramework` project, which the existing test
suite (`PlutoFrameworkTests`, referencing only `PlutoFrameworkCore`) cannot reach, so no
automated coverage is proposed. Verification is a build plus manual runs:

1. Create flow — password page shows step 1/8, role selection 2/8, questionnaire 3/8,
   agreements 4–6/8, KYC 7/8, profile registration 8/8.
2. Import over MWA — the password page appears after the wallet connects and shows 1/8.
3. Edit profile from the user page — no stepper, page identical to today.
4. Back arrows — each new bar pops to the previous page; role selection's lands on the
   password page by product-owner decision.
