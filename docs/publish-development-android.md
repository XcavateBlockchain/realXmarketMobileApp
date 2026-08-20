# publish-development-android — Google Play publishing workflow

`.github/workflows/publish-development-android.yaml` runs automatically on every
push (or merged PR) to the `publish-development` branch. It can also be started
manually from the Actions tab (**workflow_dispatch**), which always publishes
the tip of `publish-development` and lets you pick the track and the release
status for that one run.

What one run does, in order:

1. Checks out `publish-development` including the `realXmarketPlutoFramework`
   submodule (and its nested `Substrate.NET.Wallet` submodule).
2. Installs JDK 17, the .NET 10 SDK, the `maui-android` workload and the
   Android SDK platform the build compiles against.
3. **Auto-increments the app version** in `XcavateMobileApp/XcavateMobileApp.csproj`
   (see below) and pushes that bump back to the branch with `[skip ci]` so it
   does not re-trigger the workflow.
4. Recreates the gitignored `XcavateMobileApp/appsettings.json` (the csproj
   embeds it as a resource, so the build fails without it) and
   `XcavateMobileApp/Platforms/Android/google-services.json` (Android Firebase
   config; without it `FirebaseApp` never initializes and FCM token retrieval
   fails) from secrets.
5. Decodes the upload keystore into the runner's temp directory.
6. Builds and signs the app bundle:
   `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=aab ...`
7. Uploads the resulting `.aab` as a workflow artifact (kept 30 days).
8. Uploads it to Google Play with `.github/scripts/upload_to_play.py`, which
   talks to the Play Developer API v3 directly (`edits.insert` →
   `bundles.upload` → `tracks.update` → `edits.commit`), and puts it on the
   **alpha (closed testing)** track with status `completed`.
9. Deletes the keystore from the runner.

Google processes the bundle within a few minutes for testing tracks; after that
the release is visible under **Test and release → Testing → Closed testing** in
the Play Console and testers on that track get the update.

## Version auto-increment

`ApplicationVersion` in the csproj is the Android **version code** and
`ApplicationDisplayVersion` is the **version name** — the very same two
properties the iOS workflow bumps. Every run increments the values currently in
the csproj:

- `ApplicationVersion` → +1 (Play rejects an upload that reuses a version code,
  which is why the increment exists)
- `ApplicationDisplayVersion` → minor part +1 (`0.27` → `0.28`)
- `PackageVersion` → kept equal to `ApplicationDisplayVersion`

The bump is committed and pushed **before** the build, so even a failed run
consumes its version code exactly once — numbers stay monotonic and a re-run can
never collide with an already-uploaded bundle. A failed run therefore "wastes"
one version number, which is harmless.

The logic lives in `.github/scripts/bump_app_version.py`, shared with the iOS
workflow.

### The two workflows share one version counter

iOS and Android publish from the same csproj, so a push to `publish-development`
triggers two runs that each consume one version number. Both declare the
`concurrency: group: publish-development`, so they **queue behind each other**
instead of racing to push the bump commit. The consequence is that the two
platforms are one version apart for the same source, e.g.:

```
push -> publish-development-ios      publishes 0.28 (build 47)
     -> publish-development-android  publishes 0.29 (build 48)
```

That is expected and harmless — the stores number builds independently anyway.
If you ever want the two to publish identical numbers, the workflows would have
to be merged into one job that builds both platforms.

Note: pull the branch after a publish (`git pull`) so your local checkout picks
up the bump commit. And if you ever protect the `publish-development` branch,
allow GitHub Actions to push to it, otherwise the bump step fails.

## One-time setup: the `googleplay` environment

The job declares `environment: googleplay`, so all secrets are read from a
**repository environment** named exactly `googleplay`:

1. GitHub → the repo → **Settings** → **Environments** → **New environment** →
   name it `googleplay`.
2. Inside it, use **Add environment secret** for each secret in the table
   below. Names must match exactly.
3. Optional but recommended: add yourself under **Required reviewers** in the
   environment's protection rules — then every publish waits for your approval
   click before it can read the secrets and upload.

### Secrets

| Secret name | What it is |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | The upload keystore (`XcavateMobileApp/realxmarket.keystore`), base64-encoded |
| `ANDROID_KEYSTORE_PASSWORD` | Password of that keystore (the "store password") |
| `ANDROID_KEY_ALIAS` | Name of the key inside the keystore |
| `ANDROID_KEY_PASSWORD` | Password of that key (often the same as the store password) |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_BASE64` | Google Play service account `.json` key file, base64-encoded |
| `APPSETTINGS_JSON_BASE64` | Your local `XcavateMobileApp/appsettings.json`, base64-encoded |
| `GOOGLE_SERVICES_JSON_BASE64` | Your local `XcavateMobileApp/Platforms/Android/google-services.json` (Android Firebase config), base64-encoded |

`APPSETTINGS_JSON_BASE64` is also used by the iOS workflow, which reads it from
the `appstore` environment. Either add the same value to both environments, or
add it once as a **repository** secret (Settings → Secrets and variables →
Actions) — environments fall back to repository secrets for names they do not
define themselves.

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

### Upload keystore (`ANDROID_KEYSTORE_*` / `ANDROID_KEY_*`)

The keystore already exists on this machine:
`P:\programming\realXmarketMobileApp\XcavateMobileApp\realxmarket.keystore`
(gitignored). It must be the **same** key the existing Play listing is
registered with — Play rejects a bundle signed with anything else — so use this
file, do not generate a new one.

```powershell
# base64 it into the ANDROID_KEYSTORE_BASE64 secret
[Convert]::ToBase64String([IO.File]::ReadAllBytes("P:\programming\realXmarketMobileApp\XcavateMobileApp\realxmarket.keystore")) | Set-Clipboard
```

To read the alias out of it (the value for `ANDROID_KEY_ALIAS`), use the
`keytool` that ships with the Android JDK — it prompts for the store password:

```powershell
& "C:\Program Files\Android\openjdk\jdk-21.0.8\bin\keytool.exe" -list -v -keystore "P:\programming\realXmarketMobileApp\XcavateMobileApp\realxmarket.keystore"
```

The output line `Alias name: ...` is `ANDROID_KEY_ALIAS`; the password you just
typed is `ANDROID_KEYSTORE_PASSWORD`. `ANDROID_KEY_PASSWORD` is the password of
the key inside the keystore, which for this JKS keystore can differ from the
store password — it is usually the same one. To check a guess without changing
anything:

```powershell
& "C:\Program Files\Android\openjdk\jdk-21.0.8\bin\keytool.exe" -certreq -keystore "P:\programming\realXmarketMobileApp\XcavateMobileApp\realxmarket.keystore" -alias "<alias>" -storepass "<store password>" -keypass "<guess>"
```

A right guess prints a `BEGIN NEW CERTIFICATE REQUEST` block; a wrong one fails
with `UnrecoverableKeyException: Cannot recover key`. The command only reads the
keystore.

Both passwords are passed to MSBuild as `env:ANDROID_KEYSTORE_PASSWORD` /
`env:ANDROID_KEY_PASSWORD` rather than as literal values, so they never appear
in the build log and characters like `$` or `"` in them need no escaping.

Keep a backup of the keystore somewhere safe. If it is lost, the only way back
is Play Console → **Test and release → Setup → App integrity → Upload key →
Request upload key reset**, which takes a couple of days.

### Google Play service account (`GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_BASE64`)

This is the credential the upload script authenticates with. Creating it spans
the Play Console and the Google Cloud console:

1. **Play Console → Setup → API access.** It shows the Google Cloud project the
   developer account is linked to; link or create one if there is none.
2. On the same page, **Create new service account** → follow the link into the
   **Google Cloud console → IAM & Admin → Service accounts → Create service
   account**. Name it e.g. `play-publisher`, skip the optional "grant this
   service account access to the project" step (it needs no Cloud IAM roles),
   and press **Done**.
3. Still in the Cloud console, open the new service account → **Keys** → **Add
   key** → **Create new key** → **JSON** → **Create**. The `.json` file
   downloads once and cannot be downloaded again. That file, base64-encoded, is
   `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_BASE64`.
4. Enable the API for that Cloud project:
   <https://console.cloud.google.com/apis/library/androidpublisher.googleapis.com>
   → **Enable**.
5. Back in **Play Console → Setup → API access**, the service account now
   appears in the list → **Manage Play Console permissions** → under **App
   permissions** add `com.xcavate.realxmarket` and tick:
   - *View app information and download bulk reports*
   - *Release apps to testing tracks*
   - *Manage testing tracks and edit tester lists*
   - *Release to production, exclude devices, and use Play App Signing* — only
     if you ever want to run the workflow with `track: production`

   Then **Invite user** / **Apply**. No account-level permissions are needed.

Newly granted permissions can take a few minutes (occasionally longer) to
propagate; until then the API answers `The caller does not have permission`.

The `.json` key is a long-lived credential for your Play account. Store it only
in the GitHub environment secret, delete the downloaded file afterwards, and
delete the key in the Cloud console if it ever leaks.

### App settings (`APPSETTINGS_JSON_BASE64`)

Base64 of your local, working
`P:\programming\realXmarketMobileApp\XcavateMobileApp\appsettings.json` (the one
with the real DynamoDB / Sumsub / etc. keys). Base64 keeps it byte-exact,
including its `//` comments. Whenever you add a key to the file locally,
re-encode and update the secret, or CI builds will ship without it.

### Android Firebase config (`GOOGLE_SERVICES_JSON_BASE64`)

Base64 of your local
`P:\programming\realXmarketMobileApp\XcavateMobileApp\Platforms\Android\google-services.json`.
The csproj registers it as `GoogleServicesJson`, which generates the Firebase
resource values the default `FirebaseApp` initializes from — a build without it
produces an app whose push notifications never register.
To re-download it: [Firebase console](https://console.firebase.google.com/) →
the `realxmarket-notifications` project → Project settings → Your apps → the
Android app (`com.xcavate.realxmarket`) → `google-services.json`.

## Choosing the track and the release status

A push publishes to **alpha** (closed testing) with status **completed**, i.e.
the release rolls out to that track's testers straight away. To publish
somewhere else, start the workflow from **Actions → publish-development-android
→ Run workflow** and pick:

- **track**: `internal`, `alpha` (closed testing), `beta` (open testing) or
  `production`. The track must already exist and be configured in the Play
  Console — the API can create a release on it, but it cannot set up testers.
- **status**: `completed` (roll it out) or `draft` (the release waits in the
  Play Console until you press *Rollout*).

`upload_to_play.py` additionally supports a staged rollout
(`--status inProgress --rollout-fraction 0.1`) and
`--changes-not-sent-for-review`; neither is wired to a workflow input, so using
them means editing the *Upload to Google Play* step.

## After the first Play upload: Play App Signing and Play Integrity

Play App Signing re-signs every bundle with the **app signing key**, which is
*not* the upload key in `realxmarket.keystore`. So the certificate a store build
reports is different from the one a locally built APK reports, and anything that
pins that certificate has to be told about it:

- The notifications API validates Play Integrity tokens against
  `GOOGLE_PLAY_INTEGRITY_APP_SIGNING_KEY`, currently set to the digest of the
  local debug keystore. Store builds attest with the Play **app signing key**
  instead, so registrations from a Play build are rejected until that variable
  is updated. Take the certificate from **Play Console → Test and release →
  Setup → App integrity → App signing key certificate** and convert it to the
  format the notifications API expects (see that repo's client-integration
  docs).
- Play Integrity must also be enabled for the app: **App integrity → Integrity
  API**, linked to the same Google Cloud project the notifications API verifies
  tokens with.
- If anything else keys off the certificate fingerprint (Firebase Android app
  SHA-1/SHA-256, App Links `assetlinks.json`), add the app signing certificate
  fingerprint there too, next to the debug one.

## Prerequisites

- A Google Play developer account with the app record for
  `com.xcavate.realxmarket` already created (the API cannot create a new app,
  only new releases of an existing one).
- The app has been published on some track at least once. Play refuses
  non-draft releases while an app has never been published — see the
  troubleshooting entry below.
- `publish-development` is not protected against pushes from GitHub Actions
  (needed for the version bump commit).

## Troubleshooting

- **`Secret X is missing or empty`** — the secret is not in the `googleplay`
  environment (a repository secret of the same name also works), or the run was
  started before the environment existed.
- **`Version code NN has already been used`** — a bundle with that version code
  is already on Play (e.g. uploaded manually). Set `ApplicationVersion` in the
  csproj above the highest version code the Play Console lists, push, and the
  incrementer continues from there.
- **`Only releases with status draft may be created on draft app`** — the app
  has never been published on any track. Run the workflow manually with
  `status: draft`, then press *Rollout* once in the Play Console; later runs can
  use `completed`.
- **`The caller does not have permission`** — the service account has no app
  permissions for `com.xcavate.realxmarket`, or they were granted minutes ago
  and have not propagated yet, or the release-to-track permission is missing for
  the track you chose.
- **`Google Play Android Developer API has not been used in project ... before
  or it is disabled`** — step 4 of the service account setup was skipped.
- **`Could not authenticate with the service account key`** — the key was
  deleted or disabled in the Cloud console, or the base64 got truncated when
  pasting. Re-encode the whole `.json` file.
- **`Failed to sign` / `keystore password was incorrect` during publish** —
  `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_PASSWORD` or `ANDROID_KEY_ALIAS`
  does not match the keystore. Verify them with the `keytool -list -v` command
  above.
- **`Your Android App Bundle is signed with the wrong key`** — the keystore in
  the secret is not the upload key registered for this app. Use
  `XcavateMobileApp/realxmarket.keystore`, or request an upload key reset in the
  Play Console.
- **`android.jar` / `platforms;android-NN` not found** — the .NET Android
  workload moved to a newer compile SDK than the runner has. Bump the version in
  the *Ensure the Android SDK has the platform this build needs* step.
- **JDK errors (`Could not find a JDK`, "requires Java version …")** — the
  workload's supported JDK range moved. Check
  `sdk-manifests/<sdk>/microsoft.net.sdk.android/<version>/WorkloadDependencies.json`
  in a local .NET install and change the *Setup JDK 17* step to match.
- **The version bump push fails** — branch protection is blocking
  `github-actions[bot]`, or the workflow lacks `contents: write` permission
  (check the repo's Settings → Actions → General → Workflow permissions).
- **The run waits at "Waiting for review"** — the `googleplay` environment has
  required reviewers configured; approve it from the run page.
