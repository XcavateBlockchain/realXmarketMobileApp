# Notification Tap Deep-Link Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tapping a push notification whose FCM data payload carries `bucketId` opens the realxmessenger bucket page in the app; any other tap just opens the app.

**Architecture:** Android's launcher activity reads `bucketId` from the tap intent's extras and stashes it in a static holder in PlutoFramework; the holder navigates to `MessageWebViewPage` (which gains an optional URL) once the main shell exists — immediately on warm taps, after `App.InitializeAsync` sets the shell on cold starts. The intent is also forwarded to Plugin.Firebase so the already-subscribed `NotificationTapped` → history recording starts firing.

**Tech Stack:** .NET MAUI (net10.0-android), Plugin.Firebase.CloudMessaging 3.1.2, MAUI Shell navigation.

**Spec:** `docs/superpowers/specs/2026-08-07-notification-tap-deep-link-design.md`

## Global Constraints

- Two repos: framework files live in the git submodule `realXmarketPlutoFramework` (branch `Solana-support` — capital S); app files in the parent repo (branch `solana-support`). Commit framework changes inside the submodule, app changes in the parent.
- NEVER stage the submodule pointer in the parent repo (`git add realXmarketPlutoFramework` is forbidden; stage only named files). After each parent commit, check `git show --stat HEAD` — if the submodule pointer got swept in (the user sometimes stages it), say so in your report.
- The user commits their own work concurrently. Record the repo's HEAD SHA before you start; if `git commit` says "nothing to commit", check `git log` — the user may have committed your files already.
- Deep-link URL format (exact): `https://realxmessenger.xcavate.io/indexed-bucket/{bucketId}?isHeaderVisible=false&primaryColor=%233B4F74` with `{bucketId}` passed through `Uri.EscapeDataString`.
- The only interpreted data key is `bucketId` (exact casing).
- No automated test reach for these projects (`PlutoFrameworkTests` covers only `PlutoFrameworkCore`); the verify cycle is an Android build. Read the build's `N Error(s)` summary line — piping to grep/tail makes the Bash tool report the pipe's exit code, so don't trust exit status.
- iOS is out of scope; only `net10.0-android` builds on this machine.
- If an incremental rebuild fails with `XAJVC0000 ... R.java not found`: run `dotnet build-server shutdown`, delete `XcavateMobileApp/obj/Debug/net10.0-android`, rebuild — stale state, not a code error.

---

### Task 1: Framework — deep-link holder and URL-capable MessageWebViewPage

**Files:**
- Create: `realXmarketPlutoFramework/PlutoFramework/Components/Notifications/NotificationDeepLinkModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Messages/MessageWebViewPage.xaml.cs:12-17` (constructor)

**Interfaces:**
- Consumes: `MessageWebViewPage` (existing page), `X25519WebView.Url` (existing bindable string property — setting it re-points the WebView), `OnboardingModel.IsOnboardingCompleted()` (existing, namespace `PlutoFramework.Components.Onboarding`).
- Produces: `PlutoFramework.Components.Notifications.NotificationDeepLinkModel` with `static void SetBucket(string? bucketId)` and `static Task TryOpenPendingAsync()`; `MessageWebViewPage(string? url)` constructor. Tasks 2 and 3 call these.

- [ ] **Step 1: Create `NotificationDeepLinkModel.cs`**

```csharp
using PlutoFramework.Components.Messages;
using PlutoFramework.Components.Onboarding;

namespace PlutoFramework.Components.Notifications;

/// <summary>
/// The deep link carried by a tapped push notification. A tray tap can arrive before
/// any shell exists (cold start), so the target is stashed here and consumed once the
/// main shell is up - immediately for taps on a running app.
/// </summary>
public static class NotificationDeepLinkModel
{
    private const string BucketUrlFormat =
        "https://realxmessenger.xcavate.io/indexed-bucket/{0}?isHeaderVisible=false&primaryColor=%233B4F74";

    private static string? pendingBucketId;

    /// <summary>Stashes the bucket from a tap intent and tries to open it right away.</summary>
    public static void SetBucket(string? bucketId)
    {
        if (string.IsNullOrWhiteSpace(bucketId))
        {
            return;
        }

        pendingBucketId = bucketId;

        _ = TryOpenPendingAsync();
    }

    /// <summary>
    /// Opens the pending deep link if the app is in a state to show it. With no shell
    /// yet (still booting behind the loading page) the link stays pending for the
    /// caller that runs after the shell is set. A user who has not finished onboarding
    /// must not land in the messenger, so their link is dropped - not kept, or it
    /// would fire out of nowhere when onboarding completes much later.
    /// </summary>
    public static Task TryOpenPendingAsync()
    {
        var bucketId = pendingBucketId;

        if (bucketId is null || Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        if (!OnboardingModel.IsOnboardingCompleted())
        {
            pendingBucketId = null;

            return Task.CompletedTask;
        }

        // Cleared before navigating so one tap can never navigate twice.
        pendingBucketId = null;

        var url = string.Format(BucketUrlFormat, Uri.EscapeDataString(bucketId));

        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await Shell.Current.Navigation.PushAsync(new MessageWebViewPage(url));
            }
            catch (Exception e)
            {
                // A lost deep link must never take down startup.
                Console.WriteLine($"[PlutoNotifications] Deep link navigation failed: {e.Message}");
            }
        });
    }
}
```

- [ ] **Step 2: Add the URL constructor to `MessageWebViewPage.xaml.cs`**

Replace the existing constructor (lines 12-17):

```csharp
    public MessageWebViewPage()
    {
        InitializeComponent();

        webView.HeaderChanged += OnWebHeaderChanged;
    }
```

with a chained pair (the true parameterless ctor stays for any reflection/XAML instantiation):

```csharp
    public MessageWebViewPage() : this(null)
    {
    }

    public MessageWebViewPage(string? url)
    {
        InitializeComponent();

        webView.HeaderChanged += OnWebHeaderChanged;

        if (url is not null)
        {
            webView.Url = url;
        }
    }
```

- [ ] **Step 3: Build the framework for Android**

Run from `P:\programming\realXmarketMobileApp`:

```
dotnet build realXmarketPlutoFramework/PlutoFramework/PlutoFramework.csproj -f net10.0-android
```

Expected: `0 Error(s)` in the summary. Warnings are pre-existing noise.

- [ ] **Step 4: Commit (inside the submodule)**

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Components/Notifications/NotificationDeepLinkModel.cs PlutoFramework/Components/Messages/MessageWebViewPage.xaml.cs
git commit -m "feat: add notification deep-link holder and URL-capable MessageWebViewPage

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Android — read the tap intent in MainActivity

**Files:**
- Modify: `XcavateMobileApp/Platforms/Android/MainActivity.cs`

**Interfaces:**
- Consumes: `NotificationDeepLinkModel.SetBucket(string?)` from Task 1; `Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation.OnNewIntent(Intent)` (static, from the Plugin.Firebase.CloudMessaging package, referenced transitively via PlutoFrameworkCore).
- Produces: `MainActivity` with `LaunchMode.SingleTop`, an `OnNewIntent` override, and a private static `HandleIntent(Intent?)` — nothing later depends on these names, but Task 3 assumes a cold-start tap has already stashed its bucket by the time `App.InitializeAsync` finishes.

- [ ] **Step 1: Rewrite `MainActivity.cs` intent handling**

Three changes to `XcavateMobileApp/Platforms/Android/MainActivity.cs`:

1. Add `LaunchMode = LaunchMode.SingleTop` to the `[Activity]` attribute (restores the MAUI template default — a tap on a running app reuses the activity via `OnNewIntent` instead of stacking a second MAUI activity):

```csharp
[Activity(Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    EnableOnBackInvokedCallback = false,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
```

2. Add usings at the top of the file:

```csharp
using Plugin.Firebase.CloudMessaging;
using PlutoFramework.Components.Notifications;
```

3. In `OnCreate`, replace the trailing plutonication block:

```csharp
        if (Intent.Data != null)
        {
            var uriString = Intent?.Data.ToString();

            if (uriString.Equals("plutonication:") || uriString.Equals("plutonication://"))
            {
                // Nothing
            }
            else if (uriString.StartsWith("plutonication"))
            {
                AccessCredentials ac = new AccessCredentials(new Uri(uriString));

                PlutonicationModel.ProcessAccessCredentials(ac);
            }
        }
```

with:

```csharp
        HandleIntent(Intent);
```

and add the override plus the shared handler at the bottom of the class:

```csharp
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent is not null)
        {
            // Later reads of the activity's Intent should see the newest one.
            Intent = intent;
        }

        HandleIntent(intent);
    }

    /// <summary>
    /// Everything a launch or relaunch intent can carry: a plutonication link, a
    /// tapped push notification (forwarded to Plugin.Firebase so NotificationTapped
    /// fires for the history recorder), and the bucketId deep link.
    /// </summary>
    private static void HandleIntent(Intent? intent)
    {
        if (intent is null)
        {
            return;
        }

        FirebaseCloudMessagingImplementation.OnNewIntent(intent);

        NotificationDeepLinkModel.SetBucket(intent.Extras?.GetString("bucketId"));

        var uriString = intent.Data?.ToString();

        if (uriString is null)
        {
            return;
        }

        if (uriString.Equals("plutonication:") || uriString.Equals("plutonication://"))
        {
            // Nothing
        }
        else if (uriString.StartsWith("plutonication"))
        {
            AccessCredentials ac = new AccessCredentials(new Uri(uriString));

            PlutonicationModel.ProcessAccessCredentials(ac);
        }
    }
```

Note: if the build cannot find `FirebaseCloudMessagingImplementation.OnNewIntent`, check the actual surface of Plugin.Firebase.CloudMessaging 3.1.2 (`~/.nuget/packages/plugin.firebase.cloudmessaging/3.1.2`) rather than guessing an alternative name.

- [ ] **Step 2: Build the app for Android**

Run from `P:\programming\realXmarketMobileApp`:

```
dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android
```

Expected: `0 Error(s)` in the summary.

- [ ] **Step 3: Commit (parent repo)**

```bash
git add XcavateMobileApp/Platforms/Android/MainActivity.cs
git commit -m "feat: read notification tap intent in MainActivity

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Then run `git show --stat HEAD` and confirm only `MainActivity.cs` is in the commit.

---

### Task 3: App — consume the pending deep link after the shell is set

**Files:**
- Modify: `XcavateMobileApp/App.xaml.cs:155-161` (end of `InitializeAsync`)

**Interfaces:**
- Consumes: `NotificationDeepLinkModel.TryOpenPendingAsync()` from Task 1.
- Produces: the complete cold-start path; nothing further depends on it.

- [ ] **Step 1: Consume the stash in `App.InitializeAsync`**

Add the using:

```csharp
using PlutoFramework.Components.Notifications;
```

Then in `InitializeAsync`, after the `MainPage = ...` switch and the `StartNotificationServices();` call:

```csharp
            MainPage = OnboardingModel.IsOnboardingCompleted() switch
            {
                true when KeysModel.HasSolanaKey() || KeysModel.HasSubstrateKey() => new XcavateAppShell(),
                _ => new OnboardingShell(),
            };

            StartNotificationServices();
```

append:

```csharp
            // A cold-start notification tap stashed its deep link in MainActivity
            // before any shell existed. Deferred one dispatcher loop so the fresh
            // shell's handlers are attached before a page is pushed onto it.
            Dispatcher.Dispatch(() => _ = NotificationDeepLinkModel.TryOpenPendingAsync());
```

- [ ] **Step 2: Build the app for Android**

Run from `P:\programming\realXmarketMobileApp`:

```
dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android
```

Expected: `0 Error(s)` in the summary.

- [ ] **Step 3: Commit (parent repo)**

```bash
git add XcavateMobileApp/App.xaml.cs
git commit -m "feat: open notification deep link once the shell is up

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Then run `git show --stat HEAD` and confirm only `App.xaml.cs` is in the commit.

---

### Task 4: Manual end-to-end verification (user-driven)

**Files:** none — this is the acceptance check; the app has no automated UI test reach.

- [ ] **Step 1: Install the debug build on a device**

```
dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android -t:Install
```

(Device must be registered against a notifications API instance whose
`GOOGLE_PLAY_INTEGRITY_APP_SIGNING_KEY` matches the local debug keystore digest — see
Settings → notification testing page for the registration verdict.)

- [ ] **Step 2: Send a test push with a bucketId (server side, needs the API key)**

```bash
curl -X POST <NOTIFICATIONS_API_URL>/api/notify/ \
  -H "Authorization: Api-Key <key>" -H "Content-Type: application/json" \
  -d '{"user_id": "<registered wallet address>", "title": "Deep link test", "body": "Tap me", "data": {"bucketId": "<real bucket id>"}}'
```

- [ ] **Step 3: Verify the three tap scenarios**

1. App killed (swiped away) → tap → app cold-starts and lands on the bucket page in `MessageWebViewPage`.
2. App backgrounded → tap → app comes forward and pushes the bucket page (no activity restart).
3. App foregrounded → notification arrives silently in-app (foreground pushes don't auto-display a tray entry; the in-app history on the Notifications page records it).
4. After scenario 1, kill the app (swipe away) and reopen it from the Recents screen → the app must open normally and must NOT reopen the bucket page.

Also confirm: a push **without** `data` behaves exactly as before, and a `plutonication://` link still connects a wallet while the app is running (it now flows through `OnNewIntent`).
