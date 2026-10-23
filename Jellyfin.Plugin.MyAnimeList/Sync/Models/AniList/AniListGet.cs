using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.MyAnimeList.Sync.Models {
    public class AniListGet {
        public class AniListGetData {
            [JsonPropertyName("Media")] public AniListSearch.Media Media { get; set; }
        }

        public class AniListGetMedia {
            [JsonPropertyName("data")] public AniListGetData Data { get; set; }
        }
    }
}