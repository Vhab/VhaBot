using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class BansManager : PluginBase
    {
        public BansManager()
        {
            this.Name = "Bans Manager";
            this.InternalName = "vhBansManager";
            this.Author = "Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("bans", true, UserLevel.Leader),
                new Command("bans add", true, UserLevel.Admin),
                new Command("bans remove", true, UserLevel.Admin),
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "allowedclan", "Allow Access to Clan", true);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "allowedneutral", "Allow Access to Neutral", true);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "allowedomni", "Allow Access to Omni", true);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "levelreq", "Level Requirement", bot.Bans.LevelRequirement);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(OnConfigurationChangedEvent);

            bot.Bans.AllowedClan = bot.Configuration.GetBoolean(this.InternalName, "allowedclan", true);
            bot.Bans.AllowedNeutral = bot.Configuration.GetBoolean(this.InternalName, "allowedneutral", true);
            bot.Bans.AllowedOmni = bot.Configuration.GetBoolean(this.InternalName, "allowedomni", true);
            bot.Bans.LevelRequirement = bot.Configuration.GetInteger(this.InternalName, "levelreq", bot.Bans.LevelRequirement);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(OnConfigurationChangedEvent);
        }

        public void OnConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
            {
                if (e.Key == "allowedclan")
                    bot.Bans.AllowedClan = (bool)e.Value;
                if (e.Key == "allowedneutral")
                    bot.Bans.AllowedNeutral = (bool)e.Value;
                if (e.Key == "allowedomni")
                    bot.Bans.AllowedOmni = (bool)e.Value;
                if (e.Key == "levelreq")
                    bot.Bans.LevelRequirement = (int)e.Value;
            }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "bans":
                    this.OnBansCommand(bot, e);
                    break;
                case "bans add":
                case "bans remove":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: " + e.Command + " [username]");
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    string username = Format.UppercaseFirst(e.Args[0]);
                    if (e.Command == "bans add")
                    {
                        if (bot.Bans.IsBanned(username))
                        {
                            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is already banned");
                            break;
                        }
                        if (bot.Bans.Add(username, e.Sender))
                            bot.SendReply(e, "You have banned " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                        else
                            bot.SendReply(e, "Unable to ban " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                    }
                    else
                    {
                        if (bot.Bans.Remove(username) || bot.Bans.Remove(bot.GetUserID(username)))
                            bot.SendReply(e, "You have removed " + HTML.CreateColorString(bot.ColorHeaderHex, username + "'s") + " ban");
                        else
                            bot.SendReply(e, "No ban found for " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                    }
                    break;
                case "bans clear":
                    if (e.Args.Length == 0 || e.Args[0] != "confirm")
                    {
                        bot.SendReply(e, "This command will remove ALL bans. If you wish to continue use: /tell " + bot.Character + " bans clear confirm");
                        break;
                    }
                    bot.Bans.RemoveAll();
                    bot.SendReply(e, "All bans have been removed");
                    break;
            }
        }

        private void OnBansCommand(BotShell bot, CommandArgs e)
        {
            Ban[] bans =bot.Bans.List();
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Bans");
            foreach (Ban ban in bans)
            {
                window.AppendHighlight(ban.Character);
                window.AppendNormal(" (By " + ban.AddedBy + " on " + Format.DateTime(ban.AddedOn, FormatStyle.Compact) + ")");
                window.AppendLineBreak();
            }
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, bans.Length.ToString()) + " Bans »» ", window);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "bans":
                    return "Shows the list of all characters that are currently banned from the bot.\n" +
                        "Usage: /tell " + bot.Character + " bans";
                case "bans add":
                    return "Allows you to ban a character from the bot.\nAfter a character is banned, the bot will ignore all input from that character.\n" +
                        "Usage: /tell " + bot.Character + " bans add [username]";
                case "bans remove":
                    return "Allows you to unban a character from the bot.\nAfter this the character should be able to use the bot as he/she was previously able to do.\n" +
                        "Usage: /tell " + bot.Character + " bans remove [username]";
            }
            return null;
        }
    }
}
