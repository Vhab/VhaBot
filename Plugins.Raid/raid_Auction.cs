using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Data;
using AoLib.Utils;
using VhaBot;
using VhaBot.Communication;

namespace VhaBot.Plugins
{
    public class RaidAuction : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private BotShell _bot;

        private int _auctionDuration = 60;
        private int _ninjaDuration = 20;
        private Timer _timer;

        private string _item;
        private bool _running = false;
        private int _currentBid = 0;
        private int _proxyBid = 0;
        private int _timeLeft = 0;
        private string _bidder =  null;
        private string _admin = null;

        private AoItem _realItem = null;

        public RaidAuction()
        {
            this.Name = "Raid :: Auction";
            this.InternalName = "RaidAuction";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Manages auctioning loot";
            this.Commands = new Command[] {
                new Command("auction", true, UserLevel.Leader, UserLevel.Member, UserLevel.Leader),
                new Command("auction start", true, UserLevel.Leader),
                new Command("auction abort", true, UserLevel.Leader),
                new Command("bid", true, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
                new Command("loot", true, UserLevel.Leader, UserLevel.Member, UserLevel.Leader),
                new Command("remains of", "loot")
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

            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS loot_history (id INT NOT NULL AUTO_INCREMENT, main VARCHAR(14), admin VARCHAR(14), points INT NOT NULL, date INT NOT NULL, item INT NOT NULL, PRIMARY KEY (id))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS loot (id INT NOT NULL AUTO_INCREMENT, name VARCHAR(255), monster VARCHAR(255), lowid INT NOT NULL, highid INT NOT NULL, ql INT NOT NULL, icon INT NOT NULL, visible ENUM('true','false') NOT NULL, PRIMARY KEY (id))");

            this._bot = bot;
            this._timer = new Timer();
            this._timer.Interval = 1000;
            this._timer.Elapsed += new ElapsedEventHandler(OnTimer);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            lock (this)
            {
                switch (e.Command)
                {
                    case "auction":
                        this.OnAuctionCommand(bot, e);
                        break;
                    case "auction start":
                        this.OnAuctionStartCommand(bot, e);
                        break;
                    case "auction abort":
                        this.OnAuctionAbortCommand(bot, e);
                        break;
                    case "bid":
                        this.OnBidCommand(bot, e);
                        break;
                    case "loot":
                        this.OnLootCommand(bot, e);
                        break;
                }
            }
        }

        private void OnAuctionCommand(BotShell bot, CommandArgs e)
        {
            if (!this._running)
            {
                bot.SendReply(e, "There is currently no auction running");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Auction");
            window.AppendHighlight("Item: ");
            window.AppendNormalStart();
            window.AppendRawString(this._item);
            window.AppendString(" [");
            window.AppendBotCommand("Abort", "auction abort");
            window.AppendString("]");
            window.AppendColorEnd();
            window.AppendLineBreak();
            window.AppendHighlight("High Bidder: ");
            if (this._currentBid > 0)
            {
                window.AppendNormal(this._bidder);
                window.AppendLineBreak();
                window.AppendHighlight("High Bid: ");
                window.AppendNormal(this._currentBid.ToString());
            }
            else
            {
                window.AppendNormal("N/A");
            }
            window.AppendLineBreak();
            double points = this._core.GetPoints(e.Sender);
            if (points > this._core.MinimumPoints)
            {
                window.AppendHighlight("Your Points: ");
                if (points > this._currentBid)
                    window.AppendColorString(RichTextWindow.ColorGreen, points.ToString());
                else
                    window.AppendColorString(RichTextWindow.ColorRed, points.ToString());
                window.AppendLineBreak();
            }
            window.AppendHighlight("Time Left: ");
            window.AppendNormal(this._timeLeft.ToString() + " seconds");
            window.AppendLineBreak();
            bot.SendReply(e, "Auction »» ", window);
        }

        private void OnAuctionStartCommand(BotShell bot, CommandArgs e)
        {
            // Special support for Loot Logger
            if (bot.Plugins.IsLoaded("RaidLootLog"))
            {
                bool lootlog = false;
                ReplyMessage reply = bot.SendPluginMessageAndWait(this.InternalName, "RaidLootLog", "started", 1000);
                if (reply != null) lootlog = (bool)reply.Args[0];
                if (lootlog)
                {
                    bot.SendReply(e, "You can't start an auction while the Loot Logger is running.");
                    return;
                }
            }
            if (this._running)
            {
                bot.SendReply(e, "An auction has already been started");
                return;
            }
            if (e.Words.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: auction start [item]");
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
            this._timeLeft = this._auctionDuration;
            this._currentBid = 0;
            this._proxyBid = 0;
            this._bidder = null;
            this._running = true;
            this._timer.Start();
            string output = string.Format("{0}\n¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯\n    {1}{2}{0} has started an auction for {1}{3}\n    {0}This auction will run for {1}{4}{0} seconds\n______________________________________________", bot.ColorHighlight, bot.ColorHeader, e.Sender, this._item, this._timeLeft);
            bot.SendPrivateChannelMessage(output);
            bot.SendReply(e, "You started an auction for " + bot.ColorHeader + this._item);
            this._core.Log(e.Sender, e.Sender, this.InternalName, "auction", e.Sender + " has started an auction for " + HTML.StripTags(this._item));
        }

        private void OnAuctionAbortCommand(BotShell bot, CommandArgs e)
        {
            if (!this._running)
            {
                bot.SendReply(e, "There is no auction to abort");
                return;
            }
            this._timer.Stop();
            this._running = false;
            this._item = null;
            this._timeLeft = 0;
            this._currentBid = 0;
            this._proxyBid = 0;
            this._bidder = null;
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has aborted the auction");
            bot.SendReply(e, "You aborted the auction");
            this._core.Log(e.Sender, e.Sender, this.InternalName, "auction", e.Sender + " has aborted the auction");
        }

        private void OnBidCommand(BotShell bot, CommandArgs e)
        {
            if (!this._running)
            {
                bot.SendReply(e, "There is no auction you can bid on");
                return;
            }
            if (e.Words.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: bid [amount]");
                return;
            }
            int amount = 0;
            try { amount = Convert.ToInt32(e.Args[0]); }
            catch { }
            if (this._core.GetPoints(e.Sender) < amount)
            {
                bot.SendReply(e, "You don't have that many points");
                return;
            }
            if (this._bidder == e.Sender)
            {
                bot.SendReply(e, "You already have the leading bid");
                return;
            }
            if (this._currentBid >= amount)
            {
                bot.SendReply(e, "You need to bid at least " + HTML.CreateColorString(bot.ColorHeaderHex, ((int)(this._currentBid + 1)).ToString()) + " points");
                return;
            }
            if (this._proxyBid >= amount)
            {
                if (this._proxyBid == amount)
                    this._currentBid = amount;
                else
                    this._currentBid = amount + 1;
                bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " tried to outbid " + HTML.CreateColorString(bot.ColorHeaderHex, this._bidder) + ", but " + HTML.CreateColorString(bot.ColorHeaderHex, this._bidder) + " is still leading with " + HTML.CreateColorString(bot.ColorHeaderHex, this._currentBid.ToString()) + " points");
                bot.SendReply(e, "You failed to outbid " + HTML.CreateColorString(bot.ColorHeaderHex, this._bidder));
                this._core.Log(e.Sender, e.Sender, this.InternalName, "auction", e.Sender + " has failed to outbid with " + amount + " points");
                return;
            }
            else
            {
                this._currentBid = this._proxyBid + 1;
                this._bidder = e.Sender;
                this._proxyBid = amount;
                if (this._timeLeft < this._ninjaDuration)
                    this._timeLeft = this._ninjaDuration;
                bot.SendReply(e, "You are now leading the auction with " + HTML.CreateColorString(bot.ColorHeaderHex, this._currentBid.ToString()) + " points");
                bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " is now leading the auction with " + HTML.CreateColorString(bot.ColorHeaderHex, this._currentBid.ToString()) + " points");
                this._core.Log(e.Sender, e.Sender, this.InternalName, "auction", e.Sender + "'s bid of " + amount + " points is leading the auction with " + this._currentBid + " points");
                return;
            }
        }

        private void OnLootCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length > 0)
            {
                string monster = e.Words[0].ToLower();
                monster = HTML.UnescapeString(monster);
                int results = 0;
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Loot results for '" + monster + "'");
                lock (this._database.Connection)
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT id, name, lowid, highid, ql, icon FROM loot WHERE monster LIKE '%" + Config.EscapeString(monster).Replace(" ", "%") + "%' AND visible = 'true' ORDER BY name ASC";
                        //SELECT count(points), min(points), max(points), avg(points) FROM loot_history WHERE item = 244718 AND points > 0
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            results++;
                            int icon = reader.GetInt32(5);
                            if (icon > 0)
                            {
                                window.AppendIcon(icon);
                                window.AppendLineBreak();
                            }
                            window.AppendItem(reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4));
                            window.AppendNormal(" [");
                            window.AppendBotCommand("Start Auction", "auction start item:" + reader.GetInt32(2) + ":" + reader.GetInt32(3) + ":" + reader.GetInt32(4) + " " + reader.GetString(1));
                            window.AppendNormal("]");
                            window.AppendLineBreak(2);
                        }
                        reader.Close();
                    }
                }
                if (results == 0)
                    bot.SendReply(e, "No loot results for '" + monster + "'");
                else
                    bot.SendReply(e, "Loot results for '" + monster + "' »» ", window);
                return;
            }
            RichTextWindow monsters = new RichTextWindow(bot);
            monsters.AppendTitle("Loot Information");
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT monster, count(*) FROM loot WHERE visible = 'true' GROUP BY monster ORDER BY monster ASC";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        monsters.AppendHighlight(reader.GetString(0));
                        monsters.AppendNormal(" (" + reader.GetInt32(1) + " items) [");
                        monsters.AppendBotCommand("View", "loot " + reader.GetString(0));
                        monsters.AppendNormal("]");
                        monsters.AppendLineBreak();

                    }
                    reader.Close();
                }
            }
            bot.SendReply(e, "Loot Information »» ", monsters);
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            lock (this)
            {
                if (!this._running)
                    return;
                this._timeLeft--;
                if (this._timeLeft == 30 || this._timeLeft == 15)
                {
                    string output = string.Format("{0}The auction for {1}{2}{0} is still running. This auction will end in {1}{3}{0} seconds!", this._bot.ColorHighlight, this._bot.ColorHeader, this._item, this._timeLeft);
                    this._bot.SendPrivateChannelMessage(output);
                }
                else if (this._timeLeft < 1)
                {
                    this._running = false;
                    if (this._currentBid < 1)
                    {
                        this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + "The auction for " + this._bot.ColorHeader + this._item + this._bot.ColorHighlight + " has ended without any bids");
                        this._core.Log(null, null, this.InternalName, "auction", "The auction for " + HTML.StripTags(this._item) + " has ended without any bids");
                    }
                    else
                    {
                        this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + HTML.CreateColorString(this._bot.ColorHeaderHex, this._bidder) + " has won the auction for " + this._bot.ColorHeader + this._item + this._bot.ColorHighlight + " with " + HTML.CreateColorString(this._bot.ColorHeaderHex, this._currentBid.ToString()) + " points");
                        this._core.RemovePoints(this._bidder, (double)this._currentBid);
                        if (this._realItem != null)
                            this._database.ExecuteNonQuery(string.Format("INSERT INTO loot_history SET main = '{0}', admin = '{1}', points = {2}, date = {3}, item = {4}", this._bidder, this._admin, this._currentBid, TimeStamp.Now, this._realItem.LowID));
                        this._core.Log(this._bidder, null, this.InternalName, "auction", this._bidder + " has won the auction for " + HTML.StripTags(this._item));
                    }
                    this._item = null;
                    this._timeLeft = 0;
                    this._currentBid = 0;
                    this._proxyBid = 0;
                    this._bidder = null;
                    this._timer.Stop();
                }
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "auction":
                    return "Shows the current state of the running auction.\n" +
                        "Usage: /tell " + bot.Character + " auction";
                case "auction start":
                    return "Starts an auction for [item]. An auction lasts 1 minute, during which everyone with sufficient points can bid on the item.\nThe player with the highest bid at the end of the auction wins the auctioned item. Only the winner is deduced points.\n" +
                        "Usage: /tell " + bot.Character + " auction start [item]";
                case "auction abort":
                    return "Aborts the ongoing auction. All bids will be ignored and no points will be deducted.\n" +
                        "Usage: /tell " + bot.Character + " auction abort";
                case "bid":
                    return "Allows you to bid [amount] points on the currently auctioned item.\nYou can't bid for more points than you currently have.\nBidding is done using proxy bids.\nThis means if someone bids 10 points and you bid 15 you will be leading with 11 points.\nIf someone else after that bids 13 you will still lead with 14 points.\nIf there's another bid for 20 points, that person will now lead with 16 points.\n" +
                        "Usage: /tell " + bot.Character + " bid [amount]";
                case "loot":
                    return "Displays an interface to browse the items in the bot's database.\n" +
                        "If the command is called without any arguments it will display an index of all known monsters in the database.\n"+
                        "Usage: /tell " + bot.Character + " loot [[monster]]";
            }
            return null;
        }
    }
}
