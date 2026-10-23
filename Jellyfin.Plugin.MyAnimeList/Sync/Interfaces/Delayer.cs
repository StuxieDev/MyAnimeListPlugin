using System;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MyAnimeList.Sync.Interfaces;

public interface IAsyncDelayer
{
    Task Delay(TimeSpan timeSpan);
}
public class Delayer : IAsyncDelayer
{
    public async Task Delay(TimeSpan timeSpan)
    {
        await Task.Delay(timeSpan);
    }
}