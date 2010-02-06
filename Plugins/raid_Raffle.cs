using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Data;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidRaffle : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private BotShell _bot;

        private string _item;
        private bool _running = false;
        private string _admin = null;
        private AoItem _realItem = null;
        private Dictionary<string, string> _joined = new Dictionary<string, string>();

        public RaidRaffle()
        {
            this.Name = "Raid :: Raffle";
            this.InternalName = "RaidRaffle";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Manages raffling loot";
            this.Commands = new Command[] {
                new Command("raffle start", false, UserLevel.Leader),
                new Command("raffle abort", false, UserLevel.Leader),
                new Command("raffle stop", false, UserLevel.Leader),
                new Command("raffle join", false, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
                new Command("raffle leave", false, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled)
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
            lock (this)
            {
                switch (e.Command)
                {
                    case "raffle start":
                        this.OnRaffleStartCommand(bot, e);
                        break;
                    case "raffle join":
                        this.OnRaffleJoinCommand(bot, e);
                        break;
                    case "raffle leave":
                        this.OnRaffleLeaveCommand(bot, e);
                        break;
                    case "raffle stop":
                        this.OnRaffleStopCommand(bot, e);
                        break;
                    case "raffle abort":
                        this.OnRaffleAbortCommand(bot, e);
                        break;
                }
            }
        }

        private void OnRaffleStartCommand(BotShell bot, CommandArgs e)
        {
            if (this._running)
            {
                bot.SendReply(e, "A raffle has already been started");
                return;
            }
            if (e.Words.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: raffle start [item]");
                return;
            }
            this._item = e.Words[0];
            this._realItem = null;
            this._admin = e.Sender;
            if (e.Items.Length > 0)
            {
                this._item = e.Items[0].ToLink();
                this._realItem = e.Items[0];
            }
            else if (e.Args[0].StartsWith("item:") && e.Args.Length > 1)
            {
                string[] tmp = e.Args[0].Substring(5).Split(':');
                if (tmp.Length == 3)
                {
                    try
                    {
                        int lowID = Convert.ToInt32(tmp[0]);
                        int highID = Convert.ToInt32(tmp[0]);
                        int ql = Convert.ToInt32(tmp[0]);
                        string name = HTML.UnescapeString(e.Words[1]);
                        this._realItem = new AoItem(name, lowID, highID, ql, null);
                        this._item = this._realItem.ToLink();
                    }
                    catch { }
                }
            }
            this._running = true;

            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Raffle");
            window.AppendLineBreak();
            window.AppendNormalStart();
            window.AppendString("[");
            window.AppendBotCommand("Join", "raffle join");
            window.AppendString("] [");
            window.AppendBotCommand("Leave", "raffle leave");
            window.AppendString("]");

            string output = string.Format("{1}{2}{0} has started a raffle for {1}{3}{0} »» ", bot.ColorHighlight, bot.ColorHeader, e.Sender, this._item) + window.ToString();
            bot.SendPrivateChannelMessage(output);

            window = new RichTextWindow(bot);
            window.AppendTitle("Raffle");
            window.AppendLineBreak();
            window.AppendNormalStart();
            window.AppendString("[");
            window.AppendBotCommand("Abort", "raffle abort");
            window.AppendString("] [");
            window.AppendBotCommand("Finish & Announce Winner", "raffle stop");
            window.AppendString("]");

            bot.SendReply(e, "You started a raffle for " + bot.ColorHeader + this._item + bot.ColorHighlight + " »» ", window);
        }

        private void OnRaffleAbortCommand(BotShell bot, CommandArgs e)
        {
            if (!this._running)
            {
                bot.SendReply(e, "There is no raffle to abort");
                return;
            }
            this._running = false;
            this._item = null;
            this._realItem = null;
            this._admin = null;
            lock (this._joined)
                this._joined.Clear();
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has aborted the raffle");
            bot.SendReply(e, "You aborted the raffle");
        }

        private void OnRaffleJoinCommand(BotShell bot, CommandArgs e)
        {
            if (!this._running)
            {
                bot.SendReply(e, "There is no raffle to join");
                return;
            }
            string main = bot.Users.GetMain(e.Sender);
            lock (this._joined)
            {
                if (this._joined.ContainsKey(main))
                {
                    bot.SendReply(e, "You have already joined the raffle");
                    return;
                }
                this._joined.Add(main, e.Sender);
                bot.SendReply(e, "You have joined the raffle");
                bot.SendPrivateChannelMessage(bot.ColorHeader + e.Sender + bot.ColorHighlight + " has joined the raffle");
            }
        }

        private void OnRaffleLeaveCommand(BotShell bot, CommandArgs e)
        {
            string main = bot.Users.GetMain(e.Sender);
            lock (this._joined)
            {
                if (!this._joined.ContainsKey(main))
                {
                    bot.SendReply(e, "You haven't joined the raffle");
                    return;
                }
                this._joined.Remove(main);
                bot.SendReply(e, "You have left the raffle");
                bot.SendPrivateChannelMessage(bot.ColorHeader + e.Sender + bot.ColorHighlight + " has left the raffle");
            }
        }

        private void OnRaffleStopCommand(BotShell bot, CommandArgs e)
        {
            if (!this._running)
            {
                bot.SendReply(e, "There is no raffle to finish");
                return;
            }
            lock (this._joined)
            {
                bot.SendReply(e, "You have ended the raffle");
                if (this._joined.Count == 0)
                {
                    bot.SendPrivateChannelMessage(bot.ColorHighlight + "The raffle has ended without any winners");
                }
                else
                {
                    string[] keys = new string[this._joined.Keys.Count];
                    this._joined.Keys.CopyTo(keys, 0);
                    Random random = new Random();
                    int winner = random.Next(0, keys.Length);
                    bot.SendPrivateChannelMessage(bot.ColorHeader + this._joined[keys[winner]] + bot.ColorHighlight + " has won the raffle for " + bot.ColorHeader + this._item);
                }
                this._running = false;
                this._item = null;
                this._realItem = null;
                this._admin = null;
                this._joined.Clear();
            }
        }
    }
}
