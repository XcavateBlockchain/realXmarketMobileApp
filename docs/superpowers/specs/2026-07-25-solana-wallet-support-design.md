# Solana Wallet Support — Design

**Date:** 2026-07-25
**Status:** Approved, ready for implementation planning

## Goal

Add Solana wallet support to `KeysModel` and the surrounding key classes, in two variants:

1. **Mnemonic wallet** — a locally held BIP39 seed phrase, either generated fresh or imported by entering an existing phrase.
2. **Mobile Wallet Adapter (MWA)** — a remote signer: the user's installed wallet app (Phantom, Solflare, Backpack) holds the key, and this app holds only an authorization token.

Plus the pages needed to drive both.

**Out of scope:** the onboarding process is not modified. Solana keys are reachable only through the existing Keys pages.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Package | `Solana.Wallet` 8.7.0 | The live packages from `bmresearch/Solnet`. The `Solnet.*` ids are stale (6.1.0, July 2022) and depend on `Portable.BouncyCastle` 1.9.0, which collides with the repo's `BouncyCastle.Cryptography` 2.6.0. |
| Seed mode | `Ed25519Bip32` only | `m/44'/501'/0'/0'` — what Phantom, Solflare and Backpack show by default, and Solnet's own default. Mnemonics from `solana-keygen` will resolve to a different address than that CLI reports; accepted. |
| MWA depth | Full 2.0 client (Android) | Hand-written; no C# SDK exists. |
| Coexistence | Mutually exclusive | Mirrors the existing `Sr25519`/`PolkadotJson` precedent. One unambiguous Solana address app-wide. |
| Cluster | User-selectable in UI | Picker on the MWA connect page, persisted in `Preferences`. |

## Research findings

### Solnet package naming

The repository at `github.com/bmresearch/Solnet` publishes under the **`Solana.*`** package ids, not `Solnet.*`:

- `Solana.Wallet` 8.7.0, published 2025-11-26, authors `blockmountain, Bifrost`.
- Contains `lib/net8.0/Solnet.Wallet.dll` — the **assembly and namespace remain `Solnet.Wallet`**; only the NuGet id changed.
- Single dependency: `BifrostSecurity` 1.0.1, which is one managed DLL (`lib/net8.0/Bifrost.Security.dll`) with no native runtime assets. Safe for `net10.0-android` and `net10.0-ios`, including AOT.
- The legacy `Solnet.Wallet` 6.1.0 (2022-07-01) depends on `Chaos.NaCl.Standard` and `Portable.BouncyCastle` 1.9.0 — the latter would conflict with the repo's existing `BouncyCastle.Cryptography` 2.6.0 (aliased `bc26`). Avoided by using `Solana.Wallet` 8.7.0.

`net8.0` assets are compatible with the `net10.0` / `net10.0-android` / `net10.0-ios` targets in use.

### Solnet derivation, verified from source

From `src/Solnet.Wallet/Wallet.cs`:

- `private const string DerivationPath = "m/44'/501'/x'/0'";`
- `SeedMode.Ed25519Bip32` (the constructor default): `Account = GetAccount(0)` → `m/44'/501'/0'/0'`.
- `SeedMode.Bip39`: `Account` is built directly from the raw BIP39 seed (`solana-keygen` compatible), **and `GetAccount(index)` throws** — `$"seed mode: {_seedMode} cannot derive Ed25519 based BIP32 keys"`. `Sign(message, accountIndex)` throws likewise.

Consequence: choosing `Bip39` would foreclose multi-account derivation entirely. `Ed25519Bip32` is chosen.

### Mobile Wallet Adapter

- **MWA 2.0** is current. Official SDKs: Android (Kotlin/Java), React Native, Flutter, Unity, Unreal. **No C# or .NET implementation exists**, on NuGet or otherwise. The client must be written against the specification.
- **Android only.** The spec states iOS support "is planned for a future version". The protocol depends on Android intents for wallet discovery and Digital Asset Links / App Links for dApp identity verification. Since `PlutoFramework.csproj` targets `net10.0-ios;net10.0-android`, MWA is permanently a disabled state on iOS.
- Local association is the on-device case. Remote and Nostr association exist for desktop dApps reflecting through a relay — excluded.

Protocol details taken from the MWA 2.0 specification:

| Element | Value |
|---|---|
| Association keypair | Ephemeral P-256 (secp256r1) |
| Association token | Public keypoint `Qa`, X9.62 uncompressed (`0x04 \|\| x \|\| y`, 65 bytes), base64url-encoded |
| Association URI | `solana-wallet:/v1/associate/local?association=<token>&port=<port>&v=<version>` |
| Port | Random, 49152–65535, **chosen by the dApp** |
| WebSocket | `ws://localhost:<port>/solana-wallet`; wallet is the **server**, dApp is the **client** |
| Subprotocol | `com.solana.mobilewalletadapter.v1` (binary) or `.v1.base64` |
| Listen window | Wallet listens ≥ 10 s; dApp retries ≥ 30 s before showing user guidance |
| `HELLO_REQ` | `<Qd><Sa>` — `Qd` = dApp ephemeral ECDH keypoint (65 B X9.62); `Sa` = ECDSA-SHA256 signature over `Qd`, P1363 encoded (64 B), signed by the association private key. 129 bytes total. |
| `HELLO_RSP` | `<Qw><session_props>` — `Qw` = wallet ephemeral ECDH keypoint (65 B X9.62), followed by the encrypted session-properties message |
| Key agreement | ECDH P-256 (NIST SP 800-56A) → 32-byte shared secret |
| KDF | HKDF (RFC 5869), SHA-256; `ikm` = shared secret, `salt` = the 65-byte X9.62 `Qa`, `L` = 16 (AES-128 key) |
| Frame | `[4-byte sequence, big-endian][12-byte IV][ciphertext][16-byte auth tag]`, AES-128-GCM, fresh IV per message |
| Sequence | Starts at 1; each received frame must be exactly one greater than the previous |
| Non-privileged methods | `authorize`, `deauthorize`, `get_capabilities` |
| Privileged methods | `sign_and_send_transactions`, `sign_messages`, `clone_authorization` |
| Deprecated | `reauthorize`, `sign_transactions` — not implemented; reauthorization is `authorize` carrying the stored `auth_token` |

`authorize` request carries `identity: { uri, icon, name }`, `chain`, and optionally `features`, `addresses`, `auth_token`, `sign_in_payload`. Default chain is `solana:mainnet` when unspecified.

The request accepts both a `chain` field and a legacy `cluster` field. **This implementation sends `chain` only.** The `SolanaCluster` enum (`Devnet` / `Testnet` / `Mainnet`) is the UI-facing type; `SolanaCluster.ToChainId()` maps it to the wire value (`"solana:devnet"` etc.), which is also what `SolanaMwaKey.Chain` persists. The response carries `auth_token`, `accounts[] { address, display_address, label, icon, chains, features }`, `wallet_uri_base`, and `wallet_icon`. Address fields are base64-encoded on the wire.

## Architecture

### Type-collision constraint

Solnet and Substrate collide on three names the codebase uses pervasively:

| Solnet | Substrate (already in wide use) |
|---|---|
| `Solnet.Wallet.Account` | `Substrate.NetApi.Model.Types.Account` |
| `Solnet.Wallet.Wallet` | `Substrate.NET.Wallet.Wallet` |
| `Solnet.Wallet.Bip39.Mnemonic` | `Substrate.NetApi.Mnemonic` (imported via `using static`) |

**All Solnet usage is confined to `PlutoFrameworkCore/SolanaMnemonicsModel.cs`.** Solnet types do not leak into files that reference Substrate types, except behind a `using SolanaAccount = Solnet.Wallet.Account;` alias. This keeps `KeysModel.cs`, which already has `using Substrate.NetApi.Model.Types;`, unambiguous.

### Layer placement

`PlutoFrameworkCore` is `net10.0` and platform-agnostic, receiving platform services through interfaces injected via `PlutoConfigurationModel` (the existing `IPlutoSecureStorage` pattern). Everything the MWA protocol needs is in the BCL — `ECDiffieHellman`, `ECDsa`, `HKDF`, `AesGcm`, `ClientWebSocket` — so **the entire protocol client lives in Core**. Only the intent launch requires platform code.

```
PlutoFrameworkCore/
  SolanaMnemonicsModel.cs           # sole Solnet touchpoint
  Keys/
    ISolanaAccountKey.cs
    SolanaMnemonicKey.cs
    SolanaMwaKey.cs
    GenericLockedKey.cs             # + two converters, + enum values
  Solana/
    SolanaCluster.cs                # enum Devnet|Testnet|Mainnet + ToChainId()
    Mwa/
      IMwaIntentLauncher.cs         # platform seam
      MwaAssociationKeypair.cs
      MwaSessionCipher.cs
      MwaSession.cs
      MwaClient.cs
      MwaConnectFlow.cs
      MwaModels.cs

PlutoFramework/
  Model/KeysModel.cs                # + Solana methods
  Platforms/Android/MwaIntentLauncher.cs
  Platforms/iOS/MwaIntentLauncher.cs # IsSupported => false
  Components/Solana/                # five pages, below
  Components/Keys/                  # NewKeyView, KeyView, CreateNewKeyPage wiring
```

## Key model

`KeyTypeEnum` gains `SolanaMnemonic` and `SolanaMwa`, **appended** to the enum. `KeyTypeEnumExtensions.GetName()` gains `"Solana key"` and `"Solana wallet"`. The enum is serialized by name into the `Serialized` JSON column, so ordering is not load-bearing, but appending remains the safe habit.

`SolanaMnemonicKey` and `SolanaMwaKey` cannot implement the existing `IAccountKey` — that returns a Substrate `Account`. A parallel `ISolanaAccountKey` exposes `SolanaAccount Account { get; }`.

```csharp
public record SolanaMnemonicKey : ISolanaAccountKey
{
    public required string Mnemonics { get; set; }
    public SolanaAccount Account => SolanaMnemonicsModel.GetAccountFromMnemonics(Mnemonics);
    public string Address => Account.PublicKey.Key;
}

public record SolanaMwaKey        // remote signer: no private key locally
{
    public required string AuthToken { get; set; }
    public required string Address { get; set; }   // base58
    public required string Chain { get; set; }      // solana:devnet | solana:testnet | solana:mainnet
    public string? WalletUriBase { get; set; }
    public string? AccountLabel { get; set; }
}
```

### MWA storage without widening `GenericLockedKey`

MWA holds no private key but does hold a secret — the `auth_token`. The whole `SolanaMwaKey` is serialized to JSON and stored in `SecureStorage` under the existing `SecretStorageKey`, with `GenericLockedKey.PublicKey` set to the base58 address. `KeyView` then renders it like any other key, and the database dedup key `SolanaMwa-<address>` works unchanged.

*Alternative considered and rejected:* adding nullable `Chain` / `WalletUriBase` / `AccountLabel` fields to `GenericLockedKey`. That record is shared by four other key types with no use for those fields; the JSON-blob approach leaves it untouched apart from the enum, a materially smaller blast radius. The cost is that cluster is not readable without unlocking — acceptable, because `KeyView.OnClicked` already authenticates before opening any detail page.

`GenericLockedKey` gains `ToSolanaMnemonicKeyAsync(string reason)` and `ToSolanaMwaKeyAsync(string reason)`, following the existing converters' shape including the type guard and the null-from-storage check.

### `KeysModel` additions

- `GenerateNewSolanaAccountAsync()` — generate mnemonics, then save.
- `SaveSolanaMnemonicKeyAsync(string mnemonics)`
- `SaveSolanaMwaKeyAsync(MwaAuthorizationResult result)`
- `GetSolanaAddressAsync()` → `Task<string?>` — works for either variant.
- `GetSolanaAccountAsync()` → `Task<SolanaAccount?>` — returns a signing account **only** for the mnemonic variant. MWA cannot produce one locally, so it returns `null`; callers that need to sign under MWA must go through `MwaClient` instead.
- `HasSolanaKeyAsync()`

Both save methods delete **both** Solana key types first, enforcing mutual exclusion — mirroring how `SaveSr25519KeyAsync` clears `Sr25519` and `PolkadotJson`.

`SaveKeyAsync`'s `DeviceRegisterService.UpdateUserIdAsync` call stays gated on the Substrate types. Solana keys must not repoint the notification user id.

`ClearAsync` needs no change: `KeysDatabase.DeleteAllAsync` already covers new types.

### `SolanaMnemonicsModel`

Mirrors the existing `MnemonicsModel`:

- `GenerateMnemonics()` → 12 words, English wordlist.
- `GetAccountFromMnemonics(string)` → `new Wallet(mnemonics, WordList.English).Account` (`Ed25519Bip32`, index 0).
- `GetAddressFromMnemonics(string)`
- `ValidateMnemonics(string)` → bool.

## MWA client

**`MwaAssociationKeypair`** — ephemeral P-256 keypair. Encodes `Qa` as 65-byte X9.62 uncompressed, base64url for the URI. Signs `HELLO_REQ` with ECDSA-SHA256, P1363 output.

**`MwaSessionCipher`** — ECDH P-256 → 32-byte shared secret → `HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, outputLength: 16, salt: Qa, info: null)` → AES-128-GCM. Encrypts and decrypts frames of `[4-byte BE seq][12-byte IV][ciphertext][16-byte tag]` with a fresh IV per message. Outbound sequence increments from 1; inbound sequence must be exactly previous + 1, so replayed and reordered frames are rejected independently of decryption success.

**`MwaSession`** — `ClientWebSocket` to `ws://127.0.0.1:<port>/solana-wallet`, subprotocol `com.solana.mobilewalletadapter.v1`. Performs the `HELLO_REQ` / `HELLO_RSP` exchange and hands back a live cipher.

**`MwaClient`** — JSON-RPC 2.0 over the encrypted session: `authorize`, `deauthorize`, `get_capabilities`, `sign_messages`, `sign_and_send_transactions`.

**`MwaConnectFlow`** — orchestration. **Ordering is load-bearing:** the dApp picks the port, so the wallet cannot already be listening.

1. Generate association keypair; pick a random port in 49152–65535.
2. Build the association URI.
3. **Launch the intent** via `IMwaIntentLauncher`.
4. *Then* retry-connect the WebSocket, up to 30 s.
5. `HELLO_REQ` / `HELLO_RSP` → session cipher.
6. `authorize` with `identity { name, uri, icon }` and the selected chain.
7. Return `MwaAuthorizationResult` → `KeysModel.SaveSolanaMwaKeyAsync`.

### Platform seam

```csharp
public interface IMwaIntentLauncher
{
    bool IsSupported { get; }
    Task<bool> LaunchAsync(string associationUri);
}
```

Registered through `PlutoConfigurationModel` alongside the existing `SecureStorage` implementation.

- **Android** — `Intent(Intent.ActionView, Uri.Parse(associationUri))` + `StartActivity`. `ActivityNotFoundException` → `false`, surfaced as "no compatible wallet installed" rather than a crash.
- **iOS** — `IsSupported => false`. The UI must state this plainly rather than fail obscurely.

A `<queries>` element for the `solana-wallet` scheme is added to `XcavateMobileApp/Platforms/Android/AndroidManifest.xml`, required by Android 11+ package visibility to *detect* an installed wallet rather than fire blindly. This is the one edit outside the `realXmarketPlutoFramework` submodule.

## Pages

New pages rather than parameterizing `EnterMnemonicsPage` / `CreateMnemonicsPage`: those sit on the onboarding path, which is out of scope, and they carry Polkadot-specific furniture (the "Import json" button, `CanNotRecoverKeyPopupView`). New pages reuse the presentational components — `PageTemplate`, `Card`, `ElevatedButton`, `MnemonicsView`, `AddressView`.

In `PlutoFramework/Components/Solana/`:

1. **`EnterSolanaMnemonicsPage`** + VM — editor, wordlist validation, inline error, and a **live derived-address preview** so the user can confirm the address matches their existing wallet before committing. Exposes a `Navigation` callback matching `EnterMnemonicsViewModel`'s shape.
2. **`CreateSolanaMnemonicsPage`** + VM — generates 12 words, displays via `MnemonicsView`, backup warning, confirm → save.
3. **`SolanaMnemonicKeyDetailPage`** + VM — `AddressView` with QR (`solana:<address>`), `MnemonicsView`, delete.
4. **`ConnectMwaPage`** + VM — cluster `Picker` (devnet / testnet / mainnet) persisted in `Preferences`; Connect button; explicit states: *launching*, *waiting for wallet*, *authorizing*, *connected*, *failed*, *no wallet installed*, *unsupported on this platform* (iOS).
5. **`SolanaMwaKeyDetailPage`** + VM — wallet label, address, cluster, `AddressView`, and Disconnect, which calls `deauthorize` before deleting the key.

### Wiring

- `CreateNewKeyPage.xaml` — two more `NewKeyView` entries: `KeyType="SolanaMnemonic"`, `KeyType="SolanaMwa"`.
- `NewKeyView.xaml.cs` — a Solana exclusivity group mirroring the existing polkadot group in `CheckKeyExistsAsync`; create branch → `CreateSolanaMnemonicsPage` / `ConnectMwaPage`; import branch → `EnterSolanaMnemonicsPage` / `ConnectMwaPage`.
- `KeyView.xaml.cs` — two switch arms plus descriptions ("Your Solana account", "Connected Solana wallet").

## Testing

NUnit 4 in `PlutoFrameworkTests` (`net10.0`), matching existing style.

| Test | Guards |
|---|---|
| Derivation vector: known mnemonic → expected `m/44'/501'/0'/0'` address | The seed-mode decision. Fails loudly if anyone switches to `Bip39`. |
| `MwaSessionCipher` roundtrip | HKDF + AES-128-GCM correctness |
| Sequence increment; out-of-order and replayed frames rejected | Replay protection independent of decryption |
| Tampered ciphertext fails authentication | GCM tag validation |
| `HELLO_REQ` layout: exactly 129 bytes; signature verifies against the association public key | Wire format and signing |
| Association URI: base64url X9.62 encoding, port within 49152–65535 | URI construction |
| `ValidateMnemonics` accepts valid, rejects bad checksum and non-wordlist words | Import validation |

**Not verifiable without hardware:** the Android intent launch and a real end-to-end handshake require a physical Android device with Phantom, Solflare or Backpack installed. This will be reported as untested rather than implied to be working.

## Explicit exclusions

- Onboarding flow — unchanged.
- `Solana.Rpc` and transaction construction — nothing in scope needs a blockhash; `sign_and_send_transactions` has the wallet submit.
- Remote and Nostr association — desktop-oriented.
- BIP44 account indexes beyond 0.
- `reauthorize` and `sign_transactions` — deprecated in MWA 2.0.
- iOS MWA — not supported by the protocol.

## Sources

- [bmresearch/Solnet](https://github.com/bmresearch/Solnet)
- [Solana.Wallet 8.7.0 on NuGet](https://www.nuget.org/packages/Solana.Wallet)
- [Solnet.Wallet 6.1.0 on NuGet](https://www.nuget.org/packages/Solnet.Wallet)
- [Mobile Wallet Adapter 2.0 specification](https://solana-mobile.github.io/mobile-wallet-adapter/spec/spec.html)
- [solana-mobile/mobile-wallet-adapter](https://github.com/solana-mobile/mobile-wallet-adapter)
- [Mobile dApp Architecture Overview](https://docs.solanamobile.com/developers/mobile-wallet-adapter-deep-dive)
