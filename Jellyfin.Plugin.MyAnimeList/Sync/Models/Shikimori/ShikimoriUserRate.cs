using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.MyAnimeList.Sync.Models.Shikimori;

public class ShikimoriUserRate {
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum Status { get; set; }

    [JsonPropertyName("episodes")]
    public int Episodes { get; set; }

    [JsonPropertyName("rewatches")]
    public int Rewatches { get; set; }

    public enum StatusEnum {
        planned,
        watching,
        rewatching,
        completed,
        on_hold,
        dropped
    }
}
