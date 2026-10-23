#nullable enable
using Jellyfin.Plugin.MyAnimeList.Configuration;
using Jellyfin.Plugin.MyAnimeList.Providers.MyAnimeList.APIs.JikanSystem;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Jellyfin.Plugin.MyAnimeList
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IServerConfigurationManager serverConfigurationManager, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            JikanAPI.Initialize(applicationPaths);

            // The sync user page is added to the Plugin Pages plugin's menu, if it's installed.
            try
            {
                if (Configuration.enableUserPages)
                    CheckPluginPages(applicationPaths, serverConfigurationManager);
                else
                    RemovePluginPages(applicationPaths);
            }
            catch (Exception)
            {
                // A broken Plugin Pages config file must not stop this plugin loading.
            }
        }

        /// <inheritdoc />
        public override string Name => Constants.PluginName;

        /// <inheritdoc />
        public override string Description => "MyAnimeList metadata for anime, and watch status sync with MyAnimeList and other anime tracking sites.";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse(Constants.PluginGuid);

        /// <summary>
        /// The plugin's settings. The sync code reads them under this name.
        /// </summary>
        public PluginConfiguration PluginConfiguration => Configuration;

        public static Plugin? Instance { get; private set; }

        private static string PluginPagesConfigPath(IApplicationPaths applicationPaths) =>
            Path.Combine(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.PluginPages", "config.json");

        private static string PluginPagesId => typeof(Plugin).Namespace + ".Sync";

        public void CheckPluginPages(IApplicationPaths applicationPaths, IServerConfigurationManager serverConfigurationManager)
        {
            int pluginPageConfigVersion = 1;
            string pluginPagesConfig = PluginPagesConfigPath(applicationPaths);

            JObject config = new JObject();
            if (!File.Exists(pluginPagesConfig))
            {
                FileInfo info = new FileInfo(pluginPagesConfig);
                info.Directory?.Create();
            }
            else
            {
                config = JObject.Parse(File.ReadAllText(pluginPagesConfig));
            }

            if (!config.ContainsKey("pages"))
            {
                config.Add("pages", new JArray());
            }

            JObject? pageConfig = config.Value<JArray>("pages")!.FirstOrDefault(x =>
                x.Value<string>("Id") == PluginPagesId) as JObject;

            if (pageConfig != null)
            {
                if ((pageConfig.Value<int?>("Version") ?? 0) < pluginPageConfigVersion)
                {
                    config.Value<JArray>("pages")!.Remove(pageConfig);
                }
            }

            if (!config.Value<JArray>("pages")!.Any(x => x.Value<string>("Id") == PluginPagesId))
            {
                Assembly? pluginPagesAssembly = AssemblyLoadContext.All.SelectMany(x => x.Assemblies).FirstOrDefault(x => x.FullName?.Contains("Jellyfin.Plugin.PluginPages") ?? false);

                Version earliestVersionWithSubUrls = new Version("2.4.1.0");
                bool supportsSubUrls = pluginPagesAssembly != null && pluginPagesAssembly.GetName().Version >= earliestVersionWithSubUrls;

                string rootUrl = serverConfigurationManager.GetNetworkConfiguration().BaseUrl.TrimStart('/').Trim();
                if (!string.IsNullOrEmpty(rootUrl))
                {
                    rootUrl = $"/{rootUrl}";
                }

                config.Value<JArray>("pages")!.Add(new JObject
                {
                    { "Id", PluginPagesId },
                    { "Url", $"{(supportsSubUrls ? "" : rootUrl)}/AniSync/settings" },
                    { "DisplayText", "MyAnimeList Sync" },
                    { "Icon", "sync" },
                    { "Version", pluginPageConfigVersion }
                });

                File.WriteAllText(pluginPagesConfig, config.ToString(Formatting.Indented));
            }
        }

        public void RemovePluginPages(IApplicationPaths applicationPaths)
        {
            string pluginPagesConfig = PluginPagesConfigPath(applicationPaths);
            if (!File.Exists(pluginPagesConfig))
            {
                return;
            }

            JObject config = JObject.Parse(File.ReadAllText(pluginPagesConfig));
            if (!config.ContainsKey("pages"))
            {
                return;
            }

            JArray pages = config.Value<JArray>("pages")!;
            List<JToken> toRemove = pages.Where(x => x.Value<string>("Id") == PluginPagesId).ToList();
            if (toRemove.Count == 0)
            {
                return;
            }

            foreach (JToken entry in toRemove)
            {
                pages.Remove(entry);
            }

            File.WriteAllText(pluginPagesConfig, config.ToString(Formatting.Indented));
        }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            var ns = GetType().Namespace;

            return
            [
                // The plugin's settings button opens the page named after the plugin.
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{ns}.Configuration.configPage.html"
                },
                new PluginPageInfo
                {
                    Name = "MyAnimeList_Sync",
                    EmbeddedResourcePath = $"{ns}.Sync.Configuration.ConfigPage.html"
                },
                new PluginPageInfo
                {
                    Name = "MyAnimeList_SyncJs",
                    EmbeddedResourcePath = $"{ns}.Sync.Configuration.ConfigPageJs.js"
                },
                new PluginPageInfo
                {
                    Name = "MyAnimeList_CommonJs",
                    EmbeddedResourcePath = $"{ns}.Sync.Configuration.CommonJs.js"
                },
                new PluginPageInfo
                {
                    Name = "MyAnimeList_ManualSync",
                    EmbeddedResourcePath = $"{ns}.Sync.Configuration.ManualSync.html"
                },
                new PluginPageInfo
                {
                    Name = "MyAnimeList_ManualSyncJs",
                    EmbeddedResourcePath = $"{ns}.Sync.Configuration.ManualSyncJs.js"
                }
            ];
        }

        /// <summary>
        /// The pages served to users by the sync controller (not the admin dashboard).
        /// </summary>
        public IEnumerable<PluginPageInfo> GetViews()
        {
            return
            [
                new PluginPageInfo
                {
                    Name = "settings",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Sync.Configuration.ConfigPageUser.html"
                }
            ];
        }
    }
}
