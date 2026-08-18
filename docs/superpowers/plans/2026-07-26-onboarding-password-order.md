# Onboarding Password Order Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the onboarding password step so a seed-phrase import asks for the phrase and the password on one screen, and an MWA import connects the wallet before asking for a password at all.

**Architecture:** Both key saves read the stored password to encrypt the key, which is why every branch is password-first today. The seed-phrase branch gets a new page that commits the password and the key in one handler; the MWA popup stops saving entirely and hands the connected key to its caller, so onboarding can defer the save past a page. The password fields and the phrase editor are extracted into reusable views first, so the new page and the existing screens share one implementation.

**Tech Stack:** .NET 10 MAUI, CommunityToolkit.Mvvm, NUnit, Solnet (behind `SolanaMnemonicsModel`), Substrate.NET.Wallet (`WordManager`).

**Spec:** [docs/superpowers/specs/2026-07-26-onboarding-password-order-design.md](../specs/2026-07-26-onboarding-password-order-design.md)

## Global Constraints

- **Build command (the primary verification for every task):**
  `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
- **Test command:** `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj`
- **`PlutoFrameworkTests` references only `PlutoFrameworkCore`.** View models, views and pages live in `PlutoFramework` (a MAUI project) and in `XcavateMobileApp`, and are **not reachable** from the test project. Only Tasks 1 and 2 add automated tests, because only they add pure logic to `PlutoFrameworkCore`. **Do not add a MAUI project reference to the test project** to make other tasks testable — that is a restructuring nobody asked for. Every other task is verified by the build plus the manual checks written into it.
- **Password rules, verbatim:** length 6–20, at least one lowercase, at least one uppercase, at least one digit. The rule expressions are copied unchanged from the current `SetupPasswordPage` so behaviour does not drift.
- **Mobile Wallet Adapter is Android-only.** `SolanaMwaModel.IsSupported` is false on iOS, and manual MWA checks require an Android device with Phantom, Solflare or Backpack installed.
- **Both Solana key types share one account slot.** `SaveSolanaMnemonicKeyAsync` and `SaveSolanaMwaKeyAsync` each delete the other before saving. Never call two saves for one account.
- **Commit at the end of every task.** Each task leaves the app building and every existing flow working.
- The repo has a submodule: `PlutoFramework` files live in `realXmarketPlutoFramework/`, which is its own git repository. **Commit inside the submodule first, then commit the updated submodule pointer in the parent repo.** Tasks that touch both list the two commits separately.

---

### Task 1: Password rules in Core, `SetPasswordView`, and an all-at-once `SetupPasswordPage`

**Files:**
- Create: `realXmarketPlutoFramework/PlutoFrameworkCore/PasswordRulesModel.cs`
- Create: `realXmarketPlutoFramework/PlutoFrameworkTests/PasswordRulesTests.cs`
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetPasswordView.xaml`
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetPasswordView.xaml.cs`
- Create: `realXmarketPlutoFramework/PlutoFramework/Model/PasswordSetupModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces:
  - `PlutoFramework.Model.PasswordRulesModel` — `static bool HasAllowedLength(string?)`, `HasLowercase(string?)`, `HasUppercase(string?)`, `HasDigit(string?)`, `IsValid(string?)`; `const int MINIMUM_LENGTH = 6`, `MAXIMUM_LENGTH = 20`.
  - `PlutoFramework.Components.Password.SetPasswordView` — `string Password { get; }`, `bool IsValid { get; }`, `event EventHandler? ValidityChanged`.
  - `PlutoFramework.Model.PasswordSetupModel.SaveNewPasswordAsync(string password)` → `Task`.
  - `SetupPasswordPage` keeps its `public required Func<Task> Navigation;` field. Its constructor signature does not change.

- [ ] **Step 1: Write the failing test**

Create `realXmarketPlutoFramework/PlutoFrameworkTests/PasswordRulesTests.cs`:

```csharp
using PlutoFramework.Model;

namespace PlutoFrameworkTests
{
    /// <summary>
    /// The password rules two setup screens enforce. Tested here rather than through the
    /// screens because nothing in the MAUI project is reachable from this test project.
    /// </summary>
    public class PasswordRules
    {
        private const string ValidPassword = "Passw0rd";

        [Test]
        public void ValidPasswordSatisfiesEveryRule()
        {
            Assert.That(PasswordRulesModel.IsValid(ValidPassword), Is.True);
        }

        [Test]
        public void PasswordShorterThanSixCharactersFailsLength()
        {
            Assert.That(PasswordRulesModel.HasAllowedLength("Pa0w"), Is.False);
            Assert.That(PasswordRulesModel.IsValid("Pa0w"), Is.False);
        }

        [Test]
        public void PasswordLongerThanTwentyCharactersFailsLength()
        {
            // 21 characters.
            var tooLong = "Passw0rdPassw0rdPassw";

            Assert.That(tooLong, Has.Length.EqualTo(21));
            Assert.That(PasswordRulesModel.HasAllowedLength(tooLong), Is.False);
            Assert.That(PasswordRulesModel.IsValid(tooLong), Is.False);
        }

        [Test]
        public void PasswordWithoutUppercaseIsRejected()
        {
            Assert.That(PasswordRulesModel.HasUppercase("passw0rd"), Is.False);
            Assert.That(PasswordRulesModel.IsValid("passw0rd"), Is.False);
        }

        [Test]
        public void PasswordWithoutLowercaseIsRejected()
        {
            Assert.That(PasswordRulesModel.HasLowercase("PASSW0RD"), Is.False);
            Assert.That(PasswordRulesModel.IsValid("PASSW0RD"), Is.False);
        }

        [Test]
        public void PasswordWithoutDigitIsRejected()
        {
            Assert.That(PasswordRulesModel.HasDigit("Password"), Is.False);
            Assert.That(PasswordRulesModel.IsValid("Password"), Is.False);
        }

        [Test]
        public void NullAndEmptyAreRejectedWithoutThrowing()
        {
            Assert.That(PasswordRulesModel.IsValid(null), Is.False);
            Assert.That(PasswordRulesModel.IsValid(""), Is.False);
        }

        [Test]
        public void BoundaryLengthsAreAccepted()
        {
            // Exactly 6 and exactly 20, both otherwise valid.
            Assert.That(PasswordRulesModel.HasAllowedLength("Pas0wd"), Is.True);
            Assert.That(PasswordRulesModel.HasAllowedLength("Passw0rdPassw0rdPass"), Is.True);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter FullyQualifiedName~PasswordRules`
Expected: compile error — `PasswordRulesModel` does not exist in namespace `PlutoFramework.Model`.

- [ ] **Step 3: Write the implementation**

Create `realXmarketPlutoFramework/PlutoFrameworkCore/PasswordRulesModel.cs`:

```csharp
using Substrate.NET.Wallet;

namespace PlutoFramework.Model
{
    /// <summary>
    /// The rules an app password has to satisfy.
    /// </summary>
    /// <remarks>
    /// Lives in Core so the labels a user reads and the rule the Continue button obeys are
    /// the same expression, and so they can be tested - nothing in the MAUI project can be.
    /// The expressions are copied unchanged from the screen that used to hold them inline.
    /// </remarks>
    public static class PasswordRulesModel
    {
        public const int MINIMUM_LENGTH = 6;

        public const int MAXIMUM_LENGTH = 20;

        public static bool HasAllowedLength(string? password) =>
            WordManager.Create()
                .WithMinimumLength(MINIMUM_LENGTH)
                .WithMaximumLength(MAXIMUM_LENGTH)
                .IsValid(password ?? "");

        public static bool HasLowercase(string? password) =>
            WordManager.Create().Should().AtLeastOneLowercase().IsValid(password ?? "");

        public static bool HasUppercase(string? password) =>
            WordManager.Create().Should().AtLeastOneUppercase().IsValid(password ?? "");

        public static bool HasDigit(string? password) =>
            WordManager.Create().Should().AtLeastOneDigit().IsValid(password ?? "");

        public static bool IsValid(string? password) =>
            HasAllowedLength(password) &&
            HasLowercase(password) &&
            HasUppercase(password) &&
            HasDigit(password);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter FullyQualifiedName~PasswordRules`
Expected: PASS, 8 tests.

- [ ] **Step 5: Create `PasswordSetupModel`**

Create `realXmarketPlutoFramework/PlutoFramework/Model/PasswordSetupModel.cs`:

```csharp
namespace PlutoFramework.Model
{
    /// <summary>
    /// Commits the app password chosen during setup.
    /// </summary>
    /// <remarks>
    /// Both setup screens go through here so that "the password is stored" and "biometrics
    /// are registered" cannot come apart on one screen and not the other.
    /// </remarks>
    public static class PasswordSetupModel
    {
        public static async Task SaveNewPasswordAsync(string password)
        {
            await SecureStorage.Default.SetAsync(PreferencesModel.PASSWORD, password);

            await KeysModel.RegisterBiometricAuthenticationAsync();
        }
    }
}
```

- [ ] **Step 6: Create `SetPasswordView.xaml`**

Create `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetPasswordView.xaml`. This is the two sections lifted out of `SetupPasswordPage.xaml`, both visible, with no `IsVisible="false"` on the confirm section:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="PlutoFramework.Components.Password.SetPasswordView">

    <VerticalStackLayout>

        <Label FontAttributes="Bold"
               HorizontalOptions="Center"
               VerticalTextAlignment="End">
            <Label.FormattedText>
                <FormattedString>
                    <Span FontAttributes="Bold"
                          Text="Set new password: " />
                </FormattedString>
            </Label.FormattedText>
        </Label>

        <Grid HeightRequest="40"
              Margin="0, 10, 0, 0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="40" />
            </Grid.ColumnDefinitions>

            <AbsoluteLayout x:Name="layout" />
            <Entry HeightRequest="40"
                   HorizontalOptions="Start"
                   Placeholder="Password"
                   Margin="0, 0, 50, 0"
                   Keyboard="Plain"
                   IsPassword="True"
                   x:Name="passwordEntry"
                   Grid.Column="0"
                   IsSpellCheckEnabled="false"
                   IsTextPredictionEnabled="false"
                   AutomationId="PasswordEntry"
                   AutomationProperties.IsInAccessibleTree="True"
                   SemanticProperties.Description="PasswordEntry"
                   Completed="OnEnterPressedAsync"
                   PropertyChanged="OnPasswordPropertyChanged" />

            <Image AbsoluteLayout.LayoutBounds="1, .5, 20, 20"
                   AbsoluteLayout.LayoutFlags="PositionProportional"
                   Margin="10"
                   Grid.Column="1"
                   ZIndex="1000"
                   x:Name="eyeball">
                <Image.Source>
                    <FontImageSource Color="{AppThemeBinding Light={StaticResource Black}, Dark={StaticResource White}}"
                                     Glyph="&#xf06e;"
                                     FontFamily="FontAwesome"
                                     Size="20" />
                </Image.Source>
                <Image.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnEyeballClicked" />
                </Image.GestureRecognizers>
            </Image>

            <Image AbsoluteLayout.LayoutBounds="1, .5, 20, 20"
                   AbsoluteLayout.LayoutFlags="PositionProportional"
                   Margin="10"
                   Grid.Column="1"
                   ZIndex="1000"
                   x:Name="eyeballSlash"
                   IsVisible="false">
                <Image.Source>
                    <FontImageSource Color="{AppThemeBinding Light={StaticResource Black}, Dark={StaticResource White}}"
                                     Glyph="&#xf070;"
                                     FontFamily="FontAwesome"
                                     Size="20" />
                </Image.Source>
                <Image.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnEyeballClicked" />
                </Image.GestureRecognizers>
            </Image>
        </Grid>

        <Label VerticalTextAlignment="Start"
               HorizontalTextAlignment="Start"
               TextColor="#888888"
               Margin="5"
               Text="Used just for this app instance." />

        <Label VerticalTextAlignment="Start"
               HorizontalTextAlignment="Start"
               x:Name="lengthRequirementLabel"
               TextColor="DarkRed"
               Margin="5,0,5,0"
               Text="Length 6-20 characters" />

        <Label VerticalTextAlignment="Start"
               HorizontalTextAlignment="Start"
               x:Name="lowercaseRequirementLabel"
               TextColor="DarkRed"
               Margin="5,0,5,0"
               Text="At least 1 lowercase" />

        <Label VerticalTextAlignment="Start"
               HorizontalTextAlignment="Start"
               x:Name="uppercaseRequirementLabel"
               TextColor="DarkRed"
               Margin="5,0,5,0"
               Text="At least 1 uppercase" />

        <Label VerticalTextAlignment="Start"
               HorizontalTextAlignment="Start"
               x:Name="numberRequirementLabel"
               TextColor="DarkRed"
               Margin="5,0,5,5"
               Text="At least 1 number" />

        <Label FontAttributes="Bold"
               HorizontalOptions="Center"
               Margin="0, 10, 0, 0"
               VerticalTextAlignment="End">
            <Label.FormattedText>
                <FormattedString>
                    <Span FontAttributes="Bold"
                          Text="Confirm password: " />
                </FormattedString>
            </Label.FormattedText>
        </Label>

        <Grid HeightRequest="40"
              Margin="0, 10, 0, 0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="40" />
            </Grid.ColumnDefinitions>

            <AbsoluteLayout x:Name="confirmLayout" />
            <Entry HeightRequest="40"
                   HorizontalOptions="Start"
                   Placeholder="Confirm Password"
                   Margin="0, 0, 50, 0"
                   Keyboard="Plain"
                   IsPassword="True"
                   x:Name="confirmPasswordEntry"
                   Grid.Column="0"
                   WidthRequest="{Binding Source={x:Reference confirmLayout}, Path=Width}"
                   IsSpellCheckEnabled="false"
                   IsTextPredictionEnabled="false"
                   AutomationId="ConfirmPasswordEntry"
                   Completed="OnEnterPressedAsync"
                   PropertyChanged="OnConfirmPropertyChanged" />

            <Image AbsoluteLayout.LayoutBounds="1, .5, 20, 20"
                   AbsoluteLayout.LayoutFlags="PositionProportional"
                   Margin="10"
                   Grid.Column="1"
                   ZIndex="1000"
                   x:Name="confirmEyeball">
                <Image.Source>
                    <FontImageSource Color="{AppThemeBinding Light={StaticResource Black}, Dark={StaticResource White}}"
                                     Glyph="&#xf06e;"
                                     FontFamily="FontAwesome"
                                     Size="20" />
                </Image.Source>
                <Image.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnConfirmEyeballClicked" />
                </Image.GestureRecognizers>
            </Image>

            <Image AbsoluteLayout.LayoutBounds="1, .5, 20, 20"
                   AbsoluteLayout.LayoutFlags="PositionProportional"
                   Margin="10"
                   Grid.Column="1"
                   ZIndex="1000"
                   x:Name="confirmEyeballSlash"
                   IsVisible="false">
                <Image.Source>
                    <FontImageSource Color="{AppThemeBinding Light={StaticResource Black}, Dark={StaticResource White}}"
                                     Glyph="&#xf070;"
                                     FontFamily="FontAwesome"
                                     Size="20" />
                </Image.Source>
                <Image.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnConfirmEyeballClicked" />
                </Image.GestureRecognizers>
            </Image>
        </Grid>

        <Label VerticalTextAlignment="Start"
               HorizontalTextAlignment="Start"
               x:Name="passwordMatchLabel"
               TextColor="DarkRed"
               Margin="5, 5, 5, 5"
               Text="Passwords do not match."
               IsVisible="false" />

    </VerticalStackLayout>
</ContentView>
```

- [ ] **Step 7: Create `SetPasswordView.xaml.cs`**

```csharp
using PlutoFramework.Model;

namespace PlutoFramework.Components.Password;

/// <summary>
/// The password half of a setup screen: both entries, their reveal toggles, and live rule
/// and mismatch feedback.
/// </summary>
/// <remarks>
/// Owns no storage and no navigation. The host page decides what a valid password is for,
/// which is what lets the same view serve the create flow and the seed-phrase import.
/// </remarks>
public partial class SetPasswordView : ContentView
{
    public SetPasswordView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised whenever <see cref="IsValid"/> may have changed, so a host page can drive its
    /// own Continue button without polling.
    /// </summary>
    public event EventHandler? ValidityChanged;

    public string Password => passwordEntry.Text ?? "";

    private string Confirmation => confirmPasswordEntry.Text ?? "";

    /// <summary>
    /// Both fields are on screen together, so the confirmation is checked as it is typed
    /// rather than on the Continue tap.
    /// </summary>
    public bool IsValid => PasswordRulesModel.IsValid(Password) && Confirmation == Password;

    private void OnEyeballClicked(object sender, TappedEventArgs e)
    {
        passwordEntry.IsPassword = !passwordEntry.IsPassword;
        eyeball.IsVisible = passwordEntry.IsPassword;
        eyeballSlash.IsVisible = !passwordEntry.IsPassword;
    }

    private void OnConfirmEyeballClicked(object sender, TappedEventArgs e)
    {
        confirmPasswordEntry.IsPassword = !confirmPasswordEntry.IsPassword;
        confirmEyeball.IsVisible = confirmPasswordEntry.IsPassword;
        confirmEyeballSlash.IsVisible = !confirmPasswordEntry.IsPassword;
    }

    private async void OnEnterPressedAsync(object sender, EventArgs e)
    {
        var entry = (Entry)sender;

        if (entry.IsSoftInputShowing())
        {
            await entry.HideSoftInputAsync(CancellationToken.None);
        }
    }

    private void OnPasswordPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Text") return;

        UpdateFeedback();
    }

    private void OnConfirmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Text") return;

        UpdateFeedback();
    }

    private void UpdateFeedback()
    {
        var password = Password;

        lengthRequirementLabel.TextColor = PasswordRulesModel.HasAllowedLength(password)
            ? Colors.Green : Colors.DarkRed;
        lowercaseRequirementLabel.TextColor = PasswordRulesModel.HasLowercase(password)
            ? Colors.Green : Colors.DarkRed;
        uppercaseRequirementLabel.TextColor = PasswordRulesModel.HasUppercase(password)
            ? Colors.Green : Colors.DarkRed;
        numberRequirementLabel.TextColor = PasswordRulesModel.HasDigit(password)
            ? Colors.Green : Colors.DarkRed;

        // Silent until there is something to disagree with, so the label does not accuse the
        // user of a mismatch after their first keystroke in the confirmation field.
        passwordMatchLabel.IsVisible = Confirmation.Length > 0 && Confirmation != password;

        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

- [ ] **Step 8: Rewrite `SetupPasswordPage.xaml`**

Replace the whole file. The `PopupContent` block is unchanged from what is there today — Task 5 and Task 6 remove the Solana popups, not this task:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<template:PageTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                       xmlns:template="clr-namespace:PlutoFramework.Templates.PageTemplate"
                       xmlns:card="clr-namespace:PlutoFramework.Components.Card"
                       xmlns:buttons="clr-namespace:PlutoFramework.Components.Buttons"
                       xmlns:account="clr-namespace:PlutoFramework.Components.Account"
                       xmlns:password="clr-namespace:PlutoFramework.Components.Password"
                       xmlns:solana="clr-namespace:PlutoFramework.Components.Solana"
                       x:Class="PlutoFramework.Components.Password.SetupPasswordPage"
                       Title="SetupPasswordPage"
                       NavigationBarIsVisible="False">
    <ScrollView AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1"
                AbsoluteLayout.LayoutFlags="All">
        <VerticalStackLayout AbsoluteLayout.LayoutBounds="0.5, 1, 1, AutoSize"
                             AbsoluteLayout.LayoutFlags="PositionProportional, WidthProportional"
                             Padding="20, 20, 20, 20"
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

    <template:PageTemplate.PopupContent>
        <account:ImportWarningPopup />

        <!-- The Solana import flows continue in a popup over this page, because saving
             either kind of key needs the password that was just set. -->
        <solana:EnterSolanaMnemonicsPopupView />
        <solana:ConnectMwaPopupView />
    </template:PageTemplate.PopupContent>
</template:PageTemplate>
```

- [ ] **Step 9: Rewrite `SetupPasswordPage.xaml.cs`**

The staged reveal, `_confirmStep`, the `OnBackButtonPressed` override and the click-time mismatch check all go away — the view checks the match live:

```csharp
using PlutoFramework.Components.Buttons;
using PlutoFramework.Model;
using PlutoFramework.Templates.PageTemplate;

namespace PlutoFramework.Components.Password;

public partial class SetupPasswordPage : PageTemplate
{
    public required Func<Task> Navigation;

    private bool _clicked = false;

    public SetupPasswordPage()
    {
        InitializeComponent();
    }

    private void OnPasswordValidityChanged(object? sender, EventArgs e)
    {
        continueButton.ButtonState = setPasswordView.IsValid
            ? ButtonStateEnum.Enabled : ButtonStateEnum.Disabled;
    }

    private async void ContinueToMainPageClicked(System.Object sender, System.EventArgs e)
    {
        if (_clicked || !setPasswordView.IsValid) return;

        _clicked = true;

        await PasswordSetupModel.SaveNewPasswordAsync(setPasswordView.Password);

        await Navigation.Invoke();

        _clicked = false;
    }
}
```

- [ ] **Step 10: Build**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: build succeeded, 0 errors.

- [ ] **Step 11: Manual check**

Run the app on Android with no account. Welcome → **Create Account**. Expected: one screen with both password fields; Continue stays disabled until all four rules go green *and* the fields match; the mismatch label appears while the confirmation differs and disappears when it matches; Continue creates the account and lands in the app.

- [ ] **Step 12: Commit**

```bash
git -C realXmarketPlutoFramework add PlutoFrameworkCore/PasswordRulesModel.cs PlutoFrameworkTests/PasswordRulesTests.cs PlutoFramework/Components/Password/SetPasswordView.xaml PlutoFramework/Components/Password/SetPasswordView.xaml.cs PlutoFramework/Model/PasswordSetupModel.cs PlutoFramework/Components/Password/SetupPasswordPage.xaml PlutoFramework/Components/Password/SetupPasswordPage.xaml.cs
git -C realXmarketPlutoFramework commit -m "refactor: extract the password fields into a reusable view

The seed-phrase import needs the same two entries, the same four live rule
labels and the same match check on a screen of its own. Extracting them also
puts the rules in Core, where they can be tested.

Both fields now show at once, replacing the staged reveal.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"

git add realXmarketPlutoFramework
git commit -m "chore: bump PlutoFramework for the extracted password view

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: `SolanaMnemonicsEntryView` and its view model

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFrameworkCore/SolanaMnemonicsModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFrameworkTests/SolanaMnemonicsTests.cs`
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaMnemonicsEntryViewModel.cs`
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaMnemonicsEntryView.xaml`
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaMnemonicsEntryView.xaml.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/EnterSolanaMnemonicsPopupViewModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/EnterSolanaMnemonicsPopupView.xaml`

**Interfaces:**
- Consumes: nothing from Task 1.
- Produces:
  - `PlutoFramework.Model.SolanaMnemonicsModel.TryGetAddressPreview(string? mnemonics)` → `string` (empty when not derivable).
  - `PlutoFramework.Components.Solana.SolanaMnemonicsEntryViewModel` — `string Mnemonics { get; set; }`, `bool IsValid { get; }`, `string AddressPreview { get; }`, `bool AddressPreviewIsVisible { get; }`, `string ErrorMessage { get; set; }`, `bool ErrorIsVisible { get; set; }`, `void ShowError(string message)`, `void Reset()`. It is an `ObservableObject`, so `PropertyChanged` is available to hosts.
  - `PlutoFramework.Components.Solana.SolanaMnemonicsEntryView` — a `ContentView` that sets **no** `BindingContext` of its own; the host assigns one.
  - `EnterSolanaMnemonicsPopupViewModel.Entry` → `SolanaMnemonicsEntryViewModel`. Its `IsVisible`, `Completed` and `ContinueWithMnemonicsCommand` are unchanged. Its `Mnemonics`, `IsValid`, `AddressPreview`, `AddressPreviewIsVisible` and `IncorrectMnemonicsEntered` members are **removed** — they move to `Entry`.

- [ ] **Step 1: Write the failing test**

Append these to the `SolanaMnemonics` class in `realXmarketPlutoFramework/PlutoFrameworkTests/SolanaMnemonicsTests.cs`:

```csharp
        [Test]
        public void TryGetAddressPreviewReturnsTheAddressForAValidPhrase()
        {
            Assert.That(
                SolanaMnemonicsModel.TryGetAddressPreview(TestMnemonics),
                Is.EqualTo(ExpectedEd25519Bip32Address));
        }

        [Test]
        public void TryGetAddressPreviewIsEmptyForAHalfTypedPhrase()
        {
            Assert.That(SolanaMnemonicsModel.TryGetAddressPreview("lens scheme misery"), Is.Empty);
        }

        [Test]
        public void TryGetAddressPreviewIsEmptyForABadChecksum()
        {
            // Twelve wordlist words in a combination BIP39's checksum rejects. The canonical
            // valid all-abandon phrase ends in "about".
            var badChecksum = string.Join(" ", Enumerable.Repeat("abandon", 12));

            Assert.That(SolanaMnemonicsModel.ValidateMnemonics(badChecksum), Is.False);
            Assert.That(SolanaMnemonicsModel.TryGetAddressPreview(badChecksum), Is.Empty);
        }

        [Test]
        public void TryGetAddressPreviewIsEmptyForNullAndEmptyInput()
        {
            Assert.That(SolanaMnemonicsModel.TryGetAddressPreview(null), Is.Empty);
            Assert.That(SolanaMnemonicsModel.TryGetAddressPreview(""), Is.Empty);
            Assert.That(SolanaMnemonicsModel.TryGetAddressPreview("   "), Is.Empty);
        }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter FullyQualifiedName~SolanaMnemonics`
Expected: compile error — `SolanaMnemonicsModel` has no `TryGetAddressPreview`.

- [ ] **Step 3: Add `TryGetAddressPreview`**

Append inside the `SolanaMnemonicsModel` class in `realXmarketPlutoFramework/PlutoFrameworkCore/SolanaMnemonicsModel.cs`:

```csharp
        /// <summary>
        /// The address a phrase unlocks, or an empty string when nothing can be derived.
        /// </summary>
        /// <remarks>
        /// Shown live while the user types. It is the only thing standing between them and a
        /// phrase imported under the wrong derivation, which yields a valid but empty
        /// account - otherwise discoverable only after the import.
        /// </remarks>
        public static string TryGetAddressPreview(string? mnemonics)
        {
            if (!ValidateMnemonics(mnemonics ?? ""))
            {
                return "";
            }

            try
            {
                return GetAddressFromMnemonics(mnemonics!);
            }
            catch
            {
                // A passing checksum does not guarantee Solnet can derive from the phrase.
                return "";
            }
        }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter FullyQualifiedName~SolanaMnemonics`
Expected: PASS, including the four new tests.

- [ ] **Step 5: Create `SolanaMnemonicsEntryViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using PlutoFramework.Model;

namespace PlutoFramework.Components.Solana
{
    /// <summary>
    /// The seed-phrase half of any import surface: the phrase, whether it is importable, the
    /// address it unlocks, and one error line.
    /// </summary>
    /// <remarks>
    /// Held as a property by whatever hosts it, never registered with
    /// <see cref="DependencyService"/>. A shared instance would carry one user's phrase into
    /// the next screen that showed it.
    /// </remarks>
    public partial class SolanaMnemonicsEntryViewModel : ObservableObject
    {
        private const string INVALID_PHRASE_MESSAGE = "That is not a valid seed phrase.";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddressPreview))]
        [NotifyPropertyChangedFor(nameof(AddressPreviewIsVisible))]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private string mnemonics = "";

        [ObservableProperty]
        private bool errorIsVisible = false;

        [ObservableProperty]
        private string errorMessage = INVALID_PHRASE_MESSAGE;

        public bool IsValid => SolanaMnemonicsModel.ValidateMnemonics(Mnemonics);

        /// <summary>
        /// Lets the user confirm the derived address matches the one their existing wallet
        /// shows, before committing to the import.
        /// </summary>
        public string AddressPreview => SolanaMnemonicsModel.TryGetAddressPreview(Mnemonics);

        public bool AddressPreviewIsVisible => !string.IsNullOrEmpty(AddressPreview);

        partial void OnMnemonicsChanged(string value)
        {
            // Clear a stale error as soon as the user edits the phrase.
            ErrorIsVisible = false;
        }

        public void ShowError(string message)
        {
            ErrorMessage = message;
            ErrorIsVisible = true;
        }

        /// <summary>
        /// Clears the phrase along with the error. Hosts must call this once the phrase has
        /// served its purpose - it is somebody's seed phrase sitting in memory.
        /// </summary>
        public void Reset()
        {
            Mnemonics = "";
            ErrorIsVisible = false;
            ErrorMessage = INVALID_PHRASE_MESSAGE;
        }
    }
}
```

- [ ] **Step 6: Create `SolanaMnemonicsEntryView.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:card="clr-namespace:PlutoFramework.Components.Card"
             x:Class="PlutoFramework.Components.Solana.SolanaMnemonicsEntryView">

    <VerticalStackLayout Spacing="15">

        <card:Card>
            <card:Card.View>
                <VerticalStackLayout Padding="10, 10, 10, 10">
                    <Label Text="Your Solana seed phrase:"
                           FontAttributes="Bold"
                           HorizontalOptions="Center" />

                    <Editor HeightRequest="100"
                            AutomationId="solanaMnemonicsEntry"
                            Placeholder="Start entering here.."
                            Text="{Binding Mnemonics}" />
                </VerticalStackLayout>
            </card:Card.View>
        </card:Card>

        <!-- Lets the user confirm the address matches their existing wallet before importing. -->
        <VerticalStackLayout Spacing="4"
                             IsVisible="{Binding AddressPreviewIsVisible}">
            <Label Text="This phrase unlocks the address"
                   FontSize="12"
                   TextColor="#A6A6A6"
                   HorizontalTextAlignment="Center" />

            <Label Text="{Binding AddressPreview}"
                   FontFamily="SourceCode"
                   FontSize="13"
                   LineBreakMode="MiddleTruncation"
                   HorizontalTextAlignment="Center" />
        </VerticalStackLayout>

        <Label Text="{Binding ErrorMessage}"
               IsVisible="{Binding ErrorIsVisible}"
               HorizontalTextAlignment="Center" />

    </VerticalStackLayout>
</ContentView>
```

- [ ] **Step 7: Create `SolanaMnemonicsEntryView.xaml.cs`**

```csharp
namespace PlutoFramework.Components.Solana;

/// <summary>
/// Deliberately sets no <see cref="BindableObject.BindingContext"/>: hosts assign their own
/// <see cref="SolanaMnemonicsEntryViewModel"/>, which is what lets a page and a popup show
/// this without sharing one instance - and one user's phrase.
/// </summary>
public partial class SolanaMnemonicsEntryView : ContentView
{
    public SolanaMnemonicsEntryView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 8: Rewrite `EnterSolanaMnemonicsPopupViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Model;

namespace PlutoFramework.Components.Solana
{
    /// <summary>
    /// Takes an existing Solana seed phrase and saves it as the app's Solana key.
    /// </summary>
    /// <remarks>
    /// One instance is shared through <see cref="DependencyService"/>. Callers set
    /// <see cref="Completed"/> and then <see cref="IsVisible"/>. Only usable where a password
    /// is already stored - <see cref="KeysModel.SaveSolanaMnemonicKeyAsync"/> reads it to
    /// encrypt the phrase. Onboarding, which has no password yet, uses
    /// <c>ImportSolanaWalletPage</c> instead.
    /// </remarks>
    public partial class EnterSolanaMnemonicsPopupViewModel : ObservableObject, IPopup, ISetToDefault
    {
        [ObservableProperty]
        private bool isVisible = false;

        /// <summary>
        /// The phrase, its validity and its address preview. Not shared with any other
        /// surface, so a phrase typed here cannot surface anywhere else.
        /// </summary>
        public SolanaMnemonicsEntryViewModel Entry { get; } = new();

        /// <summary>
        /// Runs after the key is saved, with the phrase that was imported. The popup closes
        /// itself first.
        /// </summary>
        public Func<string, Task> Completed { get; set; } = (string mnemonics) => Task.CompletedTask;

        /// <summary>
        /// Runs when the card finishes closing. Clearing the phrase matters here beyond the
        /// usual reset: a shared instance would otherwise keep somebody's seed phrase in
        /// memory, and show it to whoever opens the popup next.
        /// </summary>
        public void SetToDefault()
        {
            IsVisible = false;
            Entry.Reset();
            Completed = (string mnemonics) => Task.CompletedTask;
        }

        [RelayCommand]
        public async Task ContinueWithMnemonicsAsync()
        {
            if (!Entry.IsValid)
            {
                Entry.ShowError("That is not a valid seed phrase.");

                return;
            }

            try
            {
                await KeysModel.SaveSolanaMnemonicKeyAsync(Entry.Mnemonics);
            }
            catch
            {
                // Only the save is guarded. Letting this also cover the callback would report
                // a phrase that imported perfectly well as a failure, on a popup that has
                // already closed - and, on a shared instance, the error would still be
                // showing the next time somebody opens it.
                //
                // The phrase was validated above, so this is not an invalid-phrase failure
                // and must not claim to be one.
                Entry.ShowError("Could not save your wallet. Please try again.");

                return;
            }

            // Captured before the reset below clears them.
            var completed = Completed;
            var mnemonics = Entry.Mnemonics;

            IsVisible = false;

            // Reset here rather than leaving it to the card's close animation: onboarding's
            // callback replaces the whole page, and a card torn down mid-animation never
            // reaches SetToDefault - which would leave the phrase sitting in this shared
            // instance for whoever opens the popup next.
            SetToDefault();

            await completed.Invoke(mnemonics);
        }
    }
}
```

- [ ] **Step 9: Update `EnterSolanaMnemonicsPopupView.xaml`**

Add `xmlns:solana="clr-namespace:PlutoFramework.Components.Solana"` to the root element, then replace everything between the opening `<VerticalStackLayout Spacing="15" Padding="10, 10, 10, 20">` and the `<buttons:ElevatedButton .../>` — that is the `card:Card` block, the address-preview stack and the invalid-phrase label — with a single line:

```xml
                    <solana:SolanaMnemonicsEntryView BindingContext="{Binding Entry}" />
```

The resulting body:

```xml
            <ScrollView>
                <VerticalStackLayout Spacing="15"
                                     Padding="10, 10, 10, 20">

                    <solana:SolanaMnemonicsEntryView BindingContext="{Binding Entry}" />

                    <buttons:ElevatedButton Text="Continue"
                                            AutomationId="ContinueSolanaMnemonics"
                                            ButtonState="Enabled"
                                            Command="{Binding ContinueWithMnemonicsCommand}" />
                </VerticalStackLayout>
            </ScrollView>
```

- [ ] **Step 10: Build**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: build succeeded, 0 errors.

- [ ] **Step 11: Manual check**

With an account already set up, go to the Solana balances page (or Settings → keys) and use the existing seed-phrase import popup. Expected: unchanged — the editor accepts a phrase, the address preview appears live once the phrase is valid, an invalid phrase shows "That is not a valid seed phrase.", and a valid one imports.

- [ ] **Step 12: Commit**

```bash
git -C realXmarketPlutoFramework add PlutoFrameworkCore/SolanaMnemonicsModel.cs PlutoFrameworkTests/SolanaMnemonicsTests.cs PlutoFramework/Components/Solana/SolanaMnemonicsEntryViewModel.cs PlutoFramework/Components/Solana/SolanaMnemonicsEntryView.xaml PlutoFramework/Components/Solana/SolanaMnemonicsEntryView.xaml.cs PlutoFramework/Components/Solana/EnterSolanaMnemonicsPopupViewModel.cs PlutoFramework/Components/Solana/EnterSolanaMnemonicsPopupView.xaml
git -C realXmarketPlutoFramework commit -m "refactor: extract the seed-phrase entry into a reusable view

The combined import screen needs the same editor and the same live address
preview as the popup. The preview is what catches a phrase imported under the
wrong derivation, so it must not exist in two versions.

Also stops the popup reporting a failed save as an invalid phrase.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"

git add realXmarketPlutoFramework
git commit -m "chore: bump PlutoFramework for the extracted seed-phrase view

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 3: `ImportSolanaWalletPage`

**Files:**
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/ImportSolanaWalletPage.xaml`
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/ImportSolanaWalletPage.xaml.cs`

**Interfaces:**
- Consumes: `SetPasswordView` (`Password`, `IsValid`, `ValidityChanged`) and `PasswordSetupModel.SaveNewPasswordAsync` from Task 1; `SolanaMnemonicsEntryViewModel` and `SolanaMnemonicsEntryView` from Task 2.
- Produces: `PlutoFramework.Components.Solana.ImportSolanaWalletPage`, constructed with an object initializer setting `public required Func<Task> Navigation;` — the same shape `SetupPasswordPage` uses, so Task 6 can swap one for the other.

This task creates the page but nothing navigates to it yet; Task 6 wires it up. Verification is the build plus a temporary manual check described in Step 4.

- [ ] **Step 1: Create `ImportSolanaWalletPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<template:PageTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                       xmlns:template="clr-namespace:PlutoFramework.Templates.PageTemplate"
                       xmlns:card="clr-namespace:PlutoFramework.Components.Card"
                       xmlns:buttons="clr-namespace:PlutoFramework.Components.Buttons"
                       xmlns:password="clr-namespace:PlutoFramework.Components.Password"
                       xmlns:solana="clr-namespace:PlutoFramework.Components.Solana"
                       x:Class="PlutoFramework.Components.Solana.ImportSolanaWalletPage"
                       Title="ImportSolanaWalletPage"
                       NavigationBarIsVisible="False">
    <ScrollView AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1"
                AbsoluteLayout.LayoutFlags="All">
        <VerticalStackLayout AbsoluteLayout.LayoutBounds="0.5, 1, 1, AutoSize"
                             AbsoluteLayout.LayoutFlags="PositionProportional, WidthProportional"
                             Padding="20, 20, 20, 20"
                             Spacing="15">

            <Label Text="Import your Solana wallet"
                   FontAttributes="Bold"
                   FontSize="18"
                   HorizontalTextAlignment="Center" />

            <solana:SolanaMnemonicsEntryView x:Name="mnemonicsEntry" />

            <card:Card CardPadding="0">
                <card:Card.View>
                    <password:SetPasswordView x:Name="setPasswordView"
                                              Margin="10"
                                              ValidityChanged="OnPasswordValidityChanged" />
                </card:Card.View>
            </card:Card>

            <buttons:ElevatedButton Text="Continue"
                                    x:Name="continueButton"
                                    ButtonState="Disabled"
                                    Clicked="ContinueClicked"
                                    AutomationId="ImportSolanaWalletContinue"
                                    AutomationProperties.IsInAccessibleTree="True"
                                    SemanticProperties.Description="ImportSolanaWalletContinue" />
        </VerticalStackLayout>
    </ScrollView>
</template:PageTemplate>
```

- [ ] **Step 2: Create `ImportSolanaWalletPage.xaml.cs`**

```csharp
using PlutoFramework.Components.Buttons;
using PlutoFramework.Model;
using PlutoFramework.Templates.PageTemplate;

namespace PlutoFramework.Components.Solana;

/// <summary>
/// Imports an existing Solana wallet from its seed phrase and sets the app password, on one
/// screen.
/// </summary>
/// <remarks>
/// The two belong together because the phrase cannot be saved without the password:
/// <see cref="KeysModel.SaveSolanaMnemonicKeyAsync"/> reads the stored password to encrypt
/// it. Splitting them across two screens is what forced onboarding to ask for a password
/// before the user had done anything about their wallet.
/// </remarks>
public partial class ImportSolanaWalletPage : PageTemplate
{
    public required Func<Task> Navigation;

    /// <summary>
    /// Owned by this page rather than resolved from <see cref="DependencyService"/>, so the
    /// phrase typed here reaches nothing else.
    /// </summary>
    /// <remarks>
    /// Assigned to the entry view directly in the constructor. Do not give this page a
    /// <see cref="BindableObject.BindingContext"/> to bind it through: <c>PageTemplate</c>
    /// pushes the page's context down onto its content, which would overwrite the
    /// assignment below.
    /// </remarks>
    private readonly SolanaMnemonicsEntryViewModel _entry = new();

    private bool _clicked = false;

    public ImportSolanaWalletPage()
    {
        InitializeComponent();

        mnemonicsEntry.BindingContext = _entry;

        _entry.PropertyChanged += (_, _) => UpdateContinueState();
    }

    private void OnPasswordValidityChanged(object? sender, EventArgs e) => UpdateContinueState();

    private void UpdateContinueState()
    {
        continueButton.ButtonState = _entry.IsValid && setPasswordView.IsValid
            ? ButtonStateEnum.Enabled : ButtonStateEnum.Disabled;
    }

    private async void ContinueClicked(object sender, EventArgs e)
    {
        if (_clicked || !_entry.IsValid || !setPasswordView.IsValid) return;

        _clicked = true;

        try
        {
            // This order is the whole reason the two are on one screen:
            // SaveSolanaMnemonicKeyAsync reads the stored password to encrypt the phrase.
            await PasswordSetupModel.SaveNewPasswordAsync(setPasswordView.Password);

            await KeysModel.SaveSolanaMnemonicKeyAsync(_entry.Mnemonics);
        }
        catch
        {
            // The phrase was validated before Continue was enabled, so calling this an
            // invalid phrase would be untrue and would send the user hunting for a typo
            // that is not there.
            _entry.ShowError("Could not save your wallet. Please try again.");

            _clicked = false;

            return;
        }

        // The phrase has served its purpose, and this page stays alive behind whatever the
        // callback navigates to.
        _entry.Reset();

        await Navigation.Invoke();

        _clicked = false;
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: build succeeded, 0 errors.

- [ ] **Step 4: Manual check**

The page is not reachable from the UI until Task 6. To verify it now, temporarily change `ImportAccountCoordinator.ShowImportMethodPopupAsync`'s `SeedPhraseChosen` handler to:

```csharp
popup.SeedPhraseChosen = () => _navigationService.NavigateToAsync(new ImportSolanaWalletPage
{
    Navigation = FinishOnboardingAsync,
});
```

Run on Android with no account: Welcome → **Import Account** → **Seed phrase**. Expected: one screen with the phrase editor, the address preview, both password fields and a Continue that only enables when everything is valid; completing it lands in the app on the imported account. Then **revert the temporary edit** — Task 6 makes it permanent along with the rest of the rewiring. Confirm `git -C realXmarketPlutoFramework status` and `git status` show no change to `ImportAccountCoordinator.cs` before committing.

- [ ] **Step 5: Commit**

```bash
git -C realXmarketPlutoFramework add PlutoFramework/Components/Solana/ImportSolanaWalletPage.xaml PlutoFramework/Components/Solana/ImportSolanaWalletPage.xaml.cs
git -C realXmarketPlutoFramework commit -m "feat: add a combined seed-phrase and password import page

One screen for both, because the phrase cannot be encrypted without the
password. Nothing navigates here yet.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"

git add realXmarketPlutoFramework
git commit -m "chore: bump PlutoFramework for the combined import page

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 4: The MWA popup stops saving

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFramework/Model/SolanaMwaModel.cs:30-54`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/ConnectMwaPopupViewModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaNoAccountView.xaml.cs:50-84`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Keys/NewKeyView.xaml.cs:68-75`
- Modify: `XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs:159-168`

**Interfaces:**
- Consumes: nothing from Tasks 1–3.
- Produces:
  - `PlutoFramework.Model.SolanaMwaModel.ConnectAsync(SolanaCluster cluster, IProgress<MwaConnectStage>? progress, CancellationToken token)` → `Task<SolanaMwaKey>`. **Replaces** `ConnectAndSaveAsync`, which is deleted.
  - `ConnectMwaPopupViewModel.Completed` → `Func<PlutoFrameworkCore.Keys.SolanaMwaKey, Task>`. Callers must now persist the key themselves with `KeysModel.SaveSolanaMwaKeyAsync(key)`.

Behaviour is unchanged in this task: all three callers save immediately in their callback. Task 6 is where onboarding starts deferring it.

- [ ] **Step 1: Split connect from save in `SolanaMwaModel`**

Replace the `ConnectAndSaveAsync` method (lines 24–54) with:

```csharp
        /// <summary>
        /// Connects to a wallet and authorizes an account. Does not persist anything - the
        /// caller decides when the authorization is saved, which is what lets onboarding ask
        /// for a password in between. The cluster is passed in rather than read here: callers
        /// connect on the app-wide <see cref="SolanaNetworkModel.SelectedCluster"/>.
        /// </summary>
        public static async Task<SolanaMwaKey> ConnectAsync(
            SolanaCluster cluster,
            IProgress<MwaConnectStage>? progress,
            CancellationToken token)
        {
            var result = await MwaConnectFlow.ConnectAsync(
                BuildIdentity(),
                cluster,
                existingAuthToken: null,
                progress,
                token);

            return new SolanaMwaKey
            {
                AuthToken = result.AuthToken,
                Address = result.Address,
                Chain = result.Chain,
                WalletUriBase = result.WalletUriBase,
                AccountLabel = result.AccountLabel,
            };
        }
```

- [ ] **Step 2: Change `ConnectMwaPopupViewModel` to report the key**

In `ConnectMwaPopupViewModel.cs`, add `using PlutoFrameworkCore.Keys;` to the usings, then change the `Completed` declaration:

```csharp
        /// <summary>
        /// Runs with the authorization once the wallet approves it. The popup closes itself
        /// first. Nothing is persisted here - the caller saves the key, because onboarding
        /// has to set a password before it can.
        /// </summary>
        public Func<SolanaMwaKey, Task> Completed { get; set; } = (SolanaMwaKey key) => Task.CompletedTask;
```

In `SetToDefault`, change the reset:

```csharp
            Completed = (SolanaMwaKey key) => Task.CompletedTask;
```

In `ConnectAsync`, replace the `var connected = false;` declaration and the try block's first lines so the key is captured:

```csharp
            SolanaMwaKey? connectedKey = null;

            try
            {
                var key = await SolanaMwaModel.ConnectAsync(SelectedCluster, progress, CancellationToken.None);

                // Set before the toast: the wallet has authorized at this point, so a toast
                // that fails to show must not be read as a failed connection.
                connectedKey = key;

                await Toast.Make($"Connected to {key.DisplayName}.").Show();
            }
```

and replace the tail of the method (from `if (!connected)`) with:

```csharp
            // A failed attempt leaves the popup open on its error message, so the user can
            // retry. Kept outside the try so a throw from the callback cannot be relabelled
            // as a connection failure on an already-closed popup.
            if (connectedKey is null)
            {
                return;
            }

            var completed = Completed;

            IsVisible = false;

            // Reset here rather than leaving it to the card's close animation: onboarding's
            // callback replaces the whole page, and a card torn down mid-animation never
            // reaches SetToDefault.
            SetToDefault();

            await completed.Invoke(connectedKey);
```

- [ ] **Step 3: Save in `SolanaNoAccountView`**

Replace the `MwaChosen` assignment and the stale comment above both handlers in `ImportAsync`:

```csharp
        // The seed-phrase popup saves the key itself through
        // KeysModel.SaveSolanaMnemonicKeyAsync before reporting back, so its callback only
        // refreshes. The MWA popup does not save - it reports the authorization and leaves
        // persisting it to whoever asked, so this saves before refreshing.
        popup.SeedPhraseChosen = () =>
        {
            var seedPhrasePopup = DependencyService.Get<EnterSolanaMnemonicsPopupViewModel>();

            seedPhrasePopup.Completed = (mnemonics) => NotifyAccountAddedAsync();

            seedPhrasePopup.IsVisible = true;

            return Task.CompletedTask;
        };

        popup.MwaChosen = () =>
        {
            var mwaPopup = DependencyService.Get<ConnectMwaPopupViewModel>();

            mwaPopup.Completed = async (key) =>
            {
                await KeysModel.SaveSolanaMwaKeyAsync(key);

                await NotifyAccountAddedAsync();
            };

            mwaPopup.IsVisible = true;

            return Task.CompletedTask;
        };
```

Add `using PlutoFramework.Model;` to the file's usings if it is not already there.

- [ ] **Step 4: Save in `NewKeyView`**

Replace `ShowConnectMwaPopup`:

```csharp
    /// <summary>
    /// The popup reports the authorization without persisting it, so this saves it. There is
    /// already a password here - this screen is only reachable once the app is set up.
    /// </summary>
    private void ShowConnectMwaPopup()
    {
        var popup = DependencyService.Get<ConnectMwaPopupViewModel>();

        popup.Completed = async (key) =>
        {
            await KeysModel.SaveSolanaMwaKeyAsync(key);

            await ChangeButtonsIfKeyExistsAsync();
        };

        popup.IsVisible = true;
    }
```

- [ ] **Step 5: Save in `ImportAccountCoordinator`**

Replace `ShowConnectMwaPopupAsync`. Behaviour is deliberately unchanged here — the password page still comes first — because Task 6 owns the reordering:

```csharp
    private static Task ShowConnectMwaPopupAsync()
    {
        var popup = DependencyService.Get<ConnectMwaPopupViewModel>();

        popup.Completed = async (key) =>
        {
            await KeysModel.SaveSolanaMwaKeyAsync(key);

            await FinishOnboardingAsync();
        };

        popup.IsVisible = true;

        return Task.CompletedTask;
    }
```

- [ ] **Step 6: Build**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: build succeeded, 0 errors. If anything still references `ConnectAndSaveAsync`, the compiler names it — fix that call site rather than restoring the method.

- [ ] **Step 7: Manual check (Android device with a wallet app installed)**

This is the regression that matters most in the whole plan — three call sites changed hands on who saves.

1. With an account already set up, open the Solana balances page for an account-less state (or Settings → keys → Solana MWA) and connect a wallet. Expected: the wallet approves, the toast shows, and the account appears. Reopening the app still shows it — that proves it was persisted, not just displayed.
2. Repeat from the `NewKeyView` entry point (Generate new key → Solana MWA). Same expectation.
3. Decline the approval in the wallet. Expected: the popup stays open with an error, no account is added.

- [ ] **Step 8: Commit**

```bash
git -C realXmarketPlutoFramework add PlutoFramework/Model/SolanaMwaModel.cs PlutoFramework/Components/Solana/ConnectMwaPopupViewModel.cs PlutoFramework/Components/Solana/SolanaNoAccountView.xaml.cs PlutoFramework/Components/Keys/NewKeyView.xaml.cs
git -C realXmarketPlutoFramework commit -m "refactor: let the MWA popup report a key instead of saving it

Saving the authorization needs a password, which onboarding does not have
until after the wallet has been connected. Handing the key back puts the save
under the caller's control. Every current caller saves immediately, so nothing
changes yet.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"

git add realXmarketPlutoFramework XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs
git commit -m "refactor: save the MWA key in the onboarding callback

Follows the popup no longer saving it. Same order as before.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: Host the MWA popup in the page template

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFramework/Templates/PageTemplate/Page.xaml:55`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaBalancesPage.xaml:67`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Keys/CreateNewKeyPage.xaml`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml`
- Modify: `XcavateMobileApp/Pages/WelcomePage.xaml:116`

**Interfaces:**
- Consumes: nothing.
- Produces: `ConnectMwaPopupView` is available on every `PageTemplate` page and on `WelcomePage` without being declared per page. No C# API changes.

`ConnectMwaPopupView` binds to a view model shared through `DependencyService`, so **two live instances would show two popups**. Every per-page copy must be removed in the same commit that adds the template one.

- [ ] **Step 1: Add the popup to the page template**

In `realXmarketPlutoFramework/PlutoFramework/Templates/PageTemplate/Page.xaml`, immediately after the existing `<solana:ImportMethodPopupView ZIndex="10" />` line:

```xml
            <!-- Hosted here for the same reason as the popup above: the import-method popup
                 is raised from anywhere, and its Mobile Wallet Adapter option now continues
                 into this popup in place rather than into a page that hosted it. -->
            <solana:ConnectMwaPopupView ZIndex="10" />
```

- [ ] **Step 2: Remove the per-page copies**

In `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaBalancesPage.xaml`, delete the `<solana:ConnectMwaPopupView />` line from `PopupContent`, leaving:

```xml
    <template:PageTemplate.PopupContent>
        <address:AddressQrCodeView />
        <!-- ImportMethodPopupView and ConnectMwaPopupView are supplied by the page template. -->
        <solana:CreateSolanaMnemonicsPopupView />
        <solana:EnterSolanaMnemonicsPopupView />
    </template:PageTemplate.PopupContent>
```

In `realXmarketPlutoFramework/PlutoFramework/Components/Keys/CreateNewKeyPage.xaml`:

```xml
    <page:PageTemplate.PopupContent>
        <!-- ConnectMwaPopupView is supplied by the page template. -->
        <solana:CreateSolanaMnemonicsPopupView />
        <solana:EnterSolanaMnemonicsPopupView />
    </page:PageTemplate.PopupContent>
```

In `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml`:

```xml
    <template:PageTemplate.PopupContent>
        <account:ImportWarningPopup />

        <!-- The seed-phrase import continues in a popup over this page, because saving the
             phrase needs the password that was just set. -->
        <solana:EnterSolanaMnemonicsPopupView />
    </template:PageTemplate.PopupContent>
```

- [ ] **Step 3: Add the popup to `WelcomePage`**

`WelcomePage` is a plain `ContentPage`, not a `PageTemplate`, so it hosts its own popups. In `XcavateMobileApp/Pages/WelcomePage.xaml`, after `<solana:ImportMethodPopupView />`:

```xml
        <!-- Not a PageTemplate page, so the MWA popup the import-method popup opens into has
             to be declared here. Onboarding connects the wallet from this page, before there
             is any password to set. -->
        <solana:ConnectMwaPopupView />
```

- [ ] **Step 4: Build**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: build succeeded, 0 errors.

- [ ] **Step 5: Manual check (Android device with a wallet app installed)**

The MWA flow still runs in its old order at this point — this step is only checking that the popup still appears exactly once, everywhere it used to.

1. Settings → keys → Solana MWA. Expected: **one** popup card, not two stacked.
2. Solana balances page with no Solana account → Import → wallet app. Expected: one popup card.
3. Fresh install → Import Account → wallet app → password page. Expected: one popup card over the password page.

- [ ] **Step 6: Commit**

```bash
git -C realXmarketPlutoFramework add PlutoFramework/Templates/PageTemplate/Page.xaml PlutoFramework/Components/Solana/SolanaBalancesPage.xaml PlutoFramework/Components/Keys/CreateNewKeyPage.xaml PlutoFramework/Components/Password/SetupPasswordPage.xaml
git -C realXmarketPlutoFramework commit -m "refactor: host the MWA popup in the page template

The import-method popup is raised from anywhere, and its wallet option is
about to open this popup in place instead of on a page it navigates to. The
per-page copies go with it - the view model is shared, so two instances would
show two cards.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"

git add realXmarketPlutoFramework XcavateMobileApp/Pages/WelcomePage.xaml
git commit -m "fix: host the MWA popup on the welcome page

Not a PageTemplate page, so it does not get the template's copy.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: Reorder the import flows

**Files:**
- Modify: `XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs:116-168`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml`

**Interfaces:**
- Consumes: `ImportSolanaWalletPage` (Task 3) and `ConnectMwaPopupViewModel.Completed` taking a `SolanaMwaKey` (Task 4); the popup hosting from Task 5.
- Produces: no new API. `ImportAccountCoordinator.StartAsync` and `ContinueAsync` keep their signatures.

This is the task that actually changes what the user sees.

- [ ] **Step 1: Rewrite the import routing**

In `XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs`, replace `ShowImportMethodPopupAsync`, `ShowEnterSolanaMnemonicsPopupAsync` and `ShowConnectMwaPopupAsync` with:

```csharp
    /// <summary>
    /// Asks how the account arrives, then continues into whichever flow can ask for a
    /// password at the right moment for it.
    /// </summary>
    /// <remarks>
    /// Saving any key - a phrase or an MWA auth token - reads the stored password to encrypt
    /// it, so a password still has to exist before the save. What differs is where it is
    /// asked for: a phrase import collects it on the same screen as the phrase, and a wallet
    /// connection collects it after the wallet has approved.
    /// </remarks>
    private Task ShowImportMethodPopupAsync()
    {
        var popup = DependencyService.Get<ImportMethodPopupViewModel>();

        popup.SeedPhraseChosen = () => _navigationService.NavigateToAsync(new ImportSolanaWalletPage
        {
            // The page saves the password and the phrase itself, in that order.
            Navigation = FinishOnboardingAsync,
        });

        popup.MwaChosen = ShowConnectMwaPopupAsync;

        popup.IsVisible = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens over whatever page raised the import-method popup - the page template hosts it -
    /// so the wallet is connected before anything is asked of the user.
    /// </summary>
    private Task ShowConnectMwaPopupAsync()
    {
        var popup = DependencyService.Get<ConnectMwaPopupViewModel>();

        // The popup does not persist the authorization. It is held here, in memory only,
        // until the password that encrypts it exists. Abandoning the flow at the password
        // page therefore saves nothing, and the user reconnects on the next attempt.
        popup.Completed = (key) => _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = async () =>
            {
                await KeysModel.SaveSolanaMwaKeyAsync(key);

                await FinishOnboardingAsync();
            },
        });

        popup.IsVisible = true;

        return Task.CompletedTask;
    }
```

`ShowConnectMwaPopupAsync` is now an instance method because it navigates through `_navigationService`. Confirm `using PlutoFrameworkCore.Keys;` is present if the compiler asks for it.

- [ ] **Step 2: Update the flow-mode comment on `ContinueSetupPasswordAsync`**

The XML comment on `ContinueSetupPasswordAsync` says "Both flows re-run the password step". The Import flow no longer re-runs it — it re-runs the import-method popup, which leads to whichever screen asks for the password. Replace the comment:

```csharp
    /// <summary>
    /// Resumes onboarding into the flow the user originally chose. Create re-runs the
    /// password page; Import re-runs the method popup, which is where its password step now
    /// lives - on the import page for a phrase, after the wallet for MWA.
    /// </summary>
```

Also update the remark on `FLOW_MODE_KEY`, which claims "Both flows sit at `OnboardingStage.SetupPassword` until onboarding finishes". That is still true, so leave the remark itself alone — verify it reads correctly and change nothing if so.

- [ ] **Step 3: Drop the last Solana popup from `SetupPasswordPage`**

Nothing routes the seed-phrase import through this page any more. In `realXmarketPlutoFramework/PlutoFramework/Components/Password/SetupPasswordPage.xaml`, reduce `PopupContent` to:

```xml
    <template:PageTemplate.PopupContent>
        <account:ImportWarningPopup />
    </template:PageTemplate.PopupContent>
```

Then remove the now-unused `xmlns:solana` declaration from the root element.

- [ ] **Step 4: Build**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: build succeeded, 0 errors.

- [ ] **Step 5: Run the tests**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj`
Expected: PASS, including the tests added in Tasks 1 and 2.

- [ ] **Step 6: Manual check — all four paths**

On an Android device with a wallet app installed. Clear app data between runs so each starts with no account.

1. **Import with a seed phrase.** Welcome → Import Account → Seed phrase. Expected: the combined screen appears with **no password page before it**; the address preview matches the wallet being imported; Continue lands in the app on the imported account.
2. **Import over MWA.** Welcome → Import Account → wallet app. Expected: the wallet approval comes **first**; the password page comes after; completing it lands in the app on the connected account, and the account survives an app restart.
3. **Create.** Welcome → Create Account. Expected: unchanged — password page, then straight into the app.
4. **Interrupt the MWA flow.** Repeat 2, but kill the app on the password page. Reopen. Expected: onboarding resumes at the import-method popup, no account exists, and reconnecting works.

- [ ] **Step 7: Commit**

```bash
git -C realXmarketPlutoFramework add PlutoFramework/Components/Password/SetupPasswordPage.xaml
git -C realXmarketPlutoFramework commit -m "refactor: drop the seed-phrase popup from the password page

The seed-phrase import has its own page now.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"

git add realXmarketPlutoFramework XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs
git commit -m "feat: ask for the onboarding password where each flow needs it

A seed-phrase import collects the phrase and the password on one screen. An
MWA import connects the wallet first and asks for a password afterwards.
Creating an account is unchanged.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Verification of the whole change

After Task 6, from a clean install:

- `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android` — succeeds.
- `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj` — passes.
- The four manual paths in Task 6 Step 6 behave as described.
- The post-onboarding add-account paths from Task 4 Step 7 still save immediately and still ask for no password.
