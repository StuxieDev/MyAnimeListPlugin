using System.Collections.Generic;
using System.Dynamic;

namespace Jellyfin.Plugin.MyAnimeList.Sync.Models {        
    public class Parameters {
        public string localIpAddress { get; set; }
        public int localPort { get; set; }
        public bool https { get; set; }
        public List<ExpandoObject> providerList { get; set; }
        public List<ExpandoObject> libraries { get; set; }
    }
}