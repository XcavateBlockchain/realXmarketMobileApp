# Notification tap deep-link — design

**Date:** 2026-08-07
**Status:** Approved

## Goal

Tapping a push notification that carries a `bucketId` in its FCM data payload takes the
user to the realxmessenger bucket page inside the app. A tap on any other notification
just opens the app. Android only for now.

## Context

- The notifications API already forwards an optional `data` object (string→string) on
  `/api/notify/` verbatim as the FCM data payload
  (realXmarketNotificationsApi commit `a242e75`). Because the server sends
  `notification` + `data` together, a background tap launches the app with each data
  key as an intent extra — no `click_action` plumbing.
- The app uses Plugin.Firebase.CloudMessaging 3.1.2. `PushNotificationsAppInitializer`
  already subscribes `NotificationTapped` → `NotificationsModel.AddTapped`, but nothing
  on Android forwards intents to `FirebaseCloudMessagingImplementation.OnNewIntent`,
  so that event never fires today.
- `MainActivity` has no `LaunchMode`, so it defaults to `Standard` — the MAUI template
  sets `SingleTop`. Cold start shows a spinner `ContentPage` until `App.InitializeAsync`
  swaps in `XcavateAppShell` (or `OnboardingShell`), so navigation must be deferred
  until the real shell exists.
- `MessageWebViewPage` (PlutoFramework) hosts `X25519WebView`, whose bindable `Url`
  defaults to the my-buckets dashboard with embed params.

## Payload contract

The sender includes:

```json
{ "title": "...", "body": "...", "data": { "bucketId": "<id>" } }
```

The client interprets exactly one data key: `bucketId`. Target URL:

```
https://realxmessenger.xcavate.io/indexed-bucket/{bucketId}?isHeaderVisible=false&primaryColor=%233B4F74
```

with `bucketId` URL-escaped (`Uri.EscapeDataString`). The embed params match the
default messenger URL so the hosted page hides its own header and the native
`TopNavigationBar` mirrors it.

## Components

### 1. `NotificationDeepLinkModel` (new — PlutoFramework `Components/Notifications/`)

Static holder + single consumer:

- `SetBucket(string bucketId)` — stashes the id, then calls `TryOpenPendingAsync()`.
- `TryOpenPendingAsync()`:
  - `Shell.Current` is null (app still booting) → keep the id pending; the App init
    consumes it later.
  - Shell present but `OnboardingModel.IsOnboardingCompleted()` is false → drop the
    id. A half-onboarded user just gets the app opened.
  - Otherwise → clear the pending id **before** navigating (a tap can never navigate
    twice), then on the main thread
    `Shell.Current.Navigation.PushAsync(new MessageWebViewPage(url))`.

### 2. `MessageWebViewPage` — optional URL (PlutoFramework)

`public MessageWebViewPage(string? url = null)`; when `url` is non-null, assign
`webView.Url` after `InitializeComponent`. The existing `Url` bindable property
re-points the WebView. Parameterless callers are unchanged.

### 3. `MainActivity` (XcavateMobileApp `Platforms/Android/`)

- Add `LaunchMode = LaunchMode.SingleTop` to the `[Activity]` attribute (restores the
  MAUI template default): a tap while the app runs reuses the activity via
  `OnNewIntent` instead of stacking a second MAUI activity instance. This also routes
  `plutonication://` links through `OnNewIntent` when the app is already running.
- Extract `HandleIntent(Intent? intent)`, called from the end of `OnCreate` and a new
  `OnNewIntent` override (which also updates `Intent`):
  1. the existing `plutonication://` URI processing, moved unchanged;
  2. `FirebaseCloudMessagingImplementation.OnNewIntent(intent)` — makes the
     already-subscribed `NotificationTapped` → notification history fire for taps
     while the app is alive;
  3. `intent?.Extras?.GetString("bucketId")` → `NotificationDeepLinkModel.SetBucket(...)`.

### 4. `App.InitializeAsync` (XcavateMobileApp)

Right after `MainPage` is set (shell now exists), `await
NotificationDeepLinkModel.TryOpenPendingAsync()` — consumes a cold-start stash. When
the onboarding shell was chosen instead, the call drops the id by the rule above.

## Error handling / edge cases

- No `bucketId` extra → nothing happens beyond opening the app.
- Empty/whitespace `bucketId` → ignored at `SetBucket`.
- Navigation failure (shell mid-transition) → swallow and log; a lost deep link must
  never crash startup.
- The id is escaped before URL interpolation.

## Verification

- `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android` (no
  automated UI test reach — PlutoFrameworkTests covers only PlutoFrameworkCore).
- Manual end-to-end: send `/api/notify/` with `data: {"bucketId": ...}` server-side,
  tap the notification with the app (a) cold, (b) backgrounded, (c) foregrounded.

## Known limitations / out of scope

- Cold-start tap **history** (not navigation) still misses: the plugin raises
  `NotificationTapped` during `OnCreate`, before `PushNotificationsAppInitializer`
  subscribes. Navigation deliberately does not depend on that event.
- iOS: out of scope. The holder is platform-neutral; feed it from the plugin's
  `NotificationTapped` on iOS when that platform is wired up.
- No other `data` keys are interpreted.
