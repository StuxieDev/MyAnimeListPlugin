#!/bin/bash
# MyAnimeList - Git commit + tag script
# Commits whatever's staged/unstaged and tags it with the version currently
# in VERSION.md, read dynamically so this script never goes stale the way a
# hardcoded version number does. Pushing the tag triggers the release workflow.
set -e

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$DIR"
VERSION="$(tr -d '[:space:]' < VERSION.md)"

# Pre-stable releases use SemVer 0.x.y. The first stable release (there is no v1.0.0) and every one
# after it use Date Versioning (https://datevers.ing): the UTC release date as YY.MM.DD, plus -1, -2...
# for later releases that day.
if [[ "$VERSION" =~ ^0\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$ ]]; then
    if git tag -l | grep -qE '^v[0-9]{2}\.[0-9]{2}\.[0-9]{2}(-[0-9]+)?$'; then
        echo "VERSION.md holds '$VERSION', but this project already has Date Version tags. Once stable, it stays on Date Versioning." >&2
        exit 1
    fi
else
    DATE="${VERSION%%-*}"
    if ! [[ "$VERSION" =~ ^[0-9]{2}\.(0[1-9]|1[0-2])\.(0[1-9]|[12][0-9]|3[01])(-[1-9][0-9]*)?$ ]] \
        || [ "$(date -u -d "20${DATE//./-}" +%y.%m.%d 2>/dev/null)" != "$DATE" ]; then
        echo "VERSION.md holds '$VERSION', which is neither a pre-stable 0.x.y version nor a Date Version (YY.MM.DD or YY.MM.DD-N, see https://datevers.ing)." >&2
        exit 1
    fi
    TODAY="$(date -u +%y.%m.%d)"
    if [[ "$DATE" > "$TODAY" ]]; then
        echo "VERSION.md is dated $DATE, after today ($TODAY UTC). Date Versions can't be in the future." >&2
        exit 1
    elif [[ "$DATE" < "$TODAY" ]]; then
        echo "Warning: VERSION.md is dated $DATE, but today is $TODAY (UTC). Releasing today? Use $TODAY." >&2
    fi
fi

git add -A
if ! git diff --cached --quiet; then
    git commit -m "$(cat <<MSG
Release v${VERSION}

See CHANGELOG.md for details.
MSG
)"
else
    echo "Nothing to commit - tagging the current HEAD as v${VERSION}."
fi

if git rev-parse "v${VERSION}" >/dev/null 2>&1; then
    echo "Tag v${VERSION} already exists - skipping."
else
    git tag -a "v${VERSION}" -m "MyAnimeList v${VERSION}"
    echo "Tagged v${VERSION}."
fi

echo "Push with: git push origin main --tags"
