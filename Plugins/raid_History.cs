using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidHistory : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private BotShell _bot;

        public RaidHistory()
        {
            this.Name = "Raid :: History";
            this.InternalName = "RaidHistory";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Provides an interface to browse the raid history";
            this.Commands = new Command[] {
                new Command("raid history", true, UserLevel.Disabled, UserLevel.Leader, UserLevel.Disabled),
                new Command("raid logs", true, UserLevel.Disabled, UserLevel.Leader, UserLevel.Disabled)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "raid history":
                    if (e.Args.Length == 0)
                        this.OnRaidHistoryCommand(bot, e);
                    else
                        this.OnRaidHistoryExtendedCommand(bot, e);
                    break;
                case "raid logs":
                    this.OnRaidLogs(bot, e);
                    break;
            }
        }

        private void OnRaidHistoryCommand(BotShell bot, CommandArgs e)
        {
            RaidCore.Raid[] raids = this._core.GetRaids();
            if (raids.Length == 0)
            {
                bot.SendReply(e, "There's no raid history available");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            for (int i = raids.Length; i > 0; i--)
            {
                RaidCore.Raid raid = raids[i - 1];
                string start;
                if (raid.StartTime > 0)
                    start = Format.DateTime(raid.StartTime, FormatStyle.Compact) + " GMT";
                else
                    start = "N/A";
                string stop;
                if (raid.StopTime > 0)
                    stop = Format.DateTime(raid.StopTime, FormatStyle.Compact) + " GMT";
                else
                    stop = "N/A";
                window.AppendHeader(start + " - " + stop);
                window.AppendHighlight("Started By: ");
                window.AppendNormal(raid.StartAdmin);
                window.AppendLineBreak();
                window.AppendHighlight("Stopped By: ");
                window.AppendNormal(raid.StopAdmin);
                window.AppendLineBreak();
                window.AppendHighlight("Duration: ");
                window.AppendNormal((raid.Activity / 60) + " minutes");
                window.AppendLineBreak();
                window.AppendHighlight("Description: ");
                window.AppendNormal(raid.Description);
                window.AppendLineBreak();
                window.AppendHighlight("Raiders: ");
                window.AppendNormal(raid.Raiders + " [");
                window.AppendBotCommand("View", "raid history " + raid.RaidID);
                window.AppendNormal("] [");
                window.AppendBotCommand("Log", "raid logs " + raid.RaidID);
                window.AppendNormal("]");
                // Replace with IntraBroadcast when done
                if (this._bot.Plugins.IsLoaded("RaidLootLog"))
                {
                    window.AppendNormal(" [");
                    window.AppendBotCommand("Loot log", "lootlog " + raid.RaidID);
                    window.AppendNormal("]");
                
                }
                window.AppendLineBreak(2);
            }
            bot.SendReply(e, "Raid History »» ", window);
        }

        private void OnRaidHistoryExtendedCommand(BotShell bot, CommandArgs e)
        {
            int id = -1;
            try { id = Convert.ToInt32(e.Args[0]); }
            catch { }
            if (id < 0)
            {
                bot.SendReply(e, "Invalid RaidID");
                return;
            }
            RaidCore.Raider[] raiders = this._core.GetRaiders(id);
            if (raiders.Length == 0)
            {
                bot.SendReply(e, "There are no raiders listed under that RaidID");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Raiders on Raid #" + id);
            foreach (RaidCore.Raider raider in raiders)
            {
                window.AppendHighlight(raider.Character);
                window.AppendNormalStart();
                if (raider.Character != raider.Main)
                    window.AppendString(" (Main: " + raider.Main + ")");
                window.AppendString(" (" + Math.Round((float)raider.Activity / 60, 1) + " minutes)");
                window.AppendString(" (" + Format.DateTime(raider.JoinTime, FormatStyle.Compact) + " GMT)");
                window.AppendColorEnd();
                window.AppendLineBreak();
            }
            bot.SendReply(e, "Raiders on Raid #" + id + " »» ", window);
        }

        private void OnRaidLogs(BotShell bot, CommandArgs e)
        {
            Dictionary<string, string> colors = new Dictionary<string, string>();
            colors.Add("raid", RichTextWindow.ColorOrange);
            colors.Add("raiders", "FF4500");
            colors.Add("credits", "ADFF2F");
            colors.Add("points", "00FFFF");
            colors.Add("auction", "A52A2A");

            int max = 250;
            RaidCore.LogEntry[] logs = new RaidCore.LogEntry[0];
            string title;
            if (e.Args.Length > 0)
            {
                try
                {
                    // Try if it's a raid ID
                    logs = this._core.GetLogs(0, Convert.ToInt32(e.Args[0]));
                    title = logs.Length + " Log Entries on Raid #" + Convert.ToInt32(e.Args[0]);
                }
                catch
                {
                    // No raid ID, player maybe?
                    e.Args[0] = bot.Users.GetMain(e.Args[0]);
                    logs = this._core.GetLogs(0, e.Args[0]);
                    title = logs.Length + " Log Entries for " + Format.UppercaseFirst(e.Args[0]);
                }
            }
            else
            {
                // No arguments, let's just fetch the last entries
                logs = this._core.GetLogs(max);
                title = logs.Length + " Last Log Entries";
            }
            if (logs.Length == 0)
            {
                bot.SendReply(e, "No log entries found");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle(title);
            foreach (RaidCore.LogEntry log in logs)
            {
                window.AppendHighlight("[" + log.Time.ToString("dd/MM/yyyy hh:mm:ss") + "] ");
                window.AppendNormalStart();

                if (log.RaidID > 0)
                {
                    window.AppendString("[");
                    window.AppendColorString(colors["raid"], "Raid #" + log.RaidID);
                    window.AppendString("] ");
                }

                window.AppendString("[");
                if (colors.ContainsKey(log.Type.ToLower()))
                    window.AppendColorString(colors[log.Type.ToLower()], Format.UppercaseFirst(log.Type));
                else
                    window.AppendString(Format.UppercaseFirst(log.Type));
                window.AppendString("] ");

                window.AppendString(log.Message);
                
                if (log.Admin != null && log.Admin != string.Empty)
                    window.AppendString(" (Admin: " + log.Admin + ")");
             
                window.AppendColorEnd();
                window.AppendLineBreak();
            }
            bot.SendReply(e, title + " »» ", window);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "raid history":
                    return "Displays a list of all previous raids and links to other available statistics and logs.\n" +
                        "Usage: /tell " + bot.Character + " raid history";
                case "raid logs":
                    return "Displays various logs the bot keeps track of.\n" +
                        "If the command is called without any arguments it will display the last 250 log entries.\n" +
                        "If a raid ID is specified, it will display all log entries related to that specific raid.\n" +
                        "If a username is specified it will display all log entries related to that specific user.\n" +
                        "Usage: /tell " + bot.Character + " raid logs [query]";
            }
            return null;
        }
    }
}
