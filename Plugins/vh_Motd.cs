using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Data;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{
    public class VhMotd : PluginBase
    {
        private string _message = null;
        private Config _database;
        private bool _skipOne = false;
        private bool _sendLogon = false;
        private bool _sendTell = true;

        public VhMotd()
        {
            this.Name = "Message of the Day";
            this.InternalName = "VhMotd";
            this.Author = "Iriche / Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("motd set", true, UserLevel.Leader),
                new Command("motd reset", true, UserLevel.Leader),
                new Command("motd clear", true, UserLevel.Leader),
                new Command("motd", true, UserLevel.Member)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "send_logon", "Send MOTD on Logon", this._sendLogon);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "send_tell", "Send MOTD on Receiving Any Tell", this._sendTell);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "message", "Message of the Day", this._message);

            this._message = bot.Configuration.GetString(this.InternalName, "message", this._message);
            this._sendLogon = bot.Configuration.GetBoolean(this.InternalName, "send_logon", this._sendLogon);
            this._sendTell = bot.Configuration.GetBoolean(this.InternalName, "send_tell", this._sendTell);

            bot.Events.PrivateMessageEvent += new PrivateMessageHandler(Events_PrivateMessageEvent);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            bot.Events.UserLogonEvent += new UserLogonHandler(Events_UserLogonEvent);

            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS motd (Username VARCHAR(14) PRIMARY KEY)");
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
            {
                this._message = bot.Configuration.GetString(this.InternalName, "message", this._message);
            }
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.PrivateMessageEvent -= new PrivateMessageHandler(Events_PrivateMessageEvent);
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }

        private void Events_PrivateMessageEvent(BotShell bot, PrivateMessageArgs e)
        {
            if (e.Self) return;
            if (!this._sendTell) return;
            if (string.IsNullOrEmpty(this._message)) return;
            if (this.ReceivedMessage(e.Sender)) return;
            if (this._skipOne)
            {
                this._skipOne = false;
                return;
            }
            if (!bot.Users.Authorized(e.Sender, bot.Commands.GetRight("motd", CommandType.Tell))) return;
            bot.SendPrivateMessage(e.SenderID, bot.ColorHighlight + "Message of the day: " + bot.ColorNormal + this._message);
            this._database.ExecuteNonQuery(string.Format("INSERT INTO motd VALUES ('{0}')", e.Sender));
        }

        private void Events_UserLogonEvent(BotShell bot, UserLogonArgs e)
        {
            if (!this._sendLogon) return;
            if (string.IsNullOrEmpty(this._message)) return;
            if (!e.Sections.Contains("notify")) return;
            bot.SendPrivateMessage(e.SenderID, bot.ColorHighlight + "Message of the day: " + bot.ColorNormal + this._message);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "motd set":
                    this.OnMotdSet(bot, e);
                    break;
                case "motd reset":
                    this.OnMotdReset(bot, e);
                    break;
                case "motd clear":
                    this.OnMotdClear(bot, e);
                    break;
                case "motd":
                    this.OnMotd(bot, e);
                    break;
            }
            this._skipOne = true;
        }

        private void OnMotd(BotShell bot, CommandArgs e)
        {
            if (string.IsNullOrEmpty(this._message))
                bot.SendReply(e, "There is currently no MOTD set");
            else
                bot.SendReply(e, "Message of the day: " + bot.ColorNormal + this._message);
        }

        private void OnMotdReset(BotShell bot, CommandArgs e)
        {
            this._database.ExecuteNonQuery("DELETE FROM motd");
            bot.SendReply(e, "The MOTD receiver database has been reset");
        }

        private void OnMotdSet(BotShell bot, CommandArgs e)
        {
            if (e.Words.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: motd set [message]");
                return;
            }
            string message = HTML.UnescapeString(e.Words[0]);
            bot.Configuration.SetString(this.InternalName, "message", message);
            bot.SendReply(e, "The MOTD has been updated. Don't forget to reset the MOTD receiver database");
        }

        private void OnMotdClear(BotShell bot, CommandArgs e)
        {
            bot.Configuration.SetString(this.InternalName, "message", string.Empty);
            bot.SendReply(e, "The MOTD has been cleared");
        }

        public bool ReceivedMessage(string username)
        {
            bool result = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM motd WHERE Username = '" + Config.EscapeString(username) + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read()) result = true;
                    reader.Close();
                }
            }
            return result;
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "motd set":
                    return "Allows you to set the message of the day.\n" +
                        "Usage: /tell " + bot.Character + " motd set [message]";
                case "motd reset":
                    return "Resets the motd receiver database.\n" +
                        "Usage: /tell " + bot.Character + " motd reset";
                case "motd clear":
                    return "Clears the message of the day.\n" +
                        "Usage: /tell " + bot.Character + " motd clear";
                case "motd":
                    return "Displays the current message of the day.\n" +
                        "Usage: /tell " + bot.Character + " motd";
            }
            return null;
        }
    }
}
