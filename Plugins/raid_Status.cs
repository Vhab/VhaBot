using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidStatus : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private RaidMembers _members;
        private BotShell _bot;

        public RaidStatus()
        {
            this.Name = "Raid :: Status";
            this.InternalName = "RaidStatus";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore", "RaidMembers" };
            this.Description = "Provides a small UI displaying information about a raider";
            this.Commands = new Command[] {
                new Command("status", true, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
            try { this._members = (RaidMembers)bot.Plugins.GetPlugin("RaidMembers"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Membmers' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            string raider;
            bool other = false;
            if (e.Args.Length > 0 && (bot.Users.GetUser(e.Sender) > bot.Commands.GetRight(e.Command, e.Type) || bot.Users.GetUser(e.Sender) == UserLevel.SuperAdmin))
                other = true;
            if (other)
                raider = bot.Users.GetMain(e.Args[0]);
            else
                raider = bot.Users.GetMain(e.Sender);

            double points = this._core.GetPoints(raider);
            int activity = (int)((float)this._core.GetActivity(raider) / 60);
            string[] alts = bot.Users.GetAlts(raider);

            if (points < this._core.MinimumPoints)
            {
                bot.SendReply(e, "There is no information available for " + HTML.CreateColorString(bot.ColorHeaderHex, raider));
                return;
            }

            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle(raider + "'s Information");
            User user = bot.Users.GetUserInformation(raider);
            if (user != null)
            {
                window.AppendHighlight("Added By: ");
                window.AppendNormal(user.AddedBy);
                window.AppendLineBreak();
                window.AppendHighlight("Added On: ");
                if (user.AddedOn > 0)
                    window.AppendNormal(Format.DateTime(user.AddedOn, FormatStyle.Medium));
                else
                    window.AppendNormal("N/A");
                window.AppendLineBreak();
                window.AppendHighlight("User Level: ");
                window.AppendNormal(user.UserLevel.ToString());
                window.AppendLineBreak();
            }
            if (alts.Length > 0)
            {
                window.AppendHighlight("Alts: ");
                window.AppendNormal(string.Join(", ", alts));
                window.AppendLineBreak();
            }
            window.AppendHighlight("Points: ");
            window.AppendNormal(points.ToString() + " points");
            window.AppendLineBreak();
            window.AppendHighlight("Activity: ");
            window.AppendNormal(activity.ToString() + " minutes");
            window.AppendLineBreak();
            if (this._core.Running)
            {
                window.AppendLineBreak();
                window.AppendHeader("Raid Information");
                RaidCore.Raider account = this._core.GetRaiderByMain(raider);
                window.AppendHighlight("Status: ");
                if (account == null || !account.OnRaid)
                {
                    window.AppendNormal("Not on the current raid");
                    window.AppendLineBreak();
                }
                else
                {
                    window.AppendNormal("Currently on this raid");
                    window.AppendLineBreak();
                    window.AppendHighlight("Character: ");
                    window.AppendNormal(account.Character);
                    window.AppendLineBreak();
                    window.AppendHighlight("Joined on: ");
                    window.AppendNormal(Format.DateTime(account.JoinTime, FormatStyle.Compact));
                    window.AppendLineBreak();
                    window.AppendHighlight("Contribution: ");
                    window.AppendNormal(Math.Floor((float)account.Activity / 60) + " minutes (" + ((float)((float)account.Activity / ((float)this._core.TimeRunning.TotalSeconds - (float)this._core.TimePaused.TotalSeconds) * 100)).ToString("##0.0") + "%)");
                    window.AppendLineBreak();
                }
            }

            bot.SendReply(e, raider + "'s Information »» ", window);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "status":
                    return "Displays various statistics about your (or [username]'s) account.\n" +
                        "Usage: /tell " + bot.Character + " status [[username]]";
            }
            return null;
        }
    }
}
