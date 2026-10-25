# Changelog

All notable changes to the MyAnimeList plugin are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/). The plugin is pre-stable, so it uses
[Semantic Versioning](https://semver.org) on `0.x.y` for now. Its first stable release will be its
first [Date Versioning](https://datevers.ing) release (`YY.MM.DD`, the UTC release date, with `-1`,
`-2`… for later releases the same day), and every release after that will be a Date Version too.
There will be no v1.0.0. Jellyfin shows each version with a fourth part: `0.2.0` is `0.2.0.0`
(and later `26.10.23` would be `26.10.23.0`, `26.10.23-1` would be `26.10.23.1`).

## v0.2.0

### Added
- `tests/run.sh` checks `VERSION.md`, the newest changelog heading, the version in `Directory.Build.props` and `meta.json`, and the versioning rules below. The release workflow runs it before the unit tests.

### Changed
- The plugin is pre-stable, so it's versioned with SemVer `0.x.y` until its first stable release, which will be its first [Date Versioning](https://datevers.ing) release (there will be no v1.0.0). The two earlier releases have been relabelled: v26.10.23 is now v0.1.0 and v26.10.23-1 is now v0.1.1. `commit.sh` / `commit.bat` and the release workflow accept `0.x.y` versions as well as Date Versions, and refuse `0.x.y` once a Date Version has been released.
- In Jellyfin this release is version `0.2.0.0`. If you installed v26.10.23-1 (shown as `26.10.23.1`), Jellyfin sees that as the newer version and won't offer this one as an update: uninstall the plugin, restart, and install it again from the catalogue.

## v0.1.1

_Released 2026-10-23 as v26.10.23-1._

The first published release: v0.1.0 was tagged but never released, because a test failed while it was being built.

### Fixed
- A sync unit test failed at random. Its helper for making unique test IDs never remembered the IDs it gave out, so two test anime could share an ID. That stopped the v0.1.0 release from building. The plugin itself is unchanged.

## v0.1.0

_Released 2026-10-23 as v26.10.23 (tagged, but never published; see v0.1.1)._

First release of MyAnimeList as a standalone StuxieDev project, for Jellyfin 12. It joins two of ryandash's plugins into one: [jellyfin-plugin-myanimelist](https://github.com/ryandash/jellyfin-plugin-myanimelist) 12.0.0.2 (metadata) and [jellyfin-myanimelist-sync](https://github.com/ryandash/jellyfin-myanimelist-sync) 1.6.0.2 (watch status sync, itself a fork of vosmiic's [jellyfin-ani-sync](https://github.com/vosmiic/jellyfin-ani-sync)).

### Added
- Published from [StuxieDev/MyAnimeListPlugin](https://github.com/StuxieDev/MyAnimeListPlugin), with its own plugin ID, repository manifest, logo and banner.
- Watch status sync, from MyAnimeList-Sync, in the same plugin as the metadata. Its settings are on new **Sync** and **Manual Sync** tabs next to the **Metadata** tab, and are stored in the plugin's one settings file.
- The release workflow runs the unit tests before building.

### Changed
- Built for Jellyfin 12 (.NET 10, `targetAbi` 12.0.0.0). The sync code previously targeted Jellyfin 10.11.
- The release zip now includes `meta.json`, the banner and the three libraries the plugin needs (FuzzierSharp, JikanDotNet, LiteDB), and the server's own copies of Jellyfin's libraries are no longer bundled.
- Fetching the character list (one API request per character and voice actor) is skipped when people aren't being added to the item, which saves a lot of requests on every refresh.
- The plugin no longer creates an unused `myanimelist_<version>` folder inside Jellyfin's plugins folder.

### Fixed
- **The plugin no longer fails to load.** Its cache database was opened in LiteDB's "shared" mode, which needs a system-wide named mutex. Jellyfin can't create one when it runs in Docker or as a non-root user, so the plugin failed with "The system cannot open the device or file specified. : 'Global\…Mutex'" and provided no metadata. The cache now opens in direct mode, which only this process uses anyway.
- Searches and lookups no longer fail when a result is an anime that hasn't aired yet. Tenrai gives those a partial date like `2027-10`, which JikanDotNet couldn't read, so the whole request failed and fell back to the backup API, which often returned unrelated anime (searching "Sousou no Frieren" matched nothing). Partial dates are now padded to the first day of the month or year.
- If the cache database is damaged, it's deleted and created again. If it still can't be opened, or the cache folder can't be created, the plugin keeps going with an in-memory cache instead of failing to load.
- Clearing out caches from older versions no longer deletes the current cache's journal file.
- A failed cache write no longer stops all later cache writes for the rest of the session.
- Sync: the first episode of an anime that isn't on your list yet no longer fails with an error before the anime is added.
- Sync: "Set as rewatching" for completed anime works again. The already-watched check ran before the rewatch check, so completed anime were always skipped.
- Sync: progress is never set lower than what your list already shows, unless a rewatch is being started.
- Sync: an anime on your "plan to watch" list is no longer updated twice for the same episode.

### Removed
- Sync unit tests for code that had already been removed upstream.
