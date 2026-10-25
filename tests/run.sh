#!/usr/bin/env bash
# MyAnimeList - versioning checks, no dependencies. Run with: bash tests/run.sh
# (The plugin's own tests are in TestProject/ and run with dotnet test.)
# MyAnimeList is pre-stable on SemVer 0.x.y. Its first stable release will be
# its first Date Version (https://datevers.ing): the UTC release date as
# YY.MM.DD, plus -1, -2... for later releases the same day. There is no v1.0.0.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PASS=0
FAIL=0

check() { # check "description" command...
    local name="$1"; shift
    if "$@"; then
        PASS=$((PASS + 1)); echo "ok   - $name"
    else
        FAIL=$((FAIL + 1)); echo "FAIL - $name"
    fi
}

PRESTABLE='^0\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$'
# The spec's own pattern (2000-2099 form); calendar validity is checked separately.
DATEVER='^[0-9]{2}\.(0[1-9]|1[0-2])\.(0[1-9]|[12][0-9]|3[01])(-[1-9][0-9]*)?$'
VERSION="$(tr -d '[:space:]' < "$ROOT/VERSION.md")"

is_prestable()   { [[ "$1" =~ $PRESTABLE ]]; }
is_datever()     { [[ "$1" =~ $DATEVER ]]; }
is_real_date()   {
    local d="${1%%-*}"; IFS=. read -r yy mm dd <<< "$d"
    # Round-trip through date: an impossible date (26.02.30) fails or comes back different.
    [ "$(date -u -d "20$yy-$mm-$dd" +%y.%m.%d 2>/dev/null)" = "$d" ]
}
not_in_future()  { [[ ! "${1%%-*}" > "$(date -u +%y.%m.%d)" ]]; }
valid_version()  { is_prestable "$1" || { is_datever "$1" && is_real_date "$1" && not_in_future "$1"; }; }
invalid_version() { ! valid_version "$1"; }
no_datever_tags() { ! git -C "$ROOT" tag -l | grep -qE '^v[0-9]{2}\.[0-9]{2}\.[0-9]{2}(-[0-9]+)?$'; }
stage_allowed()  { ! is_prestable "$VERSION" || no_datever_tags; }
changelog_leads_with_version() {
    # Headings are "## v0.x.y" or "## vYY.MM.DD"; the release workflow looks them up by that exact form.
    local newest
    newest="$(grep -m1 -oE '^## v[^[:space:]]+' "$ROOT/CHANGELOG.md" | sed -E 's/^## v//')"
    [ "$newest" = "$VERSION" ]
}

# Four-part versions for Jellyfin and .NET (scripts/jellyfin-version.sh, used by the release workflow).
four_part()      { [ "$(bash "$ROOT/scripts/jellyfin-version.sh" "$1" 2>/dev/null)" = "$2" ]; }
no_four_part()   { ! bash "$ROOT/scripts/jellyfin-version.sh" "$1" >/dev/null 2>&1; }
JELLYFIN_VERSION="$(bash "$ROOT/scripts/jellyfin-version.sh" "$VERSION" 2>/dev/null)"
props_match()    { [ "$(grep -c "<\(Version\|AssemblyVersion\|FileVersion\)>${JELLYFIN_VERSION//./\.}</" "$ROOT/Directory.Build.props")" = 3 ]; }
meta_matches()   { grep -q "\"version\": \"${JELLYFIN_VERSION//./\.}\"" "$ROOT/Jellyfin.Plugin.MyAnimeList/meta.json"; }

check "VERSION.md ($VERSION) is a pre-stable 0.x.y version or a real, non-future Date Version" valid_version "$VERSION"
check "VERSION.md ($VERSION) isn't 0.x.y once a Date Version tag exists" stage_allowed
check "the newest CHANGELOG.md heading is VERSION.md" changelog_leads_with_version
check "Directory.Build.props versions are $JELLYFIN_VERSION" props_match
check "meta.json version is $JELLYFIN_VERSION" meta_matches
for v in 0.1.0 0.10.2 26.10.23 26.10.23-1; do check "accepts $v" valid_version "$v"; done
for v in 1.0.0 1.2.3 00.1.0 0.01.0 0.1 0.1.0.0 26.02.30 26.13.01 26.10.23-0 99.12.31; do check "rejects $v" invalid_version "$v"; done
check "maps 0.2.0 to 0.2.0.0" four_part 0.2.0 0.2.0.0
check "maps 0.10.3 to 0.10.3.0" four_part 0.10.3 0.10.3.0
check "maps 26.10.23 to 26.10.23.0" four_part 26.10.23 26.10.23.0
check "maps 26.10.23-1 to 26.10.23.1" four_part 26.10.23-1 26.10.23.1
for v in 1.0.0 0.1 26.10.23-0; do check "has no four-part form for $v" no_four_part "$v"; done

echo "$PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ]
