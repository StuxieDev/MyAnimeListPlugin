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

Every user-facing change goes into `CHANGELOG.md` under the upcoming version. Use these section headings, in this order, and leave out any that are empty:

1. `### Added`
2. `### Changed`
3. `### Fixed`
4. `### Removed`
5. `### Security`
6. `### Deprecated`

## Releasing

Versions follow [Date Versioning](https://datevers.ing): the UTC release date as `YY.MM.DD`, plus `-1`, `-2`… for later releases the same UTC day, and never a future date. To release:

1. Finish the `## vYY.MM.DD` section in `CHANGELOG.md`. The release workflow copies it into the plugin's changelog in Jellyfin.
2. Put the new version in `VERSION.md` as `YY.MM.DD` (or `YY.MM.DD-N`), and in the `version` field of `Jellyfin.Plugin.MyAnimeList/meta.json` as `YY.MM.DD.0` (or `YY.MM.DD.N`). The release workflow sets `meta.json` anyway.
3. Update `README.md` if anything user-facing changed.
4. Run `./commit.sh` (or `commit.bat` on Windows). It checks `VERSION.md` holds a real, non-future Date Version, commits everything and creates the tag `vYY.MM.DD` from it.
5. Push with `git push origin main --tags`.

Pushing the tag runs `.github/workflows/release.yml`. It runs the tests, builds the plugin and publishes a GitHub Release containing the plugin zip and the repository `manifest.json`. That manifest combines the plugin details in `manifest.template.json` with the version list from the previous release, so Jellyfin always reads it from `releases/latest/download/manifest.json`. Nothing is committed back to the repo.

**Why four parts in Jellyfin's files:** tags, releases, `VERSION.md` and the changelog use the Date Version (`26.10.23`, `26.10.23-1`). Jellyfin's API always reports installed plugin versions with four parts, and its plugin-image endpoint matches versions exactly, so `meta.json` and the manifest use `26.10.23.0` (or `26.10.23.1` for `-1`), or the plugin's banner won't load. .NET assembly versions can't contain `-`, so the build uses the four-part form as well.
