using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.ShellModules
{
    public class SlaveEvents
    {
        public event UserJoinChannelSlaveHandler UserJoinChannelEvent;
        public event UserLeaveChannelSlaveHandler UserLeaveChannelEvent;

        public event PrivateMessageSlaveHandler PrivateMessageEvent;
        public event PrivateChannelMessageSlaveHandler PrivateChannelMessageEvent;
        public event ChannelMessageSlaveHandler ChannelMessageEvent;

        internal void OnUserJoinChannel(BotShell sender, SlaveArgs slave, UserJoinChannelArgs e)
        {
            if (this.UserJoinChannelEvent != null)
                try { this.UserJoinChannelEvent(sender, slave, e); }
                catch { }
        }
        internal void OnUserLeaveChannel(BotShell sender, SlaveArgs slave, UserLeaveChannelArgs e)
        {
            if (this.UserLeaveChannelEvent != null)
                try { this.UserLeaveChannelEvent(sender, slave, e); }
                catch { }
        }
        internal void OnPrivateMessage(BotShell sender, SlaveArgs slave, PrivateMessageArgs e)
        {
            if (this.PrivateMessageEvent != null)
                try { this.PrivateMessageEvent(sender, slave, e); }
                catch { }
        }
        internal void OnPrivateChannelMessage(BotShell sender, SlaveArgs slave, PrivateChannelMessageArgs e)
        {
            if (this.PrivateChannelMessageEvent != null)
                try { this.PrivateChannelMessageEvent(sender, slave, e); }
                catch { }
        }
        internal void OnChannelMessage(BotShell sender, SlaveArgs slave, ChannelMessageArgs e)
        {
            if (this.ChannelMessageEvent != null)
                try { this.ChannelMessageEvent(sender, slave, e); }
                catch { }
        }
    }
}
