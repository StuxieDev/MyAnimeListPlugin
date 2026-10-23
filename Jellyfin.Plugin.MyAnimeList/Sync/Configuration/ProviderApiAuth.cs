namespace Jellyfin.Plugin.MyAnimeList.Sync.Configuration {
    public class ProviderApiAuth {
        public ApiName Name { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}