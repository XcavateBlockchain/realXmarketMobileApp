# publish-development-ios — iOS App Store publishing workflow

`.github/workflows/publish-development-ios.yaml` runs automatically on every
push (or merged PR) to the `publish-development` branch. It can also be started
manually from the Actions tab (**workflow_dispatch**), which always publishes
the tip of `publish-development`.

What one run does, in order:

1. Checks out `publish-development` including the `realXmarketPlutoFramework`
   submodule (and its nested `Substrate.NET.Wallet` submodule).
2. Installs the latest stable Xcode, the .NET 10 SDK and the MAUI workloads the
   project needs.
3. **Auto-increments the app version** in `XcavateMobileApp/XcavateMobileApp.csproj`
   (see below) and pushes that bump back to the branch with `[skip ci]` so it
   does not re-trigger the workflow.
4. Recreates the gitignored `XcavateMobileApp/appsettings.json` (the csproj
   embeds it as a resource, so the build fails without it) and
   `XcavateMobileApp/GoogleService-Info.plist` (iOS Firebase config, bundled
   into the app by the csproj) from secrets.
5. Imports the Apple Distribution certificate into a temporary keychain and
   installs the App Store provisioning profile. The signing identity name and
   the profile name are read from the uploaded files themselves — you do not
   configure them anywhere.
6. Builds and signs the app:
   `dotnet publish -f net10.0-ios -c Release -p:ArchiveOnBuild=true ...`
7. Uploads the resulting `.ipa` as a workflow artifact (kept 30 days) and then
   uploads it to App Store Connect with `xcrun altool --upload-app` using the
   App Store Connect API key.
8. Deletes the keychain, profiles and API key from the runner.

After Apple finishes processing (typically 10–30 minutes) the build appears
under **TestFlight** in App Store Connect. Releasing it to the App Store is
still a manual step there.

## Version auto-increment

The app was last deployed as `ApplicationDisplayVersion` **0.25** /
`ApplicationVersion` (build number) **44**. Every run increments the values
currently in the csproj, so the first run publishes **0.26 (build 45)**, the
next **0.27 (build 46)**, and so on:

- `ApplicationVersion` → +1 (this is the App Store build number; Apple rejects
  an upload that reuses one, which is why the increment exists)
- `ApplicationDisplayVersion` → minor part +1 (`0.25` → `0.26`)
- `PackageVersion` → kept equal to `ApplicationDisplayVersion`

The bump is committed and pushed **before** the build, so even a failed run
consumes its build number exactly once — numbers stay monotonic and a re-run
can never collide with an already-uploaded build. A failed run therefore
"wastes" one build number, which is harmless.

The logic lives in `.github/scripts/bump_app_version.py`. To jump to a
different version train (e.g. `1.0`), just edit the values in the csproj and
push — the incrementer continues from whatever is there. If you ever want only
the build number to increment (keeping the display version fixed), remove the
`ApplicationDisplayVersion` / `PackageVersion` substitutions from that script.

Note: pull the branch after a publish (`git pull`) so your local checkout picks
up the bump commit. And if you ever protect the `publish-development` branch,
allow GitHub Actions to push to it, otherwise the bump step fails.

### Shared with the Android workflow

`.github/workflows/publish-development-android.yaml` bumps the same two csproj
properties (`ApplicationVersion` is the Android version code,
`ApplicationDisplayVersion` the version name) and declares the same
`concurrency: group: publish-development`, so an iOS and an Android publish
queue behind each other instead of racing to push the bump commit. Each run
consumes one version number, so the two platforms end up one version apart for
the same source - see `docs/publish-development-android.md`.

## One-time setup: the `appstore` environment

The job declares `environment: appstore`, so all secrets are read from a
**repository environment** named exactly `appstore`:

1. GitHub → the repo → **Settings** → **Environments** → **New environment** →
   name it `appstore`.
2. Inside it, use **Add environment secret** for each secret in the table
   below. Names must match exactly.
3. Optional but recommended: add yourself under **Required reviewers** in the
   environment's protection rules — then every publish waits for your approval
   click before it can read the secrets and upload.

### Secrets

| Secret name | What it is |
| --- | --- |
| `APPLE_DISTRIBUTION_CERT_P12_BASE64` | Apple Distribution certificate **including its private key**, exported as `.p12`/`.pfx`, base64-encoded |
| `APPLE_DISTRIBUTION_CERT_PASSWORD` | The password you chose when exporting that `.p12` |
| `APPSTORE_PROVISIONING_PROFILE_BASE64` | App Store distribution provisioning profile for `com.xcavate.realxmarket` (`.mobileprovision`), base64-encoded |
| `APPSTORE_API_KEY_ID` | App Store Connect API key ID — on this machine that is `AB35Z2969M` (from the `AuthKey_AB35Z2969M.p8` file in the repo root) |
| `APPSTORE_API_ISSUER_ID` | Issuer ID (a UUID) of the App Store Connect API team keys |
| `APPSTORE_API_PRIVATE_KEY_BASE64` | The `.p8` API private key file, base64-encoded |
| `APPSETTINGS_JSON_BASE64` | Your local `XcavateMobileApp/appsettings.json`, base64-encoded |
| `GOOGLESERVICE_INFO_PLIST_BASE64` | Your local `XcavateMobileApp/GoogleService-Info.plist` (iOS Firebase config), base64-encoded |

### Base64-encoding a file

Windows (PowerShell) — puts the value on the clipboard, ready to paste:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("P:\path\to\file")) | Set-Clipboard
```

macOS:

```bash
base64 -i /path/to/file | pbcopy
```

## How to obtain each value

### Apple Distribution certificate (`APPLE_DISTRIBUTION_CERT_P12_BASE64` + password)

App Store signing needs an **Apple Distribution** certificate. The csproj
currently references an *Apple Development* certificate ("Created via API"),
which cannot be used for App Store uploads, so you likely need to create the
distribution one:

**First check whether you already have it (Windows):** run `certmgr.msc` →
*Personal* → *Certificates* and look for one named `Apple Distribution: ...`.
If it is there (Visual Studio can create these), right-click → *All Tasks* →
*Export* → choose **Yes, export the private key** → PFX format → set a
password. The `.pfx` is the `.p12` — base64 it. Without the private key the
export is useless for signing.

**Creating it from Windows (no Mac needed), using OpenSSL:**

```powershell
# 1. Create a private key and a certificate signing request
openssl genrsa -out distribution.key 2048
openssl req -new -key distribution.key -out distribution.csr -subj "/CN=Xcavate/C=GB"

# 2. Upload distribution.csr at https://developer.apple.com/account/resources/certificates/add
#    -> choose "Apple Distribution" -> download the resulting distribution.cer

# 3. Bundle the certificate and the private key into a password-protected .p12
openssl x509 -inform DER -in distribution.cer -out distribution.pem
openssl pkcs12 -export -inkey distribution.key -in distribution.pem -out distribution.p12
```

The password you type in the last command is `APPLE_DISTRIBUTION_CERT_PASSWORD`.
Keep `distribution.key`/`distribution.p12` somewhere safe and private — anyone
holding them can sign as you.

**On a Mac instead:** create the certificate via Xcode → Settings → Accounts →
Manage Certificates → "+" → Apple Distribution, then export it from Keychain
Access (select the certificate *with* its private key → Export → `.p12`).

### App Store provisioning profile (`APPSTORE_PROVISIONING_PROFILE_BASE64`)

1. Go to <https://developer.apple.com/account/resources/profiles/add>.
2. Type: **App Store Connect** (under *Distribution*).
3. App ID: `com.xcavate.realxmarket`.
4. Certificate: select the Apple Distribution certificate from the previous
   step (important — the profile must contain exactly that certificate).
5. Name it e.g. `realxmarket App Store`, generate, download the
   `.mobileprovision` file and base64 it.

The workflow reads the profile's name and UUID out of the file itself, so the
name does not need to be configured anywhere. If you later renew the
certificate, regenerate this profile too and update both secrets.

### App Store Connect API key (`APPSTORE_API_KEY_ID`, `APPSTORE_API_ISSUER_ID`, `APPSTORE_API_PRIVATE_KEY_BASE64`)

On this machine the key already exists: the repo root contains
`AuthKey_AB35Z2969M.p8` (gitignored). So:

- `APPSTORE_API_KEY_ID` = `AB35Z2969M`
- `APPSTORE_API_PRIVATE_KEY_BASE64` = base64 of `AuthKey_AB35Z2969M.p8`
- `APPSTORE_API_ISSUER_ID`: App Store Connect →
  <https://appstoreconnect.apple.com/access/integrations/api> (**Users and
  Access → Integrations → App Store Connect API → Team Keys**) — the **Issuer
  ID** is shown at the top of that page.

The key must have the **App Manager** (or Admin) role to upload builds. If you
ever need a new key: same page → "+" → name it, pick the role → **Generate** →
**Download API Key** (possible only once!) — the Key ID is shown in the list.

### App settings (`APPSETTINGS_JSON_BASE64`)

Base64 of your local, working `P:\programming\realXmarketMobileApp\XcavateMobileApp\appsettings.json`
(the one with the real DynamoDB / Sumsub / etc. keys). Base64 keeps it
byte-exact, including its `//` comments. Whenever you add a key to the file
locally, re-encode and update the secret, or CI builds will ship without it.

### iOS Firebase config (`GOOGLESERVICE_INFO_PLIST_BASE64`)

Base64 of your local `P:\programming\realXmarketMobileApp\XcavateMobileApp\GoogleService-Info.plist`.
The csproj bundles it into the app as a `BundleResource` and
`Firebase.Core.App.Configure()` reads it from the bundle root at startup, so a build
missing this secret ships an app whose push notifications never initialize.
To re-download it: [Firebase console](https://console.firebase.google.com/)
→ the `realxmarket-notifications` project → Project settings → Your apps → the
iOS app → `GoogleService-Info.plist`.

## Prerequisites (already true today, listed for completeness)

- Active Apple Developer Program membership.
- The app record for `com.xcavate.realxmarket` exists in App Store Connect
  (true — 0.25 build 44 was already deployed).
- `publish-development` branch is not protected against pushes from GitHub
  Actions (needed for the version bump commit).

## Troubleshooting

- **"No 'Apple Distribution' identity found"** — the `.p12` was exported
  without the private key, or contains a Development certificate. Re-export
  following the certificate section above.
- **"No valid iOS code signing keys found" / provisioning profile errors
  during publish** — the profile does not include the uploaded certificate
  (regenerate it selecting the right certificate), or it is not an *App Store
  Connect* type profile, or its App ID does not match `com.xcavate.realxmarket`.
- **`altool` authentication errors (401/403)** — wrong Issuer ID or Key ID, or
  the API key's role is too low (needs App Manager), or the base64 of the `.p8`
  got truncated.
- **"The bundle version must be higher than the previously uploaded version"**
  — a build with that `ApplicationVersion` already exists in App Store Connect
  (e.g. it was uploaded manually). Manually set `ApplicationVersion` in the
  csproj above the highest uploaded build number, push, and the incrementer
  continues from there.
- **The version bump push fails** — branch protection is blocking
  `github-actions[bot]`, or the workflow lacks `contents: write` permission
  (check the repo's Settings → Actions → General → Workflow permissions).
- **Workload / Xcode version errors** (`NETSDK...`, "requires Xcode X or
  later") — the runner's latest-stable Xcode and the .NET 10 iOS workload have
  drifted apart. Pin an explicit version in the workflow's *Select latest
  stable Xcode* step (`xcode-version: '26.0'` style) until images catch up.
- **"The app requests the entitlement 'aps-environment' ... but the provisioning
  profile doesn't contain this entitlement"** - the App Store profile predates push
  notifications. Enable the **Push Notifications** capability on the
  `com.xcavate.realxmarket` App ID, regenerate the App Store profile and update
  `APPSTORE_PROVISIONING_PROFILE_BASE64`. The csproj adds `aps-environment`
  (`development` for Debug, `production` for Release) through `CustomEntitlements`,
  and the iOS SDK validates it against the profile.
- **Builds install but never receive push notifications** - check that the APNs
  authentication key is uploaded to the `realxmarket-notifications` Firebase project
  (Project settings -> Cloud Messaging -> Apple app configuration). That is a
  separate `.p8` from the App Store Connect API key in the repo root.
- **If Apple ever removes `altool` uploads** — replace the *Upload to App Store
  Connect* step with `xcrun iTMSTransporter`, the
  [Transporter app](https://apps.apple.com/app/transporter/id1450874784)
  workflow, or fastlane's `pilot`, all of which accept the same API key.
