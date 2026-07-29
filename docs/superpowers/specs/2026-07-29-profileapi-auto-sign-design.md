# ProfileApi message auto-signing in the messenger WebView

**Date:** 2026-07-29
**Status:** Implemented

## Problem

The messenger dashboard hosted in `X25519WebView` (realxmessenger.xcavate.io) authenticates
every state-changing Profile API / Buckets call with a wallet signature. Each of those calls
raised the `WebSignRawPopupViewModel` signing sheet, so routine messaging actions prompted
the user over and over.

## Requirements

- On `X25519WebView`, message-signing requests whose payload is in the Xcavate Profile API
  signing format (https://github.com/pyrahermesagent/XcavateProfile) sign automatically,
  without the sheet.
- Transaction requests (`signPayload`, `solana:signTransaction`,
  `solana:signAndSendTransaction`) always show their confirmation UI, unchanged.
- Any message that does not match the Profile API format exactly shows the sheet, unchanged.
- No behavior change in the general DApp browser (`PolkadotExtensionWebView`).

## The recognised format

From the XcavateProfile repo (`CryptoHelper.ConstructPayload`, ADMIN_AUTH.md):

```
{METHOD}:{path}:{body hash}:{timestamp}
POST:/api/profiles:0xFA8847B0C33183273F5945508B31C320:2024-01-15T10:30:45.1230000Z
DELETE:/api/profiles/5Grw...::2024-01-15T10:32:00.7890000Z
```

- METHOD: uppercase `GET`/`POST`/`PUT`/`DELETE`.
- path: decoded, starts with `/`, printable ASCII.
- body hash: `0x` + 32 uppercase hex digits (Blake2b-128), or empty for bodyless requests.
- timestamp: ISO-8601 UTC with exactly seven fractional-second digits.

Solana ed25519 wallets sign this string as readable text, so the bytes arriving at
`solana:signMessage` (and at Substrate `signRaw`, when the string is passed through) are
directly recognisable.

## Design

**`PlutoFrameworkCore/Xcavate/ProfileApiPayloadModel.cs`** — a static, dependency-free
validator, `IsProfileApiSignPayload(byte[]|string, DateTime utcNow)`. Strict UTF-8 decode,
a fully anchored regex for the shape above, a real calendar-date parse, and a ±10-minute
timestamp window (the server itself enforces ±5, so honest requests always pass; far-dated
payloads a page might stockpile signatures with do not). Lives in Core so
`PlutoFrameworkTests` can cover it — MAUI UI code has no test reach.

**Both wallet bridges** (`SolanaWalletStandardBridge`, `PolkadotExtensionWalletBridge`) gain
an opt-in `Func<bool>? AllowProfileApiAutoSign { get; init; }`, null by default, so every
other host keeps today's behavior. When the delegate returns true and the message matches
the validator, the bridge signs directly through the exact code the sheet's Sign button
runs (`PlutoFrameworkSolanaAccount.SignMessageAsync` /
`WebSignRawPopupViewModel.SignWithSubstrateAccountAsync`, now internal), producing an
identical signature. Everything else falls through to the sheet. Key unlock (password /
biometrics / MWA) is untouched — only the consent sheet is skipped.

**`X25519WebView`** wires the delegate on both its bridges to a flag recomputed on every
completed navigation (on the UI thread, where the native WebView URL is safe to read): the
current host must match `PlutoConfigurationModel.WhitelistedDApps` — the same configured
list `DAppApprovalModel` honours, deliberately excluding session approvals the user tapped
through. If the user navigates the messenger WebView to some other site, auto-sign silently
becomes the ordinary sheet again.

## Why the format check is not the only gate

Any web page can craft a Profile API-shaped string, and a signature over it authenticates
as the user against the real API. The format check therefore only decides *routine vs.
needs-review*; the trust decision is the whitelisted-host gate above, plus the existing
connect approval a page needs before it can request signatures at all.

## Testing

- `PlutoFrameworkTests/ProfileApiPayloadModelTests.cs` — 13 NUnit cases: the documented
  POST/PUT/DELETE examples, empty body hash, byte/string parity, tolerance boundaries,
  malformed hashes, lowercase/unknown methods, pathless payloads, millisecond and offset
  timestamps, impossible dates, surrounding content, plain text, and invalid UTF-8.
- Android build of `XcavateMobileApp` for the MAUI-side changes; popup behavior verified
  manually (no automated UI reach).
