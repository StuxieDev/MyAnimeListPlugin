<p align="center">
  <img src="assets/logo.svg" alt="MyAnimeList plugin logo" width="96">
</p>

# Contributing to MyAnimeList

Thanks for helping out! Issues and pull requests are welcome.

## Development setup

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Jellyfin 12.x server to test against, with an anime library

Build with:

```bash
dotnet build Jellyfin.Plugin.MyAnimeList/Jellyfin.Plugin.MyAnimeList.csproj -c Release
```

To try it on a server, copy `Jellyfin.Plugin.MyAnimeList.dll`, `FuzzierSharp.dll`, `JikanDotNet.dll`, `LiteDB.dll`, `meta.json` and `banner.png` into a folder under Jellyfin's `plugins` folder and restart Jellyfin. (`dotnet publish` puts all the DLLs in one place.)

## Layout

- `Jellyfin.Plugin.MyAnimeList/Providers/` – the metadata providers, the Jikan API client and its LiteDB cache.
- `Jellyfin.Plugin.MyAnimeList/Sync/` – watch status sync: the `/AniSync` API controller, the list-site API clients and the playback listeners. Its settings live in `SyncConfiguration`, which the plugin's `PluginConfiguration` inherits, so both are one settings file.
- `Jellyfin.Plugin.MyAnimeList/Configuration/configPage.html` is the **Metadata** tab; `Sync/Configuration/` holds the **Sync** and **Manual Sync** tabs. `CommonJs.js` there draws the tab bar for all three.
- `TestProject/` – the tests.

Keep the `/AniSync` routes as they are: people register `…/AniSync/authCallback` as the callback URL of their list-site apps.

## Tests

```bash
dotnet test TestProject/TestProject.csproj --filter "FullyQualifiedName!~UnitTestProject.MyAnimeListTests"
```

These are the unit tests (mostly sync, under `TestProject/Sync/`); the release workflow runs them too. The tests in `TestProject/IntegrationTests.cs` call the live Jikan API, so they're left out there; run them by hand when you change metadata matching. Before opening a PR, also check on a real Jellyfin 12 server that the plugin loads, a metadata refresh fills in an anime, and (if you touched sync) watching an episode updates a test list.

## Jellyfin compatibility

The plugin compiles against the `Jellyfin.Controller` / `Jellyfin.Model` NuGet packages for the **lowest** Jellyfin version it supports (currently 12.0.0), and `meta.json`'s `targetAbi` matches. Jellyfin loads a plugin on any server at or above its `targetAbi`, so one build covers every 12.x release. Only raise these when you need an API from a newer release.

## Changelog

Every user-facing change goes into `CHANGELOG.md` under the upcoming version (`## v0.x.y` for now). Use these section headings, in this order, and leave out any that are empty:

1. `### Added`
2. `### Changed`
3. `### Fixed`
4. `### Removed`
5. `### Security`
6. `### Deprecated`

## Releasing

The plugin is pre-stable, so it's versioned with [Semantic Versioning](https://semver.org) on `0.x.y`: a release that adds or changes anything bumps the minor version (`0.2.0` → `0.3.0`), a fix-only release bumps the patch (`0.2.0` → `0.2.1`). Its first stable release won't be v1.0.0: it will be its first [Date Versioning](https://datevers.ing) release, the UTC release date as `YY.MM.DD` (plus `-1`, `-2`… for later releases the same UTC day, and never a future date), and every release after that is a Date Version too. Declaring the plugin stable is the maintainer's call. Once a Date Version has been tagged, the project never goes back to `0.x`.

To release:

1. Finish the `## v<version>` section in `CHANGELOG.md`. The release workflow copies it into the plugin's changelog in Jellyfin.
2. Put the new version in `VERSION.md` (`0.x.y`, or later `YY.MM.DD` / `YY.MM.DD-N`), and its four-part form in `Directory.Build.props` (`Version`, `AssemblyVersion`, `FileVersion`) and the `version` field of `Jellyfin.Plugin.MyAnimeList/meta.json`: `0.x.y.0`, or later `YY.MM.DD.0` / `YY.MM.DD.N`. `bash scripts/jellyfin-version.sh` prints it. The release workflow sets these anyway.
3. Update `README.md` if anything user-facing changed.
4. Run `bash tests/run.sh` to check the versions line up.
5. Run `./commit.sh` (or `commit.bat` on Windows). It accepts a `0.x.y` version (unless a Date Version tag already exists) or a real, non-future Date Version, and rejects anything else, including `1.x`. It commits everything and creates the tag `v<version>`.
6. Push with `git push origin main --tags`.

Pushing the tag runs `.github/workflows/release.yml`. It runs the tests, builds the plugin and publishes a GitHub Release containing the plugin zip and the repository `manifest.json`. That manifest combines the plugin details in `manifest.template.json` with the version list from the previous release, so Jellyfin always reads it from `releases/latest/download/manifest.json`. Nothing is committed back to the repo.

While the plugin is on `0.x`, the workflow drops any Date Version entries (`26.10.23.0`, `26.10.23.1`, from the releases that are now `v0.1.0` and `v0.1.1`) from that carried-over list, because Jellyfin would always rank them above every `0.x` release.

**Why four parts in Jellyfin's files:** tags, releases, `VERSION.md` and the changelog use the plain version (`0.2.0`, later `26.10.23` or `26.10.23-1`). Jellyfin's API always reports installed plugin versions with four parts, and its plugin-image endpoint matches versions exactly, so `meta.json` and the manifest use `0.2.0.0` (later `26.10.23.0`, or `26.10.23.1` for `-1`), or the plugin's banner won't load. .NET assembly versions can't contain `-`, so the build uses the four-part form as well.
