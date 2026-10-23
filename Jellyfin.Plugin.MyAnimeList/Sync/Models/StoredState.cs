using System;
using Jellyfin.Plugin.MyAnimeList.Sync.Configuration;

namespace Jellyfin.Plugin.MyAnimeList.Sync.Models;

public class StoredState {
    public Guid UserId { get; set; }
    public ApiName ApiName { get; set; }
}