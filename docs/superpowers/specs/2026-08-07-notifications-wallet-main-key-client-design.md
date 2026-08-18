# Wallet Addresses as Main Keys — Client Sync

Date: 2026-08-07
Status: Implemented

## Context

The notifications API changed how it identifies users
(realXmarketNotificationsApi commit `f8ddab9`, spec
`docs/superpowers/specs/2026-08-07-wallet-main-key-design.md` there): a registered
wallet address is now a **main key** of the device registration, equal in standing
to the legacy `uid`. `user_id` targeting in `send-notification` matches
`Q(uid=key) | Q(wallets__address=key)`, deduplicated, so a bare Solana or Polkadot
address reaches the device with no chain qualifier and no uid ever set. Each
`(device, chain, address)` registration is its own row — Polkadot and Solana
registrations on one device coexist instead of overwriting each other the way
consecutive `uid-update` calls did. The wire contract is unchanged: same endpoints,
same two targeting modes.

Before this change the client put the Polkadot address into `uid` via
`/api/user/uid-update/` and deliberately did not wallet-link Polkadot (its links
are recorded unverified). That inverted the new model: the address the backend
targets lived in the legacy slot, and the wallet rows the server now treats as
identities held only Solana.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Polkadot registration | Wallet-link row (`chain: polkadot`, no signature), from `KeysModel.SaveKeyAsync` and every `SyncAsync` | Same unverified trust level `uid` had, but per-chain, non-overwriting, and a main key. Signature-less, so it belongs in background sync. |
| `uid` | Never set; client plumbing removed (`UserIdEndpoint`, `ApiClient.UpdateUserIdRequestAsync`, `DeviceRegisterService.UpdateUserIdAsync`, `IsUserIdUpdated` storage flag) | The server keeps `uid-update` only for backend-issued identifiers. This app's only identifiers are wallet addresses, which no longer belong there. |
| `RegistrationData.Uid` | Kept, documented as legacy | Old installs still carry the Polkadot address an earlier version stored; the diagnostics page shows it as "Legacy user ID". |
| Solana flow | Unchanged | Signed links at creation/connect/unlock moments; the standing of the row changed server-side, not the ceremony. |
| Per-chain replacement | Unchanged (`LinkWalletAsync` unlinks a different address on the same chain first) | Matches the server's per-`(device, chain, address)` rows. |

## Notifications page

`NotificationsPage` (PlutoFramework/Components/Notifications) previously showed
hardcoded mock entries, tapping one navigated to the unrelated messaging overview,
and nothing in the app navigated to the page at all. It now shows real pushes:

- **`NotificationsModel`** persists a capped, newest-first list in Preferences. The
  API keeps no notification history — a push is fire-and-forget — so the device's
  own record is the only history there is: pushes delivered while the app was in
  the foreground (`NotificationReceived`, recorded unread) and tray notifications
  the user tapped (`NotificationTapped`, recorded read). The two are deduplicated
  by content within a 5-minute window. Pushes carry only title + body — no type,
  no id — so entries get a local `Guid` and default to the `System` type.
- `PushNotificationsAppInitializer` subscribes to both Firebase events at startup.
- `NotificationsPageViewModel` loads from the store, refreshes on its change event
  while the page is visible (attached/detached via page lifecycle), and drives an
  empty state. Tapping an entry marks it read instead of navigating away.
- Settings gained a "Notifications" button — the page was previously unreachable.

Limitation inherent to the payload: a push delivered while the app is closed and
then dismissed (never tapped) is never observed, so it cannot appear in the list.
Recording those would need a data-message payload or a server-side history
endpoint.

## Known gap

`uid-update` requires a non-blank string, so a legacy uid cannot be cleared over
the wire. An old install that later switches Polkadot accounts keeps its stale
address as a uid main key server-side (the old client would have overwritten it).
Needs a server-side clearing mechanism if it ever matters in practice.

## Verification

- `PlutoFrameworkTests` wallet-link / registration / nonce / error-body suites pass.
- `XcavateMobileApp` builds for `net10.0-android`.
