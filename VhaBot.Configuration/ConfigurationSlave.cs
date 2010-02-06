using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Configuration
{
    [Serializable]
    public class ConfigurationSlave
    {
        public string Account;
        public string Password;
        public string Character;
        public bool Enabled;

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Character, this.Account);
        }
    }
}
