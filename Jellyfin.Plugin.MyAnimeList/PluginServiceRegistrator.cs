using Jellyfin.Plugin.MyAnimeList.Sync;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.MyAnimeList
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            // Sync: watch for playback and "played" changes and send them to the tracking sites.
            serviceCollection.AddHostedService<SessionServerEntry>();
            serviceCollection.AddHostedService<UserDataServerEntry>();
        }
    }
}
