# Onboarding Stepper on Password, Role and Profile Pages — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show the onboarding top bar + progress stepper on the password-setup and role-selection pages, and on the profile page during first setup only — never when editing an existing profile.

**Architecture:** Extend the existing `OnboardingStepperViewModel` stage→step mapping from 5 to 8 steps, then place the existing `TopNavigationStepperBar` control on each of the three pages the same way the questionnaire/agreement/KYC pages already do (a `Grid` row above the content, `Step`/`Steps` bound to the page's BindingContext).

**Tech Stack:** .NET MAUI (net10.0), XAML, CommunityToolkit.Mvvm. Two git repos: the `realXmarketPlutoFramework` submodule (framework pages) and the parent repo (app pages).

**Spec:** `docs/superpowers/specs/2026-07-29-onboarding-stepper-navbar-design.md`

## Global Constraints

- Build check (framework tasks): `dotnet build P:\programming\realXmarketMobileApp\realXmarketPlutoFramework\PlutoFramework\PlutoFramework.csproj -f net10.0-android`
- Build check (app tasks): `dotnet build P:\programming\realXmarketMobileApp\XcavateMobileApp\XcavateMobileApp.csproj -f net10.0-android`
- No automated tests can reach these components (`PlutoFrameworkTests` references only `PlutoFrameworkCore`), so the test cycle per task is: edit → build passes → commit. TDD does not apply; the spec's verification section defines manual runs (Task 5).
- The submodule working tree contains **unrelated uncommitted Solana-transfer work**. In submodule commits, `git add` ONLY the files named in the task — never `git add -A` / `git add .`.
- In parent-repo commits, never stage the submodule pointer path `realXmarketPlutoFramework` — it was already modified before this work and the user manages it with their ongoing Solana branch work.
- Parent repo branch: `solana-support`. Submodule branch: `Solana-support` (already checked out).
- Commit messages follow the repos' existing style (`feat: …`) and end with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Step numbering is 0-based in code (`Step` is the index of the highlighted segment in `ProgressStepperView`); user-facing "step 1 of 8" = `Step = 0`, `Steps = 8`.

---

### Task 1: Extend `OnboardingStepperViewModel` to 8 steps

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Onboarding/OnboardingStepperViewModel.cs`

**Interfaces:**
- Consumes: `OnboardingStage` enum from `PlutoFramework.Model.Xcavate` (values: `SetupPassword`, `SelectRole`, `EnterUserDetails`, `Questionaire` (sic — existing spelling), `AgreeTerms`, `AgreeAgreement`, `AgreePrivacy`, `KYC`, `ProfileRegistration`).
- Produces: `OnboardingStepperViewModel.TotalSteps == 8` (const int) and `static int GetStep(OnboardingStage stage)` returning the 0-based mapping below. Tasks 2–4 call both.

- [ ] **Step 1: Update the mapping**

Replace the body of `OnboardingStepperViewModel` (keep the class shell, constructor, `Stage`, `Step`, `Steps` members as they are) so the constant and mapping read:

```csharp
public const int TotalSteps = 8;
```

```csharp
public static int GetStep(OnboardingStage stage)
{
    return stage switch
    {
        OnboardingStage.SetupPassword => 0,
        OnboardingStage.SelectRole => 1,
        OnboardingStage.EnterUserDetails => 1,
        OnboardingStage.Questionaire => 2,
        OnboardingStage.AgreeTerms => 3,
        OnboardingStage.AgreeAgreement => 4,
        OnboardingStage.AgreePrivacy => 5,
        OnboardingStage.KYC => 6,
        OnboardingStage.ProfileRegistration => 7,
        _ => 0,
    };
}
```

`EnterUserDetails` maps to 1 because the user-details popup opens over the role-selection page; the stepper underneath stays on step 2-of-8.

- [ ] **Step 2: Build to verify**

Run: `dotnet build P:\programming\realXmarketMobileApp\realXmarketPlutoFramework\PlutoFramework\PlutoFramework.csproj -f net10.0-android`
Expected: Build succeeded (warnings OK).

- [ ] **Step 3: Commit (in the submodule repo)**

```powershell
git -C P:\programming\realXmarketMobileApp\realXmarketPlutoFramework add PlutoFramework/Components/Onboarding/OnboardingStepperViewModel.cs
git -C P:\programming\realXmarketMobileApp\realXmarketPlutoFramework commit -m @'
feat: extend the onboarding stepper to 8 steps covering the whole flow

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```

---

### Task 2: Stepper bar on `SetupPasswordPage`

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml.cs`

**Interfaces:**
- Consumes: `OnboardingStepperViewModel(OnboardingStage)` from Task 1; `TopNavigationStepperBar` (existing, `PlutoFramework.Components.NavigationBar`, bindables `Step`, `Steps`, back arrow pops by default).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Restructure the XAML**

Replace the full content of `SetupPasswordPage.xaml` with:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<template:PageTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                       xmlns:template="clr-namespace:PlutoFramework.Templates.PageTemplate"
                       xmlns:card="clr-namespace:PlutoFramework.Components.Card"
                       xmlns:buttons="clr-namespace:PlutoFramework.Components.Buttons"
                       xmlns:account="clr-namespace:PlutoFramework.Components.Account"
                       xmlns:navigationbar="clr-namespace:PlutoFramework.Components.NavigationBar"
                       xmlns:password="clr-namespace:PlutoFramework.Components.Password"
                       x:Class="PlutoFramework.Components.Password.SetupPasswordPage"
                       Title="SetupPasswordPage"
                       NavigationBarIsVisible="False">
    <Grid AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1"
          AbsoluteLayout.LayoutFlags="All"
          RowDefinitions="55,*">
        <navigationbar:TopNavigationStepperBar Grid.Row="0"
                                               Step="{Binding Step}"
                                               Steps="{Binding Steps}" />

        <ScrollView Grid.Row="1">
            <VerticalStackLayout Padding="20, 20, 20, 20"
                                 Spacing="15">
                <ContentView>
                    <card:Card CardPadding="0">
                        <card:Card.View>
                            <password:SetPasswordView x:Name="setPasswordView"
                                                      Margin="10"
                                                      ValidityChanged="OnPasswordValidityChanged" />
                        </card:Card.View>
                    </card:Card>
                </ContentView>

                <buttons:ElevatedButton Text="Continue"
                                        x:Name="continueButton"
                                        ButtonState="Disabled"
                                        Clicked="ContinueToMainPageClicked"
                                        AutomationId="JoinButton"
                                        AutomationProperties.IsInAccessibleTree="True"
                                        SemanticProperties.Description="JoinButton" />
            </VerticalStackLayout>
        </ScrollView>
    </Grid>

    <template:PageTemplate.PopupContent>
        <account:ImportWarningPopup />
    </template:PageTemplate.PopupContent>
</template:PageTemplate>
```

This is the current page with the `ScrollView` moved into row 1 of a new root `Grid` (structure copied from `QuestionnaireV2QuestionsPage`), the stepper bar in row 0, and the now-redundant `AbsoluteLayout.*` attributes dropped from the inner elements.

- [ ] **Step 2: Set the stepper BindingContext in code-behind**

In `SetupPasswordPage.xaml.cs`, add these usings:

```csharp
using PlutoFramework.Components.Onboarding;
using PlutoFramework.Model.Xcavate;
```

and change the constructor to:

```csharp
public SetupPasswordPage()
{
    InitializeComponent();

    BindingContext = new OnboardingStepperViewModel(OnboardingStage.SetupPassword);
}
```

This is the same pattern `SumsubWebSDKPage` uses. `ImportWarningPopup` sets its own `BindingContext` in its constructor, so the page-level context does not affect it. Every caller of this page (create flow, MWA import, `NoAccountPopupViewModel` fallback) is at `OnboardingStage.SetupPassword`.

- [ ] **Step 3: Build to verify**

Run: `dotnet build P:\programming\realXmarketMobileApp\realXmarketPlutoFramework\PlutoFramework\PlutoFramework.csproj -f net10.0-android`
Expected: Build succeeded.

- [ ] **Step 4: Commit (in the submodule repo)**

```powershell
git -C P:\programming\realXmarketMobileApp\realXmarketPlutoFramework add PlutoFramework/Components/Password/SetupPasswordPage.xaml PlutoFramework/Components/Password/SetupPasswordPage.xaml.cs
git -C P:\programming\realXmarketMobileApp\realXmarketPlutoFramework commit -m @'
feat: show the onboarding stepper bar on the password setup page

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```

---

### Task 3: Stepper bar on `UserTypeSelectionPage`

**Files:**
- Modify: `XcavateMobileApp/Pages/UserTypeSelectionPage.xaml`
- Modify: `XcavateMobileApp/Pages/UserTypeSelectionViewModel.cs`

**Interfaces:**
- Consumes: `OnboardingStepperViewModel.GetStep` / `.TotalSteps` from Task 1; `TopNavigationStepperBar` (xmlns `navigationbar` is already declared in this XAML file).
- Produces: `UserTypeSelectionViewModel.Step` (int, get-only) and `.Steps` (int, get-only) — bound by the page only.

- [ ] **Step 1: Add `Step`/`Steps` to the view model**

In `UserTypeSelectionViewModel.cs` (the `using PlutoFramework.Components.Onboarding;` and `using PlutoFramework.Model.Xcavate;` directives already exist), add inside the class, next to the other public members:

```csharp
public int Step => OnboardingStepperViewModel.GetStep(OnboardingStage.SelectRole);

public int Steps => OnboardingStepperViewModel.TotalSteps;
```

The page's BindingContext is `new UserTypeSelectionViewModel()` (set in the page constructor), so the bar's bindings resolve against these.

- [ ] **Step 2: Wrap the page content in a stepper grid**

In `UserTypeSelectionPage.xaml`, the root content is currently
`<Grid AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1" AbsoluteLayout.LayoutFlags="All" Padding="20" ColumnSpacing="15" RowSpacing="15">…</Grid>`.
Wrap it in an outer grid so the bar sits full-width above the padded content — the outer element takes over the `AbsoluteLayout.*` attributes, the inner grid keeps its padding/spacing and everything inside it stays byte-for-byte unchanged:

```xml
<Grid AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1"
      AbsoluteLayout.LayoutFlags="All"
      RowDefinitions="55,*">
    <navigationbar:TopNavigationStepperBar Grid.Row="0"
                                           Step="{Binding Step}"
                                           Steps="{Binding Steps}" />

    <Grid Grid.Row="1"
          Padding="20"
          ColumnSpacing="15"
          RowSpacing="15">
        <!-- existing RowDefinitions/ColumnDefinitions, label and the four ClickableCards, unchanged -->
    </Grid>
</Grid>
```

Keep the existing `<page:PageTemplate.PopupContent>` block after the grid untouched.

- [ ] **Step 3: Build to verify**

Run: `dotnet build P:\programming\realXmarketMobileApp\XcavateMobileApp\XcavateMobileApp.csproj -f net10.0-android`
Expected: Build succeeded.

- [ ] **Step 4: Commit (in the parent repo — do not stage the submodule pointer)**

```powershell
git -C P:\programming\realXmarketMobileApp add XcavateMobileApp/Pages/UserTypeSelectionPage.xaml XcavateMobileApp/Pages/UserTypeSelectionViewModel.cs
git -C P:\programming\realXmarketMobileApp commit -m @'
feat: show the onboarding stepper bar on the role selection page

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```

---

### Task 4: First-setup-only stepper on `ModifyUserProfilePage`

**Files:**
- Modify: `XcavateMobileApp/Pages/ModifyUserProfilePage.xaml`
- Modify: `XcavateMobileApp/Pages/ModifyUserProfilePageViewModel.cs`

**Interfaces:**
- Consumes: `OnboardingStepperViewModel.GetStep` / `.TotalSteps` from Task 1; existing `FirstSetup` observable property.
- Produces: `ModifyUserProfilePageViewModel.IsEditMode` (bool, get-only, `!FirstSetup`, change-notified via `FirstSetup`), `.Step` / `.Steps` (int, get-only) — bound by the page only.

- [ ] **Step 1: Add `IsEditMode`, `Step`, `Steps` to the view model**

In `ModifyUserProfilePageViewModel.cs`, add the using:

```csharp
using PlutoFramework.Components.Onboarding;
```

(`using PlutoFramework.Model.Xcavate;` already exists.) Change the `firstSetup` field attributes so `IsEditMode` is re-read when the mode changes:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(CancelButtonText))]
[NotifyPropertyChangedFor(nameof(IsEditMode))]
private bool firstSetup = false;
```

and add next to `CancelButtonText`:

```csharp
public bool IsEditMode => !FirstSetup;

public int Step => OnboardingStepperViewModel.GetStep(OnboardingStage.ProfileRegistration);

public int Steps => OnboardingStepperViewModel.TotalSteps;
```

- [ ] **Step 2: Restructure the XAML root**

In `ModifyUserProfilePage.xaml`, the root content is currently
`<AbsoluteLayout AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1" AbsoluteLayout.LayoutFlags="All">…</AbsoluteLayout>`.
Wrap it in a two-row grid; the outer grid takes the `AbsoluteLayout.*` attributes:

```xml
<Grid AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1"
      AbsoluteLayout.LayoutFlags="All"
      RowDefinitions="Auto,*">
    <navigationbar:TopNavigationStepperBar Grid.Row="0"
                                           IsVisible="{Binding FirstSetup}"
                                           Step="{Binding Step}"
                                           Steps="{Binding Steps}" />

    <AbsoluteLayout Grid.Row="1">
        <!-- existing ScrollView (hero image + form), save/cancel layout, unchanged -->

        <navigationbar:TopNavigationBar IsVisible="{Binding IsEditMode}" />
    </AbsoluteLayout>
</Grid>
```

Two changes inside the existing tree, everything else unchanged:
1. The inner `AbsoluteLayout` loses its `AbsoluteLayout.LayoutBounds`/`LayoutFlags` attributes and gains `Grid.Row="1"`.
2. The trailing `<navigationbar:TopNavigationBar />` gains `IsVisible="{Binding IsEditMode}"`.

The `Auto` row collapses to zero height when the stepper bar is invisible (MAUI excludes invisible views from measurement), so edit mode renders exactly as today: full-bleed hero image with the translucent overlay bar. In first-setup mode the solid stepper bar (own `HeightRequest="55"`) pushes the hero down and shows step 8-of-8.

- [ ] **Step 3: Build to verify**

Run: `dotnet build P:\programming\realXmarketMobileApp\XcavateMobileApp\XcavateMobileApp.csproj -f net10.0-android`
Expected: Build succeeded.

- [ ] **Step 4: Commit (in the parent repo — do not stage the submodule pointer)**

```powershell
git -C P:\programming\realXmarketMobileApp add XcavateMobileApp/Pages/ModifyUserProfilePage.xaml XcavateMobileApp/Pages/ModifyUserProfilePageViewModel.cs
git -C P:\programming\realXmarketMobileApp commit -m @'
feat: show the onboarding stepper on profile registration but not when editing

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```

---

### Task 5: Manual verification (from the spec)

No code. Run the Android app and walk the flows; fix-forward anything that fails before declaring the plan done.

- [ ] **1. Create flow** — password page shows segment 1 of 8 highlighted, role selection 2/8, questionnaire 3/8, agreements 4–6/8, KYC 7/8, profile registration 8/8 with the solid bar above the hero image.
- [ ] **2. Import over MWA** — password page appears after the wallet connects and shows 1/8.
- [ ] **3. Edit profile** (user page → edit) — no stepper anywhere; page identical to before this change (overlay back arrow over the full-bleed hero image).
- [ ] **4. Back arrows** — each new bar's back arrow pops to the previous page (role selection intentionally lands back on the password page — product-owner decision).

**Note on the submodule pointer:** the parent repo's `realXmarketPlutoFramework` pointer was already dirty before this work (in-progress Solana transfer changes). Do not commit the pointer as part of this plan; the user records it together with their ongoing Solana work.
