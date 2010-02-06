using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using VhaBot.Configuration;
using VhaBot.Communication;

namespace VhaBot.Core
{
    public class BotManager
    {
        public bool Connected = false;
        public bool Enabled = true;
        public ConfigurationBot Configuration;
        public DateTime ProcessStartTime = DateTime.Now;
        public Process Process;
        public ClientCommunication Communication;
        public Queue<MessageBase> Queue;
        public string ID
        {
            get
            {
                if (this.Configuration == null)
                    return null;
                return this.Configuration.GetID();
            }
        }

        public ConfigurationBot GetConfigurationBot()
        {
            if (this.Configuration == null)
                return null;
            return this.Configuration;
        }

        public bool ProcessRunning
        {
            get
            {
                if (this.Process == null) return false;
                lock (this.Process)
                {
                    this.Process.Refresh();
                    if (this.Process.HasExited) return false;
                }
                return true;
            }
        }
    }
}
