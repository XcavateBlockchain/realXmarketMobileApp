#!/usr/bin/env python3
"""Upload an Android App Bundle to Google Play and release it on a track.

Talks to the Google Play Developer API v3 directly (edits.insert ->
bundles.upload -> tracks.update -> edits.commit), so no third-party action or
fastlane installation is involved.

The service account credentials are read from the environment variable
GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_BASE64 (base64 of the .json key file), never
from a file checked into the repo or a path on disk.

Requires: pip install google-api-python-client google-auth

Usage:
  upload_to_play.py <path-to-aab> --package-name com.example.app
                    [--track alpha] [--status completed]
                    [--release-name "0.28 (47)"]
                    [--rollout-fraction 0.1] [--changes-not-sent-for-review]
"""

import argparse
import base64
import binascii
import json
import os
import sys

try:
    from google.auth.exceptions import GoogleAuthError
    from google.oauth2 import service_account
    from googleapiclient.discovery import build
    from googleapiclient.errors import HttpError
    from googleapiclient.http import MediaFileUpload
except ImportError:
    sys.exit(
        "error: missing dependencies. Run: "
        "pip install google-api-python-client google-auth"
    )

SCOPE = "https://www.googleapis.com/auth/androidpublisher"
CREDENTIALS_ENV = "GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_BASE64"

# 8 MiB, a multiple of the 256 KiB the API requires for resumable chunks.
CHUNK_SIZE = 8 * 1024 * 1024

# Play error text -> what actually went wrong, since the raw API messages are
# terse and the fix is never obvious from them.
ERROR_HINTS = (
    (
        "already been used",
        "That version code is already on Play. Raise <ApplicationVersion> in "
        "XcavateMobileApp.csproj above the highest version code the Play "
        "Console shows and push again.",
    ),
    (
        "Only releases with status draft may be created on draft app",
        "The app has never been published on any track, so Play only accepts "
        "draft releases. Run the workflow manually with status=draft and press "
        "Rollout in the Play Console once.",
    ),
    (
        "The caller does not have permission",
        "The service account is not granted access to this app in the Play "
        "Console (Users and permissions -> the service account -> app access "
        "+ the Releases permissions).",
    ),
    (
        "not found",
        "Check the package name and that the app exists in this Play Console "
        "account.",
    ),
)


def credentials_from_env():
    raw = os.environ.get(CREDENTIALS_ENV, "").strip()
    if not raw:
        sys.exit(f"error: {CREDENTIALS_ENV} is not set or empty")

    try:
        decoded = base64.b64decode(raw, validate=True)
    except (binascii.Error, ValueError):
        sys.exit(
            f"error: {CREDENTIALS_ENV} is not valid base64. Re-encode the "
            "service account .json file (see docs/publish-development-android.md)."
        )

    try:
        info = json.loads(decoded)
    except json.JSONDecodeError:
        sys.exit(
            f"error: {CREDENTIALS_ENV} does not decode to JSON. It must be the "
            "base64 of the whole service account .json key file."
        )

    if info.get("type") != "service_account":
        sys.exit(
            f"error: {CREDENTIALS_ENV} is not a service account key "
            f"(type={info.get('type')!r}). Download a key of type JSON from the "
            "service account in the Google Cloud console."
        )

    return service_account.Credentials.from_service_account_info(
        info, scopes=[SCOPE]
    )


def upload_bundle(edits, package_name, edit_id, aab_path):
    media = MediaFileUpload(
        aab_path,
        mimetype="application/octet-stream",
        resumable=True,
        chunksize=CHUNK_SIZE,
    )
    request = edits.bundles().upload(
        packageName=package_name, editId=edit_id, media_body=media
    )

    response = None
    while response is None:
        status, response = request.next_chunk()
        if status:
            print(f"  uploaded {int(status.progress() * 100)}%")
    return response["versionCode"]


def explain(error_text):
    for needle, hint in ERROR_HINTS:
        if needle.lower() in error_text.lower():
            return hint
    return None


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("aab", help="path to the signed .aab")
    parser.add_argument("--package-name", required=True)
    parser.add_argument(
        "--track",
        default="alpha",
        help="Play track: internal, alpha (closed testing), beta (open "
        "testing), production, or a custom track name (default: %(default)s)",
    )
    parser.add_argument(
        "--status",
        default="completed",
        choices=["completed", "draft", "inProgress", "halted"],
        help="release status on that track (default: %(default)s)",
    )
    parser.add_argument(
        "--release-name",
        help="name shown for the release in the Play Console",
    )
    parser.add_argument(
        "--rollout-fraction",
        type=float,
        help="fraction of users for a staged rollout, e.g. 0.1 (only valid "
        "with --status inProgress)",
    )
    parser.add_argument(
        "--changes-not-sent-for-review",
        action="store_true",
        help="commit without sending the changes for review; needed while the "
        "app itself is still a draft in the Play Console",
    )
    args = parser.parse_args()

    if not os.path.isfile(args.aab):
        sys.exit(f"error: no such file: {args.aab}")
    if args.status == "inProgress" and args.rollout_fraction is None:
        sys.exit("error: --status inProgress requires --rollout-fraction")
    if args.rollout_fraction is not None and args.status != "inProgress":
        sys.exit("error: --rollout-fraction is only valid with --status inProgress")

    service = build(
        "androidpublisher", "v3", credentials=credentials_from_env()
    )
    edits = service.edits()

    size_mb = os.path.getsize(args.aab) / (1024 * 1024)
    print(f"Uploading {args.aab} ({size_mb:.1f} MB) to {args.package_name}")

    edit_id = None
    try:
        edit_id = edits.insert(packageName=args.package_name, body={}).execute()["id"]

        version_code = upload_bundle(edits, args.package_name, edit_id, args.aab)
        print(f"Uploaded version code {version_code}")

        release = {"versionCodes": [str(version_code)], "status": args.status}
        if args.release_name:
            release["name"] = args.release_name
        if args.rollout_fraction is not None:
            release["userFraction"] = args.rollout_fraction

        edits.tracks().update(
            packageName=args.package_name,
            editId=edit_id,
            track=args.track,
            body={"releases": [release]},
        ).execute()

        edits.commit(
            packageName=args.package_name,
            editId=edit_id,
            changesNotSentForReview=args.changes_not_sent_for_review,
        ).execute()
        edit_id = None
    except HttpError as error:
        detail = error.content.decode("utf-8", "replace") if error.content else str(error)
        print(f"::error::Google Play API rejected the upload: {detail}")
        hint = explain(detail)
        if hint:
            print(f"::error::{hint}")
        raise SystemExit(1)
    except GoogleAuthError as error:
        print(f"::error::Could not authenticate with the service account key: {error}")
        print(
            "::error::Check that the key still exists and is enabled in the Google "
            "Cloud console, and that the Google Play Android Developer API is "
            "enabled for its project."
        )
        raise SystemExit(1)
    finally:
        # An abandoned edit blocks nothing, but leaving it around makes the
        # next run's edit list confusing.
        if edit_id is not None:
            try:
                edits.delete(packageName=args.package_name, editId=edit_id).execute()
            except Exception:  # never mask the failure that got us here
                pass

    print(
        f"Committed: version code {version_code} is now on the "
        f"{args.track} track with status {args.status}"
    )

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(
                f"Uploaded **{args.release_name or version_code}** "
                f"(version code {version_code}) to the **{args.track}** track "
                f"with status `{args.status}`.\n"
            )


if __name__ == "__main__":
    main()
