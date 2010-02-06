using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class Notify : PluginBase
    {
        private bool _sendgc = true;
        private bool _sendpg = true;
        //private bool _sendirc = true;
        private string _displayMode = "medium";
        private Config _database;

        public Notify()
        {
            this.Name = "Notifier";
            this.InternalName = "vhNotify";
            this.Author = "Vhab";
            this.Description = "Displays a notification when a member of the bot logs on or off";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("logon", true, UserLevel.Guest),
                new Command("logon clear", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Events.UserLogonEvent += new UserLogonHandler(UserLogonEvent);
            bot.Events.UserLogoffEvent += new UserLogoffHandler(UserLogoffEvent);
            bot.Events.ConfigurationChangedEvent +=new ConfigurationChangedHandler(ConfigurationChangedEvent);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendgc", "Send notifications to the organization channel", this._sendgc);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendpg", "Send notifications to the private channel", this._sendpg);
            //bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendirc", "Send notifications to IRC", this._sendirc);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "mode", "Display mode", this._displayMode, "compact", "medium", "large");

            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS logon (Username VARCHAR(14) PRIMARY KEY, Date INTEGER, Message TEXT)");
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.UserLogonEvent -= new UserLogonHandler(UserLogonEvent);
            bot.Events.UserLogoffEvent -= new UserLogoffHandler(UserLogoffEvent);
            bot.Events.ConfigurationChangedEvent -=new ConfigurationChangedHandler(ConfigurationChangedEvent);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (!bot.FriendList.IsFriend("notify", e.Sender))
            {
                bot.SendReply(e, "You're required to be on the notify list before using this action");
                return;
            }
            switch (e.Command)
            {
                case "logon":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: logon [message]");
                        return;
                    }
                    this._database.ExecuteNonQuery("REPLACE INTO logon VALUES('" + e.Sender + "', " + TimeStamp.Now + ", '" + Config.EscapeString(e.Words[0]) + "')");
                    bot.SendReply(e, "Your logon message has set");
                    break;
                case "logon clear":
                    this._database.ExecuteNonQuery("DELETE FROM logon WHERE Username = '" + e.Sender + "'");
                    bot.SendReply(e, "Your logon message has been cleared");
                    break;
            }
        }

        private void ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section != this.InternalName) return;
            this.LoadConfiguration(bot);
        }

        private void LoadConfiguration(BotShell bot)
        {
            this._sendgc = bot.Configuration.GetBoolean(this.InternalName, "sendgc", this._sendgc);
            this._sendpg = bot.Configuration.GetBoolean(this.InternalName, "sendpg", this._sendpg);
            //this._sendirc = bot.Configuration.GetBoolean(this.InternalName, "sendirc", this._sendirc);
            this._displayMode = bot.Configuration.GetString(this.InternalName, "mode", this._displayMode);
        }

        private void UserLogonEvent(BotShell bot, UserLogonArgs e)
        {
            if (e.First) return;
            if (!e.Sections.Contains("notify")) return;
            string logon = "";
            string sender = HTML.CreateColorString(bot.ColorHeaderHex, e.Sender);
            if (e.SenderWhois != null)
            {
                switch (this._displayMode)
                {
                    case "compact":
                        sender = HTML.CreateColorString(bot.ColorHeaderHex, Format.Whois(e.SenderWhois, FormatStyle.Compact));
                        break;
                    case "large":
                        sender = HTML.CreateColorString(bot.ColorHeaderHex, Format.Whois(e.SenderWhois, FormatStyle.Large));
                        break;
                    default:
                        sender = HTML.CreateColorString(bot.ColorHeaderHex, Format.Whois(e.SenderWhois, FormatStyle.Medium));
                        break;
                }
            }
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT Message FROM logon WHERE Username = '" + e.Sender + "'";
                IDataReader reader = command.ExecuteReader();
                if (reader.Read()) logon = " - " + reader.GetString(0);
                reader.Close();
            }
            string alts = "";
            if (bot.Plugins.IsLoaded("vhMembersViewer"))
            {
                RichTextWindow window = ((MembersViewer)bot.Plugins.GetPlugin("vhMembersViewer")).GetAltsWindow(bot, e.Sender);
                if (window != null)
                    alts = " - " + window.ToString(bot.Users.GetMain(e.Sender) + "'s Alts");
            }
            this.SendNotify(bot, sender + " has logged on" + alts + logon);
        }

        private void UserLogoffEvent(BotShell bot, UserLogoffArgs e)
        {
            if (e.First) return;
            if (!e.Sections.Contains("notify")) return;
            this.SendNotify(bot, HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has logged off");
        }

        private void SendNotify(BotShell bot, string message)
        {
            if (this._sendgc) bot.SendOrganizationMessage(bot.ColorHighlight + message);
            if (this._sendpg) bot.SendPrivateChannelMessage(bot.ColorHighlight + message);
            //if (this._sendirc)
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "logon":
                    return "Allows you to set a message which will be displayed upon logging on.\n" +
                        "Usage: /tell " + bot.Character + " logon [message]";
                case "logon clear":
                    return "Clears your logon message.\n" +
                        "Usage: /tell " + bot.Character + " logon clear";
            }
            return null;
        }
    }
}
