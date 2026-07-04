# UI/UX Practices in the realXmarketMobileApp (C# / .NET MAUI)

This document documents the established UI/UX practices observed in the
`XcavateMobileApp` (the main app) and the `PlutoFramework` (the shared
framework submodule). Only actively reachable pages, views, and components
are cited; generated code (the `Generated/` tree) and navigationally dead
code (Bohemia, CalamarView, etc.) are explicitly excluded.

**Repository:** https://github.com/XcavateBlockchain/realXmarketMobileApp
**Tech stack:** .NET MAUI, CommunityToolkit.Mvvm, XAML code-behind, MVVM

---

## 1. Design system tokens

### 1.1 Colour palette

All colours are declared as shared resources in
`Resources/Styles/Colors.xaml` and overridden/extended in the framework's
`Resources/Styles/DefaultStyles.xaml`. The palette is:

| Token | Light value | Dark value | Semantic use |
|---|---|---|---|
| `Primary` | `#3B4F74` (steel blue) | `#3B4F74` | Buttons, active nav, links |
| `PrimaryUnimportant` | `#627290` | `#627290` | Disabled states, secondary text |
| `Secondary` | `#DFD8F7` | `#DFD8F7` | Toggle-on state, highlights |
| `Tertiary` | `#65def1` | `#65def1` | Accent surfaces |
| `Positive` | `#357461` (leaf green) | `#357461` | Success indicators, confirmed |
| `Negative` / `DangerousRed` | `#dc7da6` (muted pink) | `#dc7da6` | Errors, rejections, warnings |
| `Gray100` | `#E1E1E1` | `#E1E1E1` | Borders, dividers |
| `Gray300` | `#ACACAC` | `#ACACAC` | Placeholder text, disabled |
| `Gray500` | `#6E6E6E` | `#6E6E6E` | Secondary text |
| `Gray900` | `#212121` | `#212121` | Primary text |
| `Gray950` | `#141414` | `#141414` | Dark-mode backgrounds |
| `White` | `#FFFFFF` | `#FFFFFF` | Light-mode background |
| `Black` | `#000000` | `#000000` | Dark-mode background (`#0a0a0a` or `#000000`) |

**Practice:** Every colour that affects the UI is a keyed resource. Hardcoded
hex literals appear only in rare one-off decorations (e.g. the slider
gradient stops in `SliderView.xaml`).

### 1.2 Typography

- Custom font: `xcavatefont.ttf` registered as `"XcavateFont"` in
  `MauiProgram.cs`.
- All default `Label`, `Button`, `Entry`, `Editor`, `Picker`, etc. styles
  set `FontFamily="XcavateFont"`.
- Base font size across the app: **14** (styles), **20** for section headers,
  **25** for welcome text, **30** for property names and large values.
- `FontAutoScalingEnabled="False"` on `Button`, `Label`, `Entry`, `Editor`
  to prevent platform text-size adjustments from breaking layout.
- **Bold** (`FontAttributes="Bold"`) is used for all values, titles, and
  interactive text. Thin / medium is not used anywhere.

### 1.3 Spacing and sizing

| Constant | Value | Use |
|---|---|---|
| `Gap` | 8 | Small inline gaps (e.g. button row spacing) |
| `CardCornerRadius` | 10 | Standard cards; 15 for framework-level cards |
| `ButtonCornerRadius` | 24 (radius) → full pill on 48 px height buttons |
| `ButtonHeight` | 48 | All primary / secondary buttons |
| `ThinCardCornerRadius` | 10 | Slim cards (form rows, table cells) |
| Page horizontal padding | 20 | Standard margin on all pages |
| Vertical spacing between items | 15 | `Spacing="15"` on page `VerticalStackLayout`s |

### 1.4 Shadows

- Cards: `CardShadow` — `Radius=4, Opacity=0.25, Offset=(0,0)` on dark mode;
  `Radius=20, Opacity=0.1, Offset=(0,2)` on light mode (from framework).
- Bottom bar: `Offset=(0,0), Radius=2, Opacity=0.16` — a subtle divider
  shadow.
- Text cards: shadow is zeroed out by default.

### 1.5 App theming

All colours are wrapped in `AppThemeBinding Light=... Dark=...` so every
control automatically adapts. Pages set:

```xml
BackgroundColor="{AppThemeBinding Light=White, Dark=#0a0a0a}"
```

---

## 2. Architecture & navigation patterns

### 2.1 Three-shell navigation model

The app uses **three Shell instances**, each representing a distinct state:

| Shell | Entry condition | Routes |
|---|---|---|
| `OnboardingShell` | No account, first launch | `WelcomePage` |
| `NoAccountShell` | Has account but no KYC / not fully set up | `XcavateIndexedPropertyMarketplacePage`, `NoAccountMainPage`, `HelpPage`, `XcavateIndexedPropertyNoticeboardPage` |
| `XcavateAppShell` | Fully onboarded (KYC verified) | `InvestorMainPage`, `NoAccountMainPage`, `HelpPage`, `XcavateIndexedPropertyMarketplacePage`, `XcavateIndexedPropertyNoticeboardPage`, `LoggedOutPage` |

Switching between shells replaces `Application.Current.MainPage`:
```csharp
Application.Current.MainPage = new XcavateAppShell();
```

### 2.2 Navigation within pages

- `Shell.Navigation.PushAsync()` for nested page navigation (detail pages,
  forms, etc.).
- Modals via `Shell.PresentationMode="ModalNotAnimated"` (used for the
  property marketplace overlay).
- Bottom sheet / popup via `BottomPopupCard` (an `AcrylicView` overlay with
  a draggable bottom sheet).
- `OnboardingModel.SetOnboardingStage()` drives conditional UI visibility
  across pages (e.g. showing KYC banners only during onboarding).

### 2.3 Dead code (not accessible in any nav path)

The following components have no `PushAsync` or navigation call targeting
them from the active codebase:

- `BohemiaWelcomePage`, `BohemiaNftOwnedListView`, `ToJoinDaoView`
  (Bohemia DAO)
- `CalamarView`, `CalamarViewModel`
- `QuestionnaireV2ConditionsPage`, `QuestionnaireV2DeclarationPage`
  (replaced by `QuestionnairePage` with inline options)

These should be considered **deprecated / experimental** and are excluded
from this analysis.

---

## 3. UI component patterns

### 3.1 Page layout template

Every page follows the **PageTemplate** control template (defined in
`Templates/PageTemplate/Page.xaml`):

```
┌─────────────────────────────────┐
│  TopNavigationBar (optional)    │  <- from ControlTemplate
├─────────────────────────────────┤
│                                 │
│  MainContent (scrollable)       │
│  ScrollView → VerticalStack     │
│  Spacing=15, Padding=20,...     │
│                                 │
├─────────────────────────────────┤
│  Bottom popup layers (ZIndex)   │
│  └ BottomPopupCard              │
│  └ ExtrinsicStatusStack         │
│  └ TransactionAnalyzerPopup     │
│  └ WebSignPopup                 │
│  └ FullPageLoading (ZIndex=20)  │
└─────────────────────────────────┘
```

**Rule:** All global UI layers (popups, loaders, extrinsic status) are
declared inside `PageTemplate.PopupContent` in the page XAML, not in the
main content area. This keeps them stacked and Z-ordered without
intertwining with the page content.

### 3.2 Cards

Cards are the primary UI building block:

```xml
<card:ClickableCard CornerRadius="10" Shadow="..." Padding="10,0">
  <Border BackgroundColor="{AppThemeBinding Light=White, Dark=Black}">
    <ContentView />
  </Border>
</card:ClickableCard>
```

- **ClickableCard** extends `Border` with a `RoundRectangle` stroke shape.
- The `IsThin` variant uses `CornerRadius="10"` with a reduced height
  (used for form rows and table cells).
- Cards use a `CardShadow` (soft, subtle shadow).
- All card content is wrapped in a `ContentView` that is the `View`
  dependency property, making cards composable.
- Cards are always full-width within their container (`HorizontalOptions="Fill"`).

### 3.3 Buttons

Three button variants, all using `ButtonCornerRadius=24` and `ButtonHeight=48`:

| Variant | Background | Text | Use case |
|---|---|---|---|
| `ElevatedButton` | `Primary` (`#3B4F74`) | `White` | Primary CTA |
| `BasicGrayButton` | `White` / `#FFFFFF` | `#3B4F74` | Secondary / outline |
| `PlutoWalletElevatedButton` | `Primary` | `White` + wallet icon | Wallet actions |

Disabled states use `PrimaryUnimportant` (`#627290`) for both background
and text.

**Practical note:** Buttons are `FontAttributes="Bold"` at 14 px, and
`FontAutoScalingEnabled="False"` to prevent platform accessibility
scaling from breaking the fixed-height layout.

### 3.4 Cell widgets (2-column key-value cards)

The `XcavateCell` is a reusable 2-line component used for dashboard metrics:

```
┌───────────────────────────────┐
│  Listing price       >        │  <- Title left, Value right (bold, Primary colour)
│  Gross yield         >        │
└───────────────────────────────┘
```

- Height: 80 px.
- Layout: `AbsoluteLayout` with icon (decorative) at bottom-right, title
  at top (via `PropertyTitleWithInfoView`), value in bold Primary colour.
- Arrow at right edge (`xcavatecellarrow.png`), shown on demand.
- Wrapped in `ClickableCard` for tap support.

### 3.5 Form inputs

Form inputs are composite controls:

**FormInputView** (text input):
```
┌───────────────────────────────┐
│  Type here                     │  <- Entry inside Card shell
│                                │
└───────────────────────────────┘
```
- 40 px height, `Card` wrapper.
- Entry has `FontAttributes="Bold"`, `HeightRequest=40`.
- Optional "Max" pill button on the right (for asset amount fields).
- Spell-check and text prediction disabled (`IsSpellCheckEnabled="false"`).

**FormValueView** (read-only display):
```
┌───────────────────────────────┐
│  First name      0x1234...    │  <- 120px label column, value in monospace
└───────────────────────────────┘
```
- 40 px height, thin card.
- Label column: fixed 120 px, bold.
- Value column: monospace font (`FontFamily="SourceCode"`).
- Tap to navigate / copy support via `ClickableCard` wrapper.

### 3.6 Lists and collections

- **`CollectionView`** with `LinearItemsLayout` for property listings.
  - `ItemSizingStrategy="MeasureAllItems"` for consistent heights.
  - `RemainingItemsThresholdReachedCommand` for infinite scroll / lazy load.
  - Header: `RiskWarningView` at top.
  - Footer: loading spinner or empty-state text.
  - Wrapped in `RefreshView` with pull-to-refresh command.
- **`BindableLayout`** with `ItemTemplate` for simple vertical lists
  (e.g. asset rows on Balance page).
- Each list item is a **`PropertyThumbnailView`** — a card with:
  - Cached image (FFImageLoading) with loading placeholder.
  - Location name, property name, APY/yield, shares, price.
  - Favourite toggle (heart icon, Font Awesome).
  - Status badge (pill-shaped border with gradient background `#22888888`).

### 3.7 Top navigation bar

Custom `TopNavigationBar` (not the platform default):

```
┌─────────────────────────────────┐
│  <  Title              Edit  >  │  <- Semi-transparent bg, white text
└─────────────────────────────────┘
```
- 45 px height, `#88888888` background.
- Back arrow on left (tappable).
- Bold, white, centred title.
- Optional right-side text (e.g. "Edit") with command binding.
- Not the native `NavigationPage` bar — the app hides it and uses its
  own overlay.

### 3.8 Bottom navigation bar

Two variants, both based on `PageBottomBarView`:

**Main bottom nav** (3 tabs):
```
┌─────────┬─────────┬─────────┐
│ Account │  Help   │ Market  │  <- Icon + label, selected state
└─────────┴─────────┴─────────┘
```
- 3 columns: `*, *, *`.
- Each tab is `XcavateNavigationBarButtonView` with selected/unselected
  icon pair.
- Bound to `XcavateNavigationBarViewModel` with `Selected` property.

**Top action bar** (on main pages):
```
┌──────┬──┬──┬──┬──┐
│ Logo │ 🔔 │ 📷 │ ⋮ │  <- 5 columns: Auto, *, 35px, 35px, 35px
└──────┴──┴──┴──┴──┘
```
- Logo on left.
- Circular icon buttons (35 px, rounded, semi-transparent background).
- Commands bound to MVVM view model.

### 3.9 Stepper / progress indicator

`TopNavigationStepperBar` used in questionnaire and multi-step flows:

```
┌─────────────────────────────────┐
│  <  Step 1 of 4  [●-●-○-○]  >  │
└─────────────────────────────────┘
```
- `ProgressStepperView` renders dots (● filled, ○ empty).
- Binded to `Step` and `Steps` properties on the view model.

### 3.10 Popups and modals

Three popup types, all rendered as overlays within the page's `AbsoluteLayout`:

**BottomPopupCard** — slide-up sheet:
- 60% height (`AbsoluteLayout.LayoutBounds=".5, 1, 1, .6"`).
- Rounded top corners only (`CornerRadius="20, 20, 0, 0"`).
- Dark backdrop overlay (`AcrylicView`, opacity 0, darkening effect).
- Draggable via `PanGestureRecognizer` (drag down to dismiss).
- Thumb grabber handle at top (`2.5` radius, 100 px wide).
- Title bar with draggable title text.

**Modal popup** — inline `AbsoluteLayout` children with `ZIndex`:
- `ExtrinsicStatusStackLayout` — transaction progress toast.
- `FullPageLoadingView` (ZIndex=20) — spinner overlay.
- `BottomPillBackgroundView` / `TopPillBackgroundView` — decorative pill backgrounds.

**Full-screen modals:**
- `Shell.PresentationMode="ModalNotAnimated"` for full-screen overlays.
- Uses `Shell.PresentationMode="NotAnimated"` for smoother transitions.

---

## 4. Layout primitives

### 4.1 Layout hierarchy

The app favours **AbsoluteLayout** as the root container, then `Grid` for
structured layouts, then `VerticalStackLayout` for vertical lists:

```xml
<!-- Root: full-page overlay positioning -->
<AbsoluteLayout LayoutBounds="0.5, 0.5, 1, 1" LayoutFlags="All">
    <!-- Content: scrollable, centred -->
    <ScrollView LayoutBounds="0.5, 0.5, 1, 1" LayoutFlags="All">
        <VerticalStackLayout Spacing="15" Padding="20, 80, 20, 110">
            <!-- Page items -->
        </VerticalStackLayout>
    </ScrollView>
    <!-- Popups: siblings, Z-ordered -->
    <card:BottomPopupCard ZIndex="5"/>
</AbsoluteLayout>
```

### 4.2 Grid patterns

**2-column grids** (very common, for paired data):
```xml
<Grid ColumnDefinitions="*,*" ColumnSpacing="15">
    <Cell Grid.Column="0" />
    <Cell Grid.Column="1" />
</Grid>
```
Used in: 2x2 metric cards, 2x1 property stats, property list metadata rows.

**Flexible grids** for label-value rows:
```xml
<Grid ColumnDefinitions="120,*">
    <Label Grid.Column="0" />
    <Label Grid.Column="1" />
</Grid>
```

### 4.3 Image handling

- Images use `FFImageLoading.Maui.CachedImage` for caching.
- Loading placeholder: `xcavateloading.gif` (animated spinner).
- Fallback: `noimage.png`.
- Aspect ratios: `AspectFill` for card images, `AspectFit` for icons.
- Dark/light image variants via `AppThemeBinding` in `Source`.

### 4.4 Status badges

Pill-shaped status indicators on property cards:

```xml
<Border BackgroundColor="#22888888" StrokeThickness="0">
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="5" />
    </Border.StrokeShape>
    <Label Text="Sold Out" Margin="5,0" />
</Border>
```
- Semi-transparent grey background (`#22888888`).
- 5 px corner radius.
- Used for property status, listing status, and KYC status.

---

## 5. UX interaction patterns

### 5.1 Pull-to-refresh

Every main list page wraps its `CollectionView` or `ScrollView` in a
`RefreshView` with `Command` bound to a `RelayCommand` on the view model.

### 5.2 Infinite scroll (lazy loading)

The property marketplace uses:
```xml
RemainingItemsThreshold="0"
RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"
```
Combined with `OnMainScrollViewScrolled` event handler that checks if the
user is within 280 px of the bottom before loading.

### 5.3 Loading states

Three concurrent loading patterns:

1. **Full-page loader** (`FullPageLoadingView`, ZIndex=20):
   - Animated GIF or `ActivityIndicator`.
   - Shows during account creation, data fetch, signing.
2. **Item-level loader** (`LoadingItemView`):
   - Grey placeholder card in list footer during pagination.
3. **Skeleton / empty state**:
   - `TransparentItemView` or `ErrorItemView` for no-results state.
   - `Label` with `#A6A6A6` text for "Your properties will appear here".

### 5.4 Error handling

- `BadInternetConnectionPage` — a full-page error shown on network failures.
- All navigation to error pages goes through `try/catch` blocks:
  ```csharp
  catch (Exception ex) {
      Console.WriteLine(ex);
      await Navigation.PushAsync(new BadInternetConnectionPage());
  }
  ```
- `Console.WriteLine` used for logging (no structured logging framework).

### 5.5 Transaction status

`ExtrinsicStatusStackLayout` provides persistent toast notifications for
blockchain transaction lifecycle:

```
┌─────────────────────────────────┐
│  ⏳ Signing...  [Cancel]       │  <- Grey background, white text
│  ✓ Confirmed                    │  <- Green on confirm
│  ✗ Failed                       │  <- Red on failure
└─────────────────────────────────┘
```
- Appears at the bottom of the page.
- Auto-dismisses on confirm; requires manual dismiss on failure.
- Stacked: multiple transactions show multiple toasts.

### 5.6 Risk warning banner

Every main page shows a `RiskWarningView` at the top:

```
Don't invest unless you're prepared to lose all the money you invest.
This is a high-risk investment and you should not expect to be protected
if something goes wrong. Take 2 mins to learn more.  [tap link]
```
- Bold text, blue hyperlink.
- Always present on authenticated pages.

### 5.7 Gesture recognizers

Tap interactions are handled via XAML `GestureRecognizers`, not code-behind
events:

```xml
<Image ...>
    <Image.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding FavouriteCommand}" />
    </Image.GestureRecognizers>
</Image>
```

Pan gestures for draggable popups:
```xml
<PanGestureRecognizer PanUpdated="OnPanUpdated" />
```

### 5.8 Custom fonts via FontImageSource

Icons are embedded fonts, not asset images:

```xml
<Image Source="{FontImageSource Color=White, Glyph='&#xf004;', 
              FontFamily='FontAwesome', Size=50}" />
```
FontAwesome is used for heart/favourite icons. The app's custom font
(`XcavateFont`) is used for all text.

---

## 6. MVVM code-behind patterns

### 6.1 Observable properties

Using `CommunityToolkit.Mvvm`:

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    // Auto-generates: public string Title { get => _title; set => SetProperty(ref _title, value); }
}
```

Computed properties derived from observables:

```csharp
public string TotalPrice => Items.Sum(i => i.Price).ToString();
// Not an [ObservableProperty] — recomputed on every access
```

### 6.2 Commands

```csharp
[RelayCommand]
private async Task DoSomethingAsync() { ... }
// Generates: public IRelayCommand DoSomethingCommand => _doSomethingCommand;

[RelayCommand]
public void DoSynchronous() { ... }
// Generates: public IRelayCommand DoSynchronousCommand => _doSynchronousCommand;
```

Async relay commands bind directly to XAML `Command` properties.

### 6.3 Dependency service for page view models

Page view models are obtained from the DI container via:
```csharp
viewModel = DependencyService.Get<InvestorMainPageViewModel>();
BindingContext = viewModel;
```

The `DependencyService` is set up in the framework's
`UsePlutoFrameworkMinimal()` extension method.

### 6.4 Page code-behind responsibilities

The `.xaml.cs` code-behind for pages does NOT set bindings. Its role is:

1. `NavigationPage.SetHasNavigationBar(this, false)` /
   `Shell.SetNavBarIsVisible(this, false)` — hide platform nav bar.
2. `BindingContext = viewModel` — attach the view model.
3. `Loaded += OnLoaded` — defer async initialization until layout is done.
4. `OnDisappearing` — cancel pending operations.
5. Exposing named views (e.g. `balanceCellView`) for cross-component
   communication.

---

## 7. Accessibility & platform considerations

### 7.1 Accessibility

- `AutomationProperties.IsInAccessibleTree="true"` on primary buttons.
- No `AccessibilityLabel` or `ContentDescription` found on images or icons.
- Font scaling is disabled globally (`FontAutoScalingEnabled="False"`).

### 7.2 Platform adaptation

- `AppThemeBinding` for colours across all UI elements.
- `OnPlatform` used sparingly (e.g. `Shell.ForegroundColor` for WinUI).
- `HideSoftInputOnTapped="True"` on pages to dismiss keyboard.
- Android-specific: `AndroidNotificationHelper` setup, `Android26_0_OR_GREATER`
  conditional compilation.
- iOS-specific: background service, privacy manifest (`PrivacyInfo.xcprivacy`).
- Tizen and MacCatalyst platform folders exist but are not actively used.

### 7.3 Keyboard handling

- `Entry` and `Editor` have `ClearButtonVisibility="WhileEditing"`.
- `Keyboard="Text"` for text input (not numeric, not email).
- `ReturnType="Done"` triggers dismiss.
- `OnMainScrollViewScrolled` handler also dismisses keyboard implicitly
  via `HideSoftInputOnTapped="True"` on the `ContentPage`.

---

## 8. Component catalog (active, reachable)

This table lists every component in the framework that is referenced by
actively reachable pages (i.e. pages in the nav path from the three
Shells). Components not used by any reachable page are excluded.

| Namespace | Component type | Purpose |
|---|---|---|
| `Card` | `ClickableCard`, `Card` | Reusable card container with tap support |
| `Buttons` | `ElevatedButton`, `BasicGrayButton` | Primary and secondary buttons |
| `NavigationBar` | `TopNavigationBar`, `TopNavigationStepperBar` | Page header bars |
| `Form` | `FormInputView`, `FormValueView`, `FormLargeInputView` | Text and value input/display |
| `Xcavate` | `XcavateCell`, `RiskWarningView`, `UserTypeBadgeView` | Domain-specific UI |
| `XcavateProperty` | `PropertyThumbnailView`, `SliderView` | Property listing cards |
| `Balance` | `BalanceOverviewView`, `AssetView`, `UsdBalanceView` | Wallet / balance display |
| `Account` | `NoAccountPopup`, `CreateAccountPopup` | Account state popups |
| `Extrinsic` | `ExtrinsicStatusStackLayout` | Transaction status toasts |
| `Loading` | `FullPageLoadingView` | Full-screen loading overlay |
| `CustomLayouts` | `LoadingItemView`, `TransparentItemView` | List loading/empty states |
| `Card` | `BottomPopupCard` | Draggable bottom sheet |
| `TransactionAnalyzer` | `TransactionAnalyzerConfirmationView` | Transaction review popup |
| `Password` | `EnterPasswordPopupView` | Password prompt popup |
| `NetworkSelect` | `NetworkSelectorView`, `NetworkBubbleView` | Chain/network selection |
| `TransferView` | `TransferView` | Asset transfer dialog |
| `AssetSelect` | `AssetSelectorView`, `AssetInputView` | Asset selection / input |
| `Sumsub` | `SumsubRejectedView`, `SumsubNeedsResubmitView` | KYC status banners |
| `Tabs` | `TwoTabView`, `TabsView` | Tabbed content areas |
| `Nft` | `NftImageView`, `NftThumbnailView`, `NftAttributeView` | NFT / media display |
| `Kilt` | `NoDidPopupView`, `DidListView` | DID / identity management |
| `DAppConnection` | `DAppConnectionView`, `DAppConnectionRequestView` | Wallet connection requests |
| `SearchBar` | `SearchBarView` | Search interface |
| `Staking` | `StakingDashboardView`, `StakingEntryView` | Staking management |
| `Referenda` | `ReferendaView`, `ReferendumInfoView` | Governance display |
| `Events` | `EventsListView`, `EventItemView` | Blockchain event log |
| `Keys` | `KeyListPage`, `KeyView` | Key management |
| `AddressView` | `AddressView`, `AddressQrCodeView`, `SubscanAddressView` | Wallet address display |

---

## 9. Anti-patterns observed

### 9.1 Hardcoded dimensions

Many layouts use fixed pixel values (`HeightRequest="80"`,
`AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 80"`) rather than proportional
sizing. This can break on different screen densities.

### 9.2 Console.WriteLine for logging

Production error handling uses `Console.WriteLine(ex)` which:
- Does not capture stack traces properly on all platforms.
- Is not structured and cannot be aggregated.
- May leak sensitive data in error messages.

### 9.3 No loading state on form inputs

Form inputs do not have built-in loading/skeleton states. The only loading
indication is the full-page loader, which blocks the entire screen.

### 9.4 No empty-state images

Empty states show only text (`Label` with `#A6A6A6` colour). No illustrative
empty-state graphics are present.

### 9.5 Accessibility gaps

- No `AutomationProperties.Name` on decorative images.
- No accessibility labels on icon-only buttons (bell, QR scanner, menu).
- Font scaling is disabled, which may violate WCAG 2.1 zoom requirements.

---

## 10. Summary of key principles

The realXmarketMobileApp UI is built on these principles:

1. **Token-driven design** — all colours, radii, and spacing are shared
   resources, not hardcoded.
2. **Control-templated pages** — every page uses `PageTemplate` control
   template for consistent structure.
3. **Card-based composition** — cards are the fundamental UI building block,
   wrapped in `ClickableCard` for tap support.
4. **Overlay popups** — modals, toasts, and bottom sheets are siblings
   to page content in an `AbsoluteLayout`, stacked via `ZIndex`.
5. **MVVM with CommunityToolkit** — `[ObservableProperty]` and `[RelayCommand]`
   drive all view models; code-behind is minimal (init/dispose only).
6. **AppThemeBinding everywhere** — every colour adapts to light/dark mode.
7. **Custom font enforcement** — `XcavateFont` with scaling disabled
   ensures visual consistency across platforms.
8. **Pull-to-refresh + infinite scroll** — every data list supports both.
9. **Bottom-sheet interaction** — `BottomPopupCard` with pan-to-dismiss
   is the primary interaction pattern for complex actions.
10. **Transaction status at the bottom** — extrinsic progress is persistent
    and stacked, never a transient alert.