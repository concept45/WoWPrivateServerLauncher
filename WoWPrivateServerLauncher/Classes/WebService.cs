using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace WoWPrivateServerLauncher.Classes
{
    public class WebService
    {
        public static ExpansionList GetExpansions()
        {
            using (WebClient client = new WebClient())
            {
                return JsonConvert.DeserializeObject<ExpansionList>(client.DownloadString("http://wowprivatelauncher.ddns.net/GetExpansions.php"));
            }
        }

        public static VersionList GetVersions()
        {
            using (WebClient client = new WebClient())
            {
                return JsonConvert.DeserializeObject<VersionList>(client.DownloadString("http://wowprivatelauncher.ddns.net/GetVersions.php"));
            }
        }

        public static Server_List GetServers()
        {
            using (WebClient client = new WebClient())
            {
                return JsonConvert.DeserializeObject<Server_List>(client.DownloadString("http://wowprivatelauncher.ddns.net/GetServers.php"));
            }
        }
    }
}
