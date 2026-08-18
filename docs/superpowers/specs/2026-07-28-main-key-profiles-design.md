# Solana profiles and a main-key setting

Date: 2026-07-28

## Problem

Public profiles are Substrate-only, but new accounts are Solana-only.

`App.GenerateNewAccountAsync` creates a Solana key and nothing else. `XcavateProfileService`
reads `KeysModel.GetSubstrateKey()`, which returns the literal placeholder
`"Substrate key does not exist"` when there is no Substrate key, and writes through
`KeysModel.GetAccountAsync()`, which returns null and makes `RegisterProfileAsync` return
false without telling anyone. So every user onboarded since the Solana switch has no working
public profile.

The main menu has the same split. `MainMenuPageViewModel.Address` reads
`PreferencesModel.PUBLIC_KEY`, which only the Substrate path ever writes, so `IsLoggedIn` is
false and `UserView` — name, address, profile picture, role badges — is hidden outright for
Solana-only users.

Users can hold both a Substrate and a Solana key at once. Nothing currently decides which of
them the app should treat as the user's identity.

## What we are building

1. A **main key** setting: Solana or Polkadot, defaulting to Solana, stored in Preferences.
2. A resolution layer that turns that preference plus the keys that actually exist into one
   address and one request signer.
3. Profile read/write on either chain, through the `IRequestSigner` overloads of
   `XcavateProfileClient`.
4. Main menu and profile pages driven by the resolved key rather than the Substrate key.
5. X25519 encryption keys for Solana accounts, which the profile API requires and Solana
   onboarding never creates.

### Out of scope

Chain-specific surfaces keep using their own chain's key and are not touched: NFT pallet
interactions, the XcavatePaseo faucet, Identity, AzeroID, Hydration DCA and omnipool,
`SubstrateAddressView`, and the balance pages. These take a Substrate address by nature —
handing them a Solana one would throw in `Utils.GetPublicKeyFrom`. They are already gated on
a Substrate key existing or are unreachable without one.

Role badges and KYC stay pinned to the Substrate key for the same reason: roles come from a
XcavatePaseo pallet query and Sumsub applicants are keyed by SS58 address.

## Package change

`PlutoFrameworkCore.csproj`: `XcavateProfileApiClient` **1.0.50 → 1.0.61**.

Do **not** also reference `XcavateProfileApiSolanaClient`. Verified by diffing the two
packages' public API surfaces at 1.0.61: the Solana package contains zero types the other
lacks, in identical namespaces, and both READMEs state that referencing both is unsupported.
Referencing both produces CS0433 ambiguity on every shared type. The Solana package's only
advantage is dropping `Substrate.NET.API`, which `PlutoFrameworkCore` needs regardless for
the whole Polkadot side.

1.0.61 is what introduces `IRequestSigner`, `SolanaRequestSigner`, `SubstrateRequestSigner`
and `SolanaSignatureScheme`. 1.0.50 has none of them.

The signer contract, confirmed against the shipped assembly:

```csharp
public interface IRequestSigner
{
    string Address { get; }
    Task<byte[]> SignAsync(string payload);
    string EncodeSignature(byte[] signature);
}
```

`SignAsync` returning a `Task` is what makes Mobile Wallet Adapter viable — a signature there
needs a round trip to the wallet app.

## Components

### `PlutoFrameworkCore/Xcavate/MainKeyOptions.cs`

The decision logic, kept free of MAUI so it can be unit tested. `PlutoFrameworkTests`
references `PlutoFrameworkCore` only. This is the same split `SolanaNetworkOptions` and
`SolanaNetworkModel` already use.

```csharp
public enum MainKeyChain { Solana, Polkadot }

public static class MainKeyOptions
{
    public static MainKeyChain Default => MainKeyChain.Solana;

    /// The preference reconciled with the keys that exist. Null when there are none.
    public static MainKeyChain? Resolve(MainKeyChain preferred, bool hasSolana, bool hasSubstrate);
}
```

`Resolve` returns `preferred` when that chain has a key, the other chain when it does not but
the other does, and null when neither does.

Separating the preference from the resolved value is what keeps two groups of users working
without either of them opening Settings: a Solana-only user gets Solana because it is the
default, and a user onboarded before the Solana switch — holding only a Substrate key — gets
Polkadot despite the Solana default.

### `PlutoFramework/Model/MainKeyModel.cs`

The I/O half. Modeled on `SolanaNetworkModel`: static, Preferences-backed, writes through
immediately, raises an event on change.

```csharp
public static class MainKeyModel
{
    public static event EventHandler<MainKeyChain>? ChainChanged;

    public static MainKeyChain SelectedChain { get; set; }   // SETTINGS_MAIN_KEY_CHAIN
    public static MainKeyChain? ResolvedChain { get; }

    public static string? GetAddress();
    public static Task<IRequestSigner?> GetSignerAsync(string reason, CancellationToken token);
}
```

`ResolvedChain` feeds `MainKeyOptions.Resolve` from `KeysModel.HasSolanaKey()` and
`KeysModel.HasSubstrateKey()`, both of which read Preferences synchronously.

`GetAddress` returns `KeysModel.GetSolanaAddress()` or `KeysModel.GetSubstrateKey()` for the
resolved chain, and null when there is no key. It never returns the
`"Substrate key does not exist"` placeholder.

`GetSignerAsync` unlocks a key and can therefore prompt. It returns null when the user
cancels or unlocking fails.

New constant in `PreferencesModel`: `SETTINGS_MAIN_KEY_CHAIN = "settingsMainKeyChain"`.

### `PlutoFramework/Model/Xcavate/Profile/SolanaAccountRequestSigner.cs`

```csharp
internal sealed class SolanaAccountRequestSigner(PlutoFrameworkSolanaAccount account, string reason)
    : IRequestSigner
{
    public string Address => account.Address;

    public Task<byte[]> SignAsync(string payload) =>
        account.SignMessageAsync(Encoding.UTF8.GetBytes(payload), reason, CancellationToken.None);

    public string EncodeSignature(byte[] signature) => SolanaBase58.Encode(signature);
}
```

Signing the raw UTF-8 payload unhashed, and base58-encoding the result, is exactly what the
server's `SolanaSignatureScheme` verifies.

Wrapping `PlutoFrameworkSolanaAccount` rather than using the package's own
`SolanaRequestSigner` is what makes MWA work: `SolanaRequestSigner`'s only constructor takes a
`Solnet.Wallet.Account`, and an MWA wallet never surrenders a private key to produce one.
`PlutoFrameworkSolanaAccount` already abstracts local and remote signing behind
`SignMessageAsync`.

The Polkadot path uses the package's `SubstrateRequestSigner(Account)`, so both chains reach
the client through the same interface.

### `KeysModel.EnsureEncryptionX25519KeyAsync`

`Profile.X25519Key` is required by the API, and no Solana onboarding path creates one —
`CreateSolanaMnemonicsPopupViewModel`, `EnterSolanaMnemonicsPopupViewModel` and the MWA
callback all only call `SaveSolanaMnemonicKeyAsync` / `SaveSolanaMwaKeyAsync`.

```csharp
/// Ensures an X25519 encryption key exists. Derives it from the seed phrase when there is
/// one, so it is recoverable from the same backup as the account, and generates a fresh
/// one for MWA wallets, which keep no phrase on the device.
public static Task EnsureEncryptionX25519KeyAsync(string reason, string? mnemonics = null);
```

| Solana key type | Source |
| --- | --- |
| `SolanaMnemonic` | `SaveEncryptionX25519KeyAsync(mnemonics)` — the existing BIP39 → ed25519 seed → SHA-512 → RFC7748-clamp derivation at `KeysModel.cs:343`, the same routine written for Polkadot mnemonics. |
| `SolanaMwa` | `GenerateNewEncryptionX25519KeyAsync()` — random. |

The derived key is an independent key from the same phrase. It is not the Solana account key
and does not correspond to anything on the Solana side. That matches the Polkadot behaviour.

`Keyring.AddFromMnemonic(mnemonics, META, KeyType.Ed25519)` is plain BIP39 and accepts a
Solana phrase unchanged.

**It must no-op when a key already exists.** `SaveEncryptionX25519KeyAsync` deletes every
`EncryptionX25519` key before writing, so calling it unguarded when a Substrate user later
adds a Solana key would silently destroy the messaging key they already have. `Ensure` checks
`KeysDatabase` first and returns immediately if one is present.

Called from two places:

- `SaveSolanaMnemonicKeyAsync` and `SaveSolanaMwaKeyAsync`, passing the phrase already in
  hand so there is no second unlock prompt. Covers every new account and every import.
- `XcavateProfileService.RegisterProfileAsync`, before it reads the key. This is what repairs
  users already onboarded Solana-only, who have no key and cannot be given one retroactively
  at save time.

### `XcavateProfileService`

- `GetProfileAsync` takes its address from `MainKeyModel.GetAddress()` and returns null when
  there is no key, rather than querying the placeholder string.
- `RegisterProfileAsync` calls `EnsureEncryptionX25519KeyAsync`, then gets an `IRequestSigner`
  from `MainKeyModel`, and passes it to `UploadImageAsync` and `UpdateProfileAsync` through
  their `IRequestSigner` overloads. `Profile.Ss58Address` is set to the signer's address; the
  field holds a Solana base58 address unchanged, which is what the API expects.
- The existing `FullPageLoadingViewModel` messages stay, with the signing step named so the
  wallet prompt has context.

### Main menu

`MainMenuPageViewModel`:

- `Address` comes from `MainKeyModel.GetAddress()`. This alone un-hides `UserView`, which is
  currently invisible for every Solana-only user.
- `LoadProfileAsync` gates on an address existing instead of `KeysModel.HasSubstrateKey()`.
- `LoadAsync` keeps the role query on `KeysModel.GetSubstrateKey()` behind its existing
  `HasSubstrateKey` guard. A Solana-main user who also holds a Substrate key still gets their
  badges; a Solana-only user gets an empty list, and the `FlexLayout` renders nothing.
- Subscribes to `MainKeyModel.ChainChanged` and re-resolves, so flipping the setting and
  returning does not need an app restart. `MainMenuPage` already reloads the profile on
  appearing.

### Profile pages

No signature changes. `App.NavigateToUserPageAsync` and `ModifyUserProfilePageViewModel` both
go through `XcavateProfileService` and inherit its chain-agnosticism.

Under MWA, saving a profile with a picture costs two wallet round trips — one to sign the
image upload, one to sign the profile update. Each gets its own reason string so the prompts
say what they are for.

### Settings

New `PlutoFramework/Components/Settings/MainKeySettingsView`, copying
`SolanaNetworkSettingsView`'s structure: a `Card`, a horizontal chip segment bound to a
`BindableLayout` with `IsSelected` data triggers, and an explanatory caption. Placed above
`SolanaNetworkSettingsView` in `SettingsPage.xaml`.

```
┌────────────────────────────────────────┐
│ Main account                           │
│  ╭─────────╮  ╭──────────╮             │
│  │ Solana  │  │ Polkadot │  ← dimmed,  │
│  ╰─────────╯  ╰──────────╯    no key   │
│   selected                             │
│ Which key identifies you across the    │
│ app. Your public profile is tied to    │
│ this address.                          │
└────────────────────────────────────────┘
```

The card is always visible, so the concept is discoverable for the Solana-only majority. A
chip for a chain with no key is dimmed and not selectable — selecting it would resolve back to
the other chain anyway, which would look broken. Selection writes through immediately with no
save button, matching the network setting directly below it.

## Switching behaviour

Profiles are keyed by address on the server and nicknames are globally unique, so a user with
both keys has two independent profiles and the setting decides which one the app shows.

Switching is a silent swap. The main menu and profile pages show whatever exists for the newly
active address; when nothing does, `FullName` falls back to the `XcavateUser` first and last
name as it already does, and the edit page opens with blank fields ready to register a new
profile. No confirmation dialog, no copying between profiles.

## Testing

`PlutoFrameworkTests/MainKeyResolutionTests.cs` covers `MainKeyOptions.Resolve` across the
matrix of both preferences against all four key-availability combinations, including the two
that matter most: preferred chain missing falls back to the other, and neither present returns
null.

The rest is MAUI-bound view-model wiring, consistent with how `SolanaNetworkModel` is handled
today.

## Risks

- **Two wallet prompts per profile save under MWA.** Inherent to signing an upload and an
  update separately. Mitigated with distinct reason strings.
- **`EnsureEncryptionX25519KeyAsync` clobbering an existing key.** Guarded, and the guard is
  the first thing to check in review.
- **Users onboarded Solana-only before this change** get an X25519 key derived from their
  phrase on first profile registration. That key is recoverable from their existing backup, so
  no new backup step is needed.
