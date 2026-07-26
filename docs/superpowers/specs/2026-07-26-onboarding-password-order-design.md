# Onboarding Password Order — Design

**Date:** 2026-07-26
**Status:** Approved, ready for implementation planning
**Builds on:** [Solana balances and Solana-first onboarding](2026-07-25-solana-balances-design.md)

## Goal

Onboarding asks for a password in the wrong place for both import branches. Today every
branch is password-first, because the two save calls that persist a key read the stored
password to encrypt it. That order is an implementation detail leaking into the experience:
a user who came to import their existing wallet is asked to invent an app password before
the app has shown any sign of understanding what they came to do.

Three flows, three orders:

1. **Import with a seed phrase** — one screen that takes the phrase and the password
   together.
2. **Create a new account** — unchanged. Password, then the phrase is generated.
3. **Import over Mobile Wallet Adapter** — connect the wallet first, ask for the password
   second, save third.

The "how does the account arrive?" popup stays where it is, before all of this.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Seed-phrase branch | One page, all fields visible at once | Chosen by the product owner. Phrase, password and confirm-password with a single Continue. |
| Confirm-password field | Kept on both pages | A mistyped password is otherwise only discovered at the next unlock, when the key it encrypts is already the only copy. |
| `SetupPasswordPage` staging | Replaced with all-at-once | Chosen by the product owner. The staged password → confirm reveal would leave the two onboarding screens visibly inconsistent. |
| Create flow order | Untouched | Chosen by the product owner. |
| Password commit point (seed phrase) | Same Continue handler as the key save | The password must exist in `SecureStorage` before `SaveSolanaMnemonicKeyAsync` runs. One handler is the only ordering that has no window where a key is at rest under a password the user did not choose. |
| MWA key handoff | `ConnectMwaPopupViewModel` stops saving; reports the key to its caller | Puts the save under the caller's control, which is what lets onboarding defer it past a page. No mode flag: a `SaveImmediately` bool would leave the popup with two behaviours to reason about at three call sites. |
| Shared phrase and password UI | Extracted into `SolanaMnemonicsEntryView` and `SetPasswordView` | Chosen by the product owner. Address derivation and the four password rules each exist once. |
| MWA popup hosting | Moves into the page template | The popup now opens over whatever page raised the import-method popup, instead of over a page the flow navigated to. |
| Abandoned MWA authorization | Left un-revoked | Matches what already happens when the save fails today. Revoking was considered and declined. |

## Rejected alternatives

**Save the key under a temporary password, re-encrypt after the real one is set.** Would
keep the popups self-contained, at the cost of two encryption states for one key and a
window where the key is at rest under a password the user never chose. The failure mode —
an interrupted re-encryption leaving a key readable under a throwaway secret — is worse
than anything it saves.

**Ask for the password in the import popup itself.** Keeps the flow inside one popup, but a
bottom card is the wrong surface for four live requirement labels plus two entries plus a
phrase editor, and the popup is shared with the post-onboarding add-account paths, which
must not ask for a password at all.

## Flows

| Branch | Order |
|---|---|
| Create | `SetupPasswordPage` → generate phrase → save → finish |
| Import / seed phrase | `ImportSolanaWalletPage` → save password, save key → finish |
| Import / MWA | `ConnectMwaPopup` (connect only) → `SetupPasswordPage` → save password, save connected key → finish |

The Create column is the current behaviour, restated to make the contrast explicit.

## Components

### New — `PlutoFramework`

**`SetPasswordView`** (`Components/Password/`)
The two entries, their eyeball toggles, the four live requirement labels and the mismatch
label. Lifted verbatim out of `SetupPasswordPage` code-behind. Exposes `Password` and
`IsValid`, and raises a change notification so a host page can drive its own Continue
button. Knows nothing about storage or navigation.

**`SolanaMnemonicsEntryView` + `SolanaMnemonicsEntryViewModel`** (`Components/Solana/`)
The phrase editor, the live address preview and the invalid-phrase label. Lifted out of
`EnterSolanaMnemonicsPopupViewModel`, which afterwards holds one as an `Entry` property and
delegates `IsValid`, `Mnemonics` and `IncorrectMnemonicsEntered` to it. The view carries no
`BindingContext` of its own, so a page can bind it to its own instance while the popup binds
it to the shared one. The view model is a plain property, not a `DependencyService`
registration — only the popup's own instance is shared, and its `SetToDefault` must clear the
entry, or a seed phrase survives in the shared instance for whoever opens the popup next.

The address preview is the reason this is extracted rather than copied: a mnemonic imported
under the wrong derivation yields a valid but empty account, and the preview is the only
thing standing between the user and that outcome. It must not exist in two versions.

**`ImportSolanaWalletPage`** (`Components/Solana/`)
A `PageTemplate` page hosting `SolanaMnemonicsEntryView`, `SetPasswordView` and a Continue
button. Continue is enabled only when the phrase is valid, the password satisfies all four
rules, and the two password fields match. Its handler, in order:

1. `PasswordSetupModel.SaveNewPasswordAsync(password)`
2. `KeysModel.SaveSolanaMnemonicKeyAsync(phrase)`
3. the injected `Navigation` callback

Takes a `Navigation` callback the way `SetupPasswordPage` does, so the page stays free of
onboarding-stage knowledge.

**`PasswordSetupModel.SaveNewPasswordAsync`** (`Model/`)
The `SecureStorage` write plus `KeysModel.RegisterBiometricAuthenticationAsync()`, currently
inline in `SetupPasswordPage`. Both pages call it.

### Changed — `PlutoFramework`

**`SolanaMwaModel`** — `ConnectAndSaveAsync` becomes `ConnectAsync`, returning the
`SolanaMwaKey` without persisting it.

**`ConnectMwaPopupViewModel`** — `Completed` becomes `Func<SolanaMwaKey, Task>`. The view
model no longer persists anything. The "Connected to …" toast stays where it is: it reports
a connection, which is true at that point regardless of whether a save follows.

**`SolanaNoAccountView`, `NewKeyView`** — their MWA callbacks call
`KeysModel.SaveSolanaMwaKeyAsync(key)` before doing what they already do. Both run with a
password already stored, so their behaviour is unchanged. The comments in
`SolanaNoAccountView.ImportAsync` that explain why the callbacks do not save need updating —
they now describe the opposite of what the code does.

**`SetupPasswordPage`** — password and confirm shown together; `_confirmStep` and its
`OnBackButtonPressed` handling go away. Body becomes `SetPasswordView` plus the Continue
button. Drops `EnterSolanaMnemonicsPopupView` and `ConnectMwaPopupView` from its
`PopupContent`; neither flow ends here any more.

**`EnterSolanaMnemonicsPopupView`** — its inline editor, preview and error label are replaced
by `<solana:SolanaMnemonicsEntryView BindingContext="{Binding Entry}" />`.

**Popup hosting** — `ConnectMwaPopupView` moves into `Templates/PageTemplate/Page.xaml`
beside `ImportMethodPopupView`, for the reason already documented there: the import-method
popup is raised from anywhere, and its MWA button now continues into this popup instead of
into a page that hosted it. The per-page copies in `SolanaBalancesPage.xaml` and
`CreateNewKeyPage.xaml` are removed — the view binds to a shared view model, so two live
instances would show two popups. `WelcomePage.xaml` is not a template page and gets its own
copy, next to the `ImportMethodPopupView` it already hosts.

### Changed — `XcavateMobileApp`

**`ImportAccountCoordinator`** — `ShowImportMethodPopupAsync` routes:

- `SeedPhraseChosen` → `ImportSolanaWalletPage { Navigation = FinishOnboardingAsync }`
- `MwaChosen` → `ConnectMwaPopup { Completed = key => SetupPasswordPage { Navigation = save key, finish } }`

`CreateSolanaAccountAsync` and `ContinueSetupPasswordAsync` are unchanged. The flow-mode
preference already resumes an interrupted import into the import-method popup, and that
stays correct for both branches under the new order.

The class comments describing why both branches set the password first are now false and
must be rewritten rather than deleted — the constraint they document (the saves read the
stored password) still holds and still explains the ordering inside each handler.

## Failure and interruption

| Situation | Behaviour |
|---|---|
| Combined page abandoned | Nothing written. Resume re-shows the import-method popup. |
| MWA connected, app killed before password | The key was only ever in memory, so nothing is written. Resume re-shows the import-method popup; the user reconnects. The abandoned wallet-side authorization is left un-revoked. |
| Invalid phrase | Existing inline "That is not a valid seed phrase." |
| Passwords mismatch | Existing mismatch label; Continue stays disabled until the rules pass. |
| Key save throws on the combined page | Its own message — "Could not save your wallet. Please try again." The phrase is already validated by that point, so reusing the popup's invalid-phrase catch would tell the user their correct phrase is wrong. |
| MWA connect fails | Unchanged: the popup stays open on its error and the user retries. |

The seed-phrase page commits the password before the key. A failure between the two leaves a
password stored with no key — which is exactly the state the Create flow passes through on
every run, and which the retry resolves.

## Verification

`PlutoFrameworkTests` references only `PlutoFrameworkCore`. Every component in this design
lives in the MAUI `PlutoFramework` project or in `XcavateMobileApp`, so none of it is
reachable from the existing suite, and no automated coverage is proposed. Nothing here
changes derivation, encryption or key storage, all of which stay covered by
`SolanaMnemonicsTests` and `SolanaKeyTests`.

Verification is a build plus four manual runs:

1. Import with a seed phrase — phrase and password on one screen, address preview matches
   the wallet being imported, app opens on the imported account.
2. Import over MWA (Android) — wallet approval comes first, password second, app opens on
   the connected account.
3. Create — unchanged order, password then a generated account.
4. Post-onboarding add-account, both MWA entry points (`SolanaNoAccountView` and
   `NewKeyView`) — still saves immediately, still asks for no password.

Run 4 is the regression that matters: it is the path the `ConnectMwaPopupViewModel` change
puts at risk.
