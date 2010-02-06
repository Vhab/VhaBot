using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Configuration
{
    [Serializable]
    public class ConfigurationCore
    {
        public string CentralServer = "central.vhabot.net";
        public string CentralAccount = string.Empty;
        public string CentralPassword = string.Empty;
        public string ConfigPath = "config";
        public string PluginsPath = "plugins";
        public string SkinsPath = "skins";
        public string CachePath = "xmlcache";
        public bool Debug = false;
        public int RemotePort = 8422;
    }
}