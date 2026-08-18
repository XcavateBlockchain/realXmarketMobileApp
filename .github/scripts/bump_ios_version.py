#!/usr/bin/env python3
"""Increment the app version numbers in XcavateMobileApp.csproj.

- <ApplicationVersion> (the App Store / CFBundleVersion build number) is incremented by 1.
- <ApplicationDisplayVersion> (CFBundleShortVersionString) gets its minor part
  incremented by 1, e.g. 0.25 -> 0.26.
- <PackageVersion> is kept in sync with ApplicationDisplayVersion.

The file is read and written with newline='' so the existing line endings are
preserved byte-for-byte and the diff stays limited to the three version lines.

When run inside GitHub Actions (GITHUB_OUTPUT is set), the new values are
exported as step outputs `display_version` and `build_number`.

Usage: bump_ios_version.py <path-to-csproj>
"""

import os
import re
import sys


def main() -> None:
    if len(sys.argv) != 2:
        sys.exit(f"Usage: {sys.argv[0]} <path-to-csproj>")

    path = sys.argv[1]
    with open(path, encoding="utf-8", newline="") as f:
        content = f.read()

    match = re.search(r"<ApplicationVersion>(\d+)</ApplicationVersion>", content)
    if not match:
        sys.exit(f"error: no <ApplicationVersion> integer found in {path}")
    build_number = int(match.group(1)) + 1

    match = re.search(
        r"<ApplicationDisplayVersion>(\d+)\.(\d+)</ApplicationDisplayVersion>", content
    )
    if not match:
        sys.exit(
            f"error: no <ApplicationDisplayVersion> in <major>.<minor> form found in {path}"
        )
    display_version = f"{match.group(1)}.{int(match.group(2)) + 1}"

    content = re.sub(
        r"<ApplicationVersion>\d+</ApplicationVersion>",
        f"<ApplicationVersion>{build_number}</ApplicationVersion>",
        content,
    )
    content = re.sub(
        r"<ApplicationDisplayVersion>[\d.]+</ApplicationDisplayVersion>",
        f"<ApplicationDisplayVersion>{display_version}</ApplicationDisplayVersion>",
        content,
    )
    content = re.sub(
        r"<PackageVersion>[\d.]+</PackageVersion>",
        f"<PackageVersion>{display_version}</PackageVersion>",
        content,
    )

    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(content)

    github_output = os.environ.get("GITHUB_OUTPUT")
    if github_output:
        with open(github_output, "a", encoding="utf-8") as f:
            f.write(f"display_version={display_version}\n")
            f.write(f"build_number={build_number}\n")

    print(f"Bumped version to {display_version} (build {build_number})")


if __name__ == "__main__":
    main()
