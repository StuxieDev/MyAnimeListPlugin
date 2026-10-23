using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.MyAnimeList.Sync.Helpers;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace Jellyfin.Plugin.MyAnimeList.Tests.Sync.HelperTests;

public class AnimeListHelperTests {
    [Test]
    public void GetCorrectAniDbSeasonCount() {
        int aniDbId = 1;
        string tvdbId = "abc123";
        AnimeListHelpers.AnimeListXml animeListXml = new AnimeListHelpers.AnimeListXml {
            Anime = new List<AnimeListHelpers.AnimeListAnime> {
                new() {
                    Anidbid = aniDbId.ToString(),
                    Tvdbid = tvdbId
                },
                new() {
                    Anidbid = aniDbId.ToString(),
                    Tvdbid = tvdbId
                },
                new() {
                    Anidbid = aniDbId.ToString(),
                    Tvdbid = String.Empty
                }
            }
        };
        var returned = AnimeListHelpers.ListAllSeasonOfAniDbSeries(aniDbId, animeListXml);
        Assert.IsTrue(returned != null);
        Assert.IsTrue(returned.Count() == 2);
    }
    
    [Test]
    public void GetCorrectAniDbSeason() {
        int aniDbId = 1;
        string tvdbId = "abc123";
        AnimeListHelpers.AnimeListXml animeListXml = new AnimeListHelpers.AnimeListXml {
            Anime = new List<AnimeListHelpers.AnimeListAnime> {
                new() {
                    Anidbid = aniDbId.ToString(),
                    Tvdbid = tvdbId
                },
                new() {
                    Anidbid = aniDbId.ToString(),
                    Tvdbid = tvdbId
                },
                new() {
                    Anidbid = aniDbId.ToString(),
                    Tvdbid = String.Empty
                }
            }
        };
        var returned = AnimeListHelpers.GetAniDbSeason(aniDbId, animeListXml);
        Assert.IsTrue(returned != null);
        Assert.IsTrue(returned.Anidbid == aniDbId.ToString());
    }
}