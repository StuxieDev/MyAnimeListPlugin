using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.MyAnimeList.Sync.Models.Kitsu {
    public class KitsuGet {
        public class KitsuGetAnime {
            [JsonPropertyName("data")] public KitsuSearch.KitsuAnime KitsuAnimeData { get; set; }
            /*[JsonPropertyName("included")]
            public List<KitsuSearch.KitsuAnime> RelatedAnime { get; set; }*/
        }
    }
}