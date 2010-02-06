using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Security.Cryptography;

namespace VhaBot.Configuration
{
    [Serializable]
    public class ConfigurationBot
    {
        public string Account;
        public string Password;
        public string Character;
        public string Admin;
        public string Dimension;
        public bool Enabled;
        [XmlElement("Slave")]
        public ConfigurationSlave[] Slaves;
        public bool Master;
        public string PluginsPath;

        public override string ToString()
        {
            return string.Format("{0} ({1}@{2})", this.Character, this.Account, this.Dimension);
        }

        public string ToSecretKey()
        {
            if (this.Account == null) return null;
            if (this.Password == null) return null;
            if (this.Character == null) return null;

            string key = this.Account + "-" + this.Password + "-" + this.Character + "-" + this.Dimension;
            SHA512Managed hasher = new SHA512Managed();
            byte[] buffer = Encoding.UTF8.GetBytes(key);
            byte[] hashed = hasher.ComputeHash(buffer);
            return Convert.ToBase64String(hashed);
        }

        public string GetID()
        {
            if (this.Dimension == null) return null;
            if (this.Character == null) return null;
            return this.Character.ToLower() + "@" + this.Dimension.ToLower();
        }
    }
}
