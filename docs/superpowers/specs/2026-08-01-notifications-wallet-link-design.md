# Notifications API wallet linking (Solana + Polkadot)

**Date:** 2026-08-01
**Status:** Implemented

## Problem

The realXmarket Notifications API (realXmarketNotificationsApi) delivers push notifications
targeted at wallet addresses via `/api/user/wallet-link/`. The PlutoFramework submodule
carries an older client integration built for the previous API version: it registers the
device (attestation → JWT → FCM token) and reports the Polkadot address through the generic
`/api/user/uid-update/` endpoint only. Nothing links wallets per-chain, nothing covers
Solana, and the XcavateMobileApp never even starts the notification services — the whole
pipeline is dormant.

## API contract (from the repo's docs/api-reference.md)

`POST /api/user/wallet-link/` (device JWT auth) with body `nonce`, `chain`
(`solana`|`polkadot`), `address`, and — for Solana only — `signature` (base58, 64-byte
Ed25519). The wallet signs this exact UTF-8 message, LF separators, no trailing newline,
nonce verbatim from `/api/nonce/`, device id equal to the one in the JWT:

```
PlutoFramework wallet link
chain: <chain>
address: <address>
nonce: <nonce>
device: <device_id>
```

Solana links verify the signature (`verified: true`); Polkadot links are recorded without
ownership proof until the server implements sr25519 verification (`verified: false`, no
signature field needed). `POST /api/user/wallet-unlink/` takes `chain` + `address` and is
idempotent.

## Design

### PlutoFrameworkCore (PushNotificationServices)

- **`Core/Utils/WalletLinkMessage.cs`** — builds the canonical message string. Byte-exact
  format lives in one place, unit-tested in `PlutoFrameworkTests` (the only test reach).
- **`Core/Misc/WalletChain.cs`** — `"solana"` / `"polkadot"` constants; the server rejects
  anything else, so no free-form strings at call sites.
- **`Api/ApiEndpoints/WalletLinkEndpoint.cs` / `WalletUnlinkEndpoint.cs`** — thin endpoint
  classes in the existing style (401 → `UnauthorizedException`, otherwise
  `EnsureSuccessStatusCode`). `WalletLinkData.Signature` is omitted from JSON when null so
  Polkadot links do not send a spurious field.
- **`ApiClient.LinkWalletRequestAsync(chain, address, signMessageAsync?)`** — fetches a
  fresh nonce, reads the stored device id, builds the message, invokes the caller's signer
  delegate (message → base58 signature) when one is given, POSTs with JWT auth. The nonce
  fetch happens inside this method so a retry gets a fresh nonce (nonces are single-use,
  120 s). `UnlinkWalletRequestAsync(chain, address)` mirrors it without the nonce.
- **`DeviceRegisterService.LinkWalletAsync(chain, address, signMessageAsync?)`** — the
  entry point the app layer calls. Serialised on the existing update lock; requires the
  device to be registered; skips when the address is already linked; unlinks a stale
  same-chain link first (one account slot per chain in this app); retries with backoff;
  records success in secure storage. `UnlinkWalletAsync` and `UnlinkAllWalletsAsync`
  complete the lifecycle.
- **`IPushNotificationsSecureStorage`** gains `SaveLinkedWalletsAsync` /
  `GetLinkedWalletsAsync` (a `LinkedWallet(Chain, Address)` list), implemented in
  `PushNotificationsSecureStorageService` as one JSON blob, wiped with the rest on
  reinstall.

The signer is a `Func<string, Task<string>>` rather than any MAUI type because Core cannot
reference the MAUI layer, and because the two Solana key variants (local mnemonic, Mobile
Wallet Adapter) already hide behind an async signing call.

### PlutoFramework (MAUI layer)

- **`Model/WalletLinkModel.cs`** — the app-facing surface:
  - `LinkPolkadotAsync()` — links the stored Substrate address, no signature.
  - `LinkSolanaMnemonicAsync(address, mnemonics)` — signs with the Solnet account derived
    from the phrase the caller already holds, so no unlock prompt.
  - `TryLinkResolvedSolanaAccountAsync(account)` — opportunistic retry: whenever an
    unlocked Solana account passes through `PlutoFrameworkSolanaAccount.ResolveAsync`, a
    not-yet-linked address gets linked in the background. Restricted to
    `CanSignLocally` so a Mobile Wallet Adapter account never spontaneously launches the
    wallet app.
  - `UnlinkAllAsync()` — used on account clear/logout.
- **`KeysModel.SaveKeyAsync`** — the single funnel every key save goes through — fires the
  matching link in the background: Polkadot types link by address (keeping the legacy
  uid-update call for whatever still targets it), `SolanaMnemonic` links with the secret
  in hand. `SolanaMwa` is deliberately not auto-linked: the API demands a signature for
  Solana and an MWA signature means launching the external wallet, which is not acceptable
  as a silent side effect of saving a key. Such accounts link later only if the server
  drops the signature requirement or a future interactive flow adds it.
- **`KeysModel.ClearAsync`** — unlinks everything previously linked (fire-and-forget)
  before the keys are deleted, so a logged-out device stops receiving wallet-targeted
  notifications.
- **`PushNotificationsAppInitializer`** — after device registration and FCM token update,
  links the Polkadot address if one exists and is not linked yet (silent — no signature
  needed). Solana is not force-linked at startup because signing requires an unlock
  prompt; creation-time linking plus the resolve-time retry cover it without ever
  prompting.

### XcavateMobileApp

- **`appsettings.json`** gains `NOTIFICATIONS_API_URL`. Left as a placeholder — the
  deployed host is not recorded anywhere in either repo — and the initializer is skipped
  with a log line when the value is missing, so the app is safe until the real URL is
  filled in.
- **`App.xaml.cs`** `InitializeAsync` reads the URL from configuration and calls
  `PushNotificationsAppInitializer.Initialize(url)`.

## Known gaps, deliberate

- **MWA Solana accounts are not auto-linked** (see above).
- **No `google-services.json` in XcavateMobileApp** — FCM token retrieval fails (caught
  and logged) until Firebase is configured for the app, so delivery cannot work yet;
  registration and wallet links still land server-side.
- **iOS untested** — Windows builds cover Android only; the iOS attestation path is
  untouched.

## Testing

- `PlutoFrameworkTests/WalletLinkMessageTests.cs` — byte-exact canonical message: LF
  separators, no trailing newline, nonce verbatim, field order.
- `PlutoFrameworkTests/WalletLinkDataTests.cs` — JSON shape: snake_case field names,
  signature omitted when null, present when set.
- Android build of `XcavateMobileApp` for the MAUI/app wiring (no automated UI reach).
