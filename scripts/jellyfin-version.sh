#!/usr/bin/env bash
# MyAnimeList - print the four-part version Jellyfin and .NET need for a release version.
# Usage: scripts/jellyfin-version.sh [VERSION]   (defaults to the contents of VERSION.md)
#
#   0.2.0       ->  0.2.0.0       pre-stable SemVer 0.x.y: add a fourth part of 0
#   26.10.23    ->  26.10.23.0    Date Version (https://datevers.ing), first release that day
#   26.10.23-1  ->  26.10.23.1    later release the same day: the -N becomes the fourth part
#
# Jellyfin's API always reports installed plugin versions with four parts and its plugin-image
# lookup compares versions exactly, so a three-part meta.json version makes the banner 404.
# Assembly versions can't contain "-", so the build uses the four-part form too.
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:-$(tr -d '[:space:]' < "$DIR/VERSION.md")}"

if [[ "$VERSION" =~ ^0\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$ ]]; then
    echo "${VERSION}.0"
elif [[ "$VERSION" =~ ^[0-9]{2}\.(0[1-9]|1[0-2])\.(0[1-9]|[12][0-9]|3[01])(-[1-9][0-9]*)?$ ]]; then
    DATE="${VERSION%%-*}"
    SAME_DAY=0
    if [[ "$VERSION" == *-* ]]; then SAME_DAY="${VERSION#*-}"; fi
    echo "${DATE}.${SAME_DAY}"
else
    echo "'$VERSION' is neither a pre-stable 0.x.y version nor a Date Version (YY.MM.DD or YY.MM.DD-N)." >&2
    exit 1
fi
