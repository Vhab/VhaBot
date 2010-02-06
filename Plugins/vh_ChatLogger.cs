using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class ChatLogger : PluginBase
    {
        public static string PATH_Logs = "logs";
        private string BotName;

        public ChatLogger()
        {
            this.Name = "Chat Logger";
            this.InternalName = "vhChatLogger";
            this.Author = "Vhab";
            this.Version = 102;
            this.DefaultState = PluginState.Installed;
        }

        public override void OnLoad(BotShell bot)
        {
            this.BotName = bot.ToString();
            bot.Events.ChannelMessageEvent += new ChannelMessageHandler(Events_ChannelMessageEvent);
            bot.Events.PrivateChannelMessageEvent += new PrivateChannelMessageHandler(Events_PrivateChannelMessageEvent);
            bot.Events.PrivateMessageEvent += new PrivateMessageHandler(Events_PrivateMessageEvent);
            bot.SlaveEvents.PrivateMessageEvent += new PrivateMessageSlaveHandler(SlaveEvents_PrivateMessageEvent);
            bot.Events.UserJoinChannelEvent += new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.UserLeaveChannelEvent += new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
            bot.Events.UserLogonEvent += new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.UserLogoffEvent += new UserLogoffHandler(Events_UserLogoffEvent);
            bot.Events.BotStateChangedEvent += new BotStateChangedHandler(Events_BotStateChangedEvent);
            bot.SlaveEvents.PrivateChannelMessageEvent += new PrivateChannelMessageSlaveHandler(SlaveEvents_PrivateChannelMessageEvent);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ChannelMessageEvent -= new ChannelMessageHandler(Events_ChannelMessageEvent);
            bot.Events.PrivateChannelMessageEvent -= new PrivateChannelMessageHandler(Events_PrivateChannelMessageEvent);
            bot.Events.PrivateMessageEvent -= new PrivateMessageHandler(Events_PrivateMessageEvent);
            bot.SlaveEvents.PrivateMessageEvent -= new PrivateMessageSlaveHandler(SlaveEvents_PrivateMessageEvent);
            bot.Events.UserJoinChannelEvent -= new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.UserLeaveChannelEvent -= new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
            bot.Events.UserLogonEvent -= new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.UserLogoffEvent -= new UserLogoffHandler(Events_UserLogoffEvent);
            bot.Events.BotStateChangedEvent -= new BotStateChangedHandler(Events_BotStateChangedEvent);
            bot.SlaveEvents.PrivateChannelMessageEvent -= new PrivateChannelMessageSlaveHandler(SlaveEvents_PrivateChannelMessageEvent);
        }

        private void Events_PrivateMessageEvent(BotShell bot, PrivateMessageArgs e)
        {
            string prepend = string.Empty;
            if (e.Self)
                prepend = "To ";
            this.Output("Tells", String.Format("{0}[{1}]: {2}", prepend, e.Sender, HTML.UnescapeString(HTML.StripTags(e.Message))));
        }

        private void SlaveEvents_PrivateMessageEvent(BotShell bot, SlaveArgs slave, PrivateMessageArgs e)
        {
            string prepend = string.Empty;
            if (e.Self)
                prepend = "To ";
            this.Output("Tells", String.Format("{0}[{1}]: {2}", prepend, e.Sender, HTML.UnescapeString(HTML.StripTags(e.Message))));
        }

        private void Events_PrivateChannelMessageEvent(BotShell bot, PrivateChannelMessageArgs e)
        {
            this.Output(e.Channel, String.Format("[{0}] {1}: {2}", e.Channel, e.Sender, HTML.UnescapeString(HTML.StripTags(e.Message))));
        }

        private void SlaveEvents_PrivateChannelMessageEvent(BotShell bot, SlaveArgs slave, PrivateChannelMessageArgs e)
        {
            this.Output(e.Channel, String.Format("[{0}] {1}: {2}", slave.Slave, e.Sender, HTML.UnescapeString(HTML.StripTags(e.Message))));
        }

        private void Events_ChannelMessageEvent(BotShell bot, ChannelMessageArgs e)
        {
            if (e.Type == ChannelType.Organization || e.Type == ChannelType.Towers)
                this.Output(e.Channel, String.Format("[{0}] {1}: {2}", e.Channel, e.Sender, HTML.UnescapeString(HTML.StripTags(e.Message))));
        }

        private void Events_UserJoinChannelEvent(BotShell bot, UserJoinChannelArgs e)
        {
            this.Output(e.Channel, String.Format("[{0}] {1} Joined the channel", e.Channel, e.Sender));
        }

        private void Events_UserLeaveChannelEvent(BotShell bot, UserLeaveChannelArgs e)
        {
            this.Output(e.Channel, String.Format("[{0}] {1} Left the channel", e.Channel, e.Sender));
        }

        private void Events_UserLogonEvent(BotShell bot, UserLogonArgs e)
        {
            if (!e.First)
                this.Output("Notify", String.Format("[Notify List] {0} has logged on", e.Sender));
            else
                this.Output("Notify", String.Format("[Notify List] {0} is online", e.Sender));
        }

        private void Events_UserLogoffEvent(BotShell bot, UserLogoffArgs e)
        {
            if (!e.First)
                this.Output("Notify", String.Format("[Notify List] {0} has logged off", e.Sender));
        }

        private void Events_BotStateChangedEvent(BotShell bot, BotStateChangedArgs e)
        {
            if (e.IsSlave)
            {
                if (e.State == BotState.Connected)
                    this.Output("State", String.Format("[Slave Bot] {0} has connected", e.Character));
                else if (e.State == BotState.Disconnected)
                    this.Output("State", String.Format("[Slave Bot] {0} has disconnected", e.Character));
            }
            else
            {
                if (e.State == BotState.Connected)
                    this.Output("State", String.Format("[Main Bot] {0} has connected", e.Character));
                else if (e.State == BotState.Disconnected)
                    this.Output("State", String.Format("[Main Bot] {0} has disconnected", e.Character));
            }
        }

        private void Output(string section, string message)
        {
            string log = string.Format("{0:yyyy}-{0:MM} {1}.txt", DateTime.Now, section.ToLower());
            string path = PATH_Logs + Path.DirectorySeparatorChar + this.BotName;
            message = message.Trim();

            StreamWriter writer = null;
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                string file = path + Path.DirectorySeparatorChar + log;
                writer = new StreamWriter(file, true);
                writer.WriteLine("{0:yyyy}-{0:MM}-{0:dd}\t{1:00}:{2:00}:{3:00}\t{4}", DateTime.Now, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, message);
            }
            catch { }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            Console.WriteLine(message);
        }
    }
}
