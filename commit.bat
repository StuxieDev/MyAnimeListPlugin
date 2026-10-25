@echo off
setlocal
REM MyAnimeList - Git commit + tag script (Windows)
REM Commits whatever's staged/unstaged and tags it with the version
REM currently in VERSION.md, read dynamically so this script never goes
REM stale the way a hardcoded version number does. Pushing the tag
REM triggers the release workflow.

set "DIR=%~dp0"
set /p VERSION=<"%DIR%VERSION.md"
cd /d "%DIR%"

REM Pre-stable releases use SemVer 0.x.y. The first stable release (there is no v1.0.0) and every one
REM after it use Date Versioning (https://datevers.ing): the UTC release date as YY.MM.DD, plus -1, -2...
REM for later releases that day.
powershell -NoProfile -Command "$v = '%VERSION%'.Trim(); if ($v -match '^0\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') { if (git -C '%DIR%.' tag -l | Where-Object { $_ -match '^v\d{2}\.\d{2}\.\d{2}(-\d+)?$' }) { exit 4 }; exit 0 }; if ($v -notmatch '^\d{2}\.(0[1-9]|1[0-2])\.(0[1-9]|[12]\d|3[01])(-[1-9]\d*)?$') { exit 1 }; $d = $v.Split('-')[0]; $p = [DateTime]::MinValue; if (-not [DateTime]::TryParseExact($d, 'yy.MM.dd', [Globalization.CultureInfo]::InvariantCulture, 'None', [ref]$p)) { exit 1 }; $t = [DateTime]::UtcNow.ToString('yy.MM.dd'); if ($d -gt $t) { exit 2 }; if ($d -lt $t) { exit 3 }; exit 0"
if errorlevel 4 (
    echo VERSION.md holds %VERSION%, but this project already has Date Version tags. Once stable, it stays on Date Versioning.
    exit /b 1
) else if errorlevel 3 (
    echo Warning: VERSION.md is dated before today ^(UTC^). Releasing today? Use today's date.
) else if errorlevel 2 (
    echo VERSION.md holds %VERSION%, which is dated in the future ^(UTC^). Date Versions can't be.
    exit /b 1
) else if errorlevel 1 (
    echo VERSION.md holds %VERSION%, which is neither a pre-stable 0.x.y version nor a Date Version ^(YY.MM.DD or YY.MM.DD-N, see https://datevers.ing^).
    exit /b 1
)

git add -A
git diff --cached --quiet
if errorlevel 1 (
    git commit -m "Release v%VERSION%" -m "See CHANGELOG.md for details."
) else (
    echo Nothing to commit - tagging the current HEAD as v%VERSION%.
)

git rev-parse "v%VERSION%" >nul 2>&1
if errorlevel 1 (
    git tag -a "v%VERSION%" -m "MyAnimeList v%VERSION%"
    echo Tagged v%VERSION%.
) else (
    echo Tag v%VERSION% already exists - skipping.
)

echo Push with: git push origin main --tags
