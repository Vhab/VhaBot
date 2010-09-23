using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Timers;
using AoLib.Utils;
using VhaBot;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class RaidGlyphs : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private Dictionary<int, string> _items;
        private Dictionary<string, int> _professions;

        private bool _raffleActive = false;
        private Dictionary<int, int> _raffleItems = new Dictionary<int,int>();
        private Dictionary<int, List<string>> _raffleJoined = new Dictionary<int,List<string>>();
        private List<string> _raffleMains = new List<string>();
        private int _raffleTime = 0;
        private Timer _raffleTimer;
        private BotShell _bot;
        private RichTextWindow _raffleWindow;

        public RaidGlyphs()
        {
            this.Name = "Raid :: Glyphs";
            this.InternalName = "RaidGlyphs";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Tracks posted glyphs and raffles them";
            this.Commands = new Command[] {
                new Command("glyphs", true, UserLevel.Leader),
                new Command("glyphs clear", true, UserLevel.Leader),
                new Command("glyphs raffle", true, UserLevel.Leader),
                new Command("glyphs join", true, UserLevel.Member),
                new Command("glyphs leave", true, UserLevel.Member),
                new Command("glyphs abort", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }
            bot.Events.PrivateChannelMessageEvent += new PrivateChannelMessageHandler(OnPrivateChannelMessageEvent);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS glyphs (id INT NOT NULL AUTO_INCREMENT, item INT NOT NULL, looter VARCHAR(255), time INT NOT NULL, visible ENUM('true','false') NOT NULL, PRIMARY KEY (id))");

            this._items = new Dictionary<int, string>();
            this._items.Add(218348, "Blue Glyph of Aban");
            this._items.Add(218349, "Blue Glyph of Enel");
            this._items.Add(218350, "Blue Glyph of Ocra");

            this._professions = new Dictionary<string, int>();
            this._professions.Add("enforcer", 218348);
            this._professions.Add("engineer", 218348);
            this._professions.Add("keeper", 218348);
            this._professions.Add("martial artist", 218348);
            this._professions.Add("shade", 218348);

            this._professions.Add("agent", 218349);
            this._professions.Add("bureaucrat", 218349);
            this._professions.Add("nano-technician", 218349);
            this._professions.Add("soldier", 218349);

            this._professions.Add("adventurer", 218350);
            this._professions.Add("doctor", 218350);
            this._professions.Add("fixer", 218350);
            this._professions.Add("meta-physicist", 218350);
            this._professions.Add("trader", 218350);

            this._bot = bot;
            this._raffleTimer = new Timer();
            this._raffleTimer.Interval = 1000;
            this._raffleTimer.Elapsed += new ElapsedEventHandler(OnRaffleTimer);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.PrivateChannelMessageEvent -= new PrivateChannelMessageHandler(OnPrivateChannelMessageEvent);
        }

        public void OnPrivateChannelMessageEvent(BotShell bot, PrivateChannelMessageArgs e)
        {
            if (e.Items.Length < 1)
                return;
            if (e.Self)
                return;
            if (!e.Local)
                return;
            foreach (AoItem item in e.Items)
            {
                lock (this._items)
                {
                    if (!this._items.ContainsKey(item.LowID))
                        continue;
                    this._database.ExecuteNonQuery(String.Format("INSERT INTO glyphs SET item = {0}, looter = '{1}', time = {2}, visible = 'true'", item.LowID, e.Sender, TimeStamp.Now));
                    bot.SendPrivateChannelMessage(bot.ColorHeader + this._items[item.LowID] + bot.ColorHighlight + " has been looted by " + bot.ColorHeader + e.Sender);
                }
            }
        }

        public void OnRaffleTimer(object sender, ElapsedEventArgs e)
        {
            if (!this._raffleActive)
            {
                this._raffleTimer.Stop();
                return;
            }
            this._raffleTime--;
            if (this._raffleTime == 30 || this._raffleTime == 15)
            {
                this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + "The glyph raffle is closing in " + HTML.CreateColorString(this._bot.ColorHeaderHex, this._raffleTime.ToString()) + " seconds »» " + this._raffleWindow.ToString());
                return;
            }
            if (this._raffleTime <= 0)
            {
                try
                {
                    lock (this._raffleItems)
                    {
                        Random random = new Random();
                        string winners = this._bot.ColorHighlight + "\n¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯";
                        foreach (KeyValuePair<int, int> kvp in this._raffleItems)
                        {
                            lock (this._raffleJoined)
                            {
                                lock (this._items)
                                    winners += "\n    " + this._bot.ColorHeader + this._items[kvp.Key] + ": " + this._bot.ColorHighlight;
                                for (int i = 0; i < kvp.Value; i++)
                                {
                                    if (this._raffleJoined[kvp.Key].Count > 0)
                                    {
                                        this._raffleJoined[kvp.Key].Sort();
                                        int winner = random.Next(0, this._raffleJoined[kvp.Key].Count);
                                        winners += this._raffleJoined[kvp.Key][winner] + " ";
                                        this._raffleJoined[kvp.Key].RemoveAt(winner);
                                    }
                                }
                            }
                        }
                        winners += "\n______________________________________________";
                        this._bot.SendPrivateChannelMessage(winners);
                        this._raffleTimer.Stop();
                        this._raffleActive = false;
                        this._raffleTime = 0;
                        this._raffleItems = null;
                        lock (this._raffleJoined)
                            this._raffleJoined.Clear();
                        lock (this._raffleMains)
                            this._raffleMains.Clear();
                        this._raffleWindow = null;
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "glyphs":
                    this.OnGlyphsCommand(bot, e);
                    break;
                case "glyphs clear":
                    this.OnGlyphsClearCommand(bot, e);
                    break;
                case "glyphs raffle":
                    this.OnGlyphsRaffleCommand(bot, e);
                    break;
                case "glyphs join":
                    this.OnGlyphsJoinCommand(bot, e);
                    break;
                case "glyphs leave":
                    this.OnGlyphsLeaveCommand(bot, e);
                    break;
                case "glyphs abort":
                    this.OnGlyphsAbortCommand(bot, e);
                    break;
            }
        }

        private void OnGlyphsCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            string raffle = string.Empty;
            bool empty = true;
            lock (this._items)
            {
                foreach (KeyValuePair<int, string> kvp in this._items)
                {
                    window.AppendHeader(kvp.Value);
                    int i = 0;
                    lock (this._database.Connection)
                    {
                        using (IDbCommand command = this._database.Connection.CreateCommand())
                        {
                            command.CommandText = "SELECT id, looter, time FROM glyphs WHERE item = " + kvp.Key + " AND visible = 'true'";
                            IDataReader reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                i++;
                                window.AppendNormal(Format.DateTime(reader.GetInt64(2), FormatStyle.Compact)+" ");
                                window.AppendHighlight(reader.GetString(1));
                                window.AppendLineBreak();
                                empty = false;
                            }
                            reader.Close();
                        }
                    }
                    if (i == 0)
                    {
                        window.AppendHighlight("None");
                        window.AppendLineBreak();
                    }
                    window.AppendLineBreak();
                    raffle += " " + i;
                }
            }

            if (!empty)
            {
                window.AppendHeader("Options");
                window.AppendBotCommand("Start Raffle", "glyphs raffle" + raffle);
                window.AppendLineBreak();
                window.AppendBotCommand("Clear List", "glyphs clear");
                bot.SendReply(e, "Glyphs List »» ", window);
            }
            else
            {
                bot.SendReply(e, "The glyphs list is current empty");
            }
        }

        private void OnGlyphsClearCommand(BotShell bot, CommandArgs e)
        {
            this._database.ExecuteNonQuery("UPDATE glyphs SET visible = 'false'");
            bot.SendReply(e, "The glyph list has been cleared");
        }

        private void OnGlyphsRaffleCommand(BotShell bot, CommandArgs e)
        {
            if (this._raffleActive)
            {
                bot.SendReply(e, "There's currently already a glyph raffle active");
                return;
            }
            Dictionary<int, int> items = new Dictionary<int, int>();
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Glyph Raffle");
            lock (this._items)
            {
                if (e.Args.Length != this._items.Count)
                {
                    bot.SendReply(e, "You need to specify " + this._items.Count + " numbers");
                    return;
                }
                if (this._items.Count == 0)
                {
                    bot.SendReply(e, "This plugin hasn't been properly configured for raffling");
                    return;
                }

                int i = 0;
                this._raffleJoined.Clear();
                foreach (KeyValuePair<int, string> kvp in this._items)
                {
                    try
                    {
                        items.Add(kvp.Key, Convert.ToInt32(e.Args[i]));
                        if (items[kvp.Key] > 0)
                        {
                            window.AppendHighlight(kvp.Value + " ");
                            window.AppendNormal("(" + items[kvp.Key].ToString() + " total) [");
                            window.AppendBotCommand("Join", "glyphs join " + kvp.Key);
                            window.AppendNormal("]");
                            window.AppendLineBreak();
                            this._raffleJoined.Add(kvp.Key, new List<string>());
                        }
                    }
                    catch
                    {
                        this._raffleJoined.Clear();
                        bot.SendReply(e, "Invalid value specified: " + e.Args[i]);
                        return;
                    }
                    i++;
                }
            }
            this._raffleTime = 60;
            this._raffleItems = items;
            this._raffleJoined = new Dictionary<int, List<string>>();
            this._raffleActive = true;
            this._raffleTimer.Start();
            this._raffleWindow = window;
            string output = bot.ColorHighlight + "\n¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯\n    " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has started a glyph raffle »» " + window.ToString();
            lock (this._items)
            {
                foreach (KeyValuePair<int, string> kvp in this._items)
                {
                    if (items[kvp.Key] > 0)
                    {
                        lock (this._raffleJoined)
                            this._raffleJoined.Add(kvp.Key, new List<string>());
                        output += "\n    " + HTML.CreateColorString(bot.ColorHeaderHex, items[kvp.Key].ToString()) + " " + kvp.Value;
                    }
                }
            }
            output += "\n______________________________________________";
            bot.SendPrivateChannelMessage(output);
            bot.SendReply(e, "You have started the glyph raffle");
        }

        private void OnGlyphsAbortCommand(BotShell bot, CommandArgs e)
        {
            if (!this._raffleActive)
            {
                bot.SendReply(e, "There's no glyph raffle active");
                return;
            }
            this._raffleTimer.Stop();
            this._raffleActive = false;
            this._raffleTime = 0;
            this._raffleItems = null;
            lock (this._raffleJoined)
                this._raffleJoined.Clear();
            lock (this._raffleMains)
                this._raffleMains.Clear();
            this._raffleWindow = null;
            bot.SendReply(e, "You have aborted the glyph raffle");
        }

        private void OnGlyphsJoinCommand(BotShell bot, CommandArgs e)
        {
            if (!this._raffleActive)
            {
                bot.SendReply(e, "There's no glyph raffle active");
                return;
            }

            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Glyph Raffle »» ", this._raffleWindow);
                return;
            }
            int id = 0;
            string username = bot.Users.GetMain(e.Sender);
            try { id = Convert.ToInt32(e.Args[0]); }
            catch
            {
                bot.SendReply(e, "Invalid ID");
                return;
            }
            lock (this._raffleMains)
            {
                if (this._raffleMains.Contains(username))
                {
                    bot.SendReply(e, "You can only join 1 raffle");
                    return;
                }
            }
            lock (this._raffleJoined)
            {
                if (!this._raffleJoined.ContainsKey(id))
                {
                    bot.SendReply(e, "There's no item with that ID in the current glyph raffle");
                    return;
                }
                if (this._raffleJoined[id].Contains(e.Sender))
                {
                    bot.SendReply(e, "You already joined this raffle");
                    return;
                }
                lock (this._professions)
                {
                    if (this._professions.ContainsValue(id))
                    {
                        WhoisResult whois = XML.GetWhois(e.Sender, bot.Dimension);
                        if (whois == null || !whois.Success)
                        {
                            bot.SendReply(e, "Unable to get your profession. This is required to join this raffle. Please try again later");
                            return;
                        }
                        if (!this._professions.ContainsKey(whois.Stats.Profession.ToLower()))
                        {
                            bot.SendReply(e, "Your profession was not found in our database. Please contact an administrator if you feel this is an error");
                            return;
                        }
                        if (this._professions[whois.Stats.Profession.ToLower()] != id)
                        {
                            bot.SendReply(e, "Profession restrictions prevent you from joining this raffle");
                            return;
                        }
                    }
                }
                this._raffleJoined[id].Add(e.Sender);
                lock (this._raffleMains)
                    this._raffleMains.Add(username);
            }
            bot.SendReply(e, "You have joined the raffle");
            return;
        }

        private void OnGlyphsLeaveCommand(BotShell bot, CommandArgs e)
        {
            string username = bot.Users.GetMain(e.Sender);
            lock (this._raffleMains)
            {
                if (!this._raffleMains.Contains(username))
                {
                    bot.SendReply(e, "You haven't joined the raffle");
                    return;
                }
            }
            lock (this._raffleJoined)
            {
                foreach (KeyValuePair<int, List<string>> kvp in this._raffleJoined)
                {
                    if (kvp.Value.Contains(e.Sender))
                    {
                        kvp.Value.Remove(e.Sender);
                        lock (this._raffleMains)
                            this._raffleMains.Remove(username);
                        bot.SendReply(e, "You have left the raffle");
                        return;
                    }
                }
            }
            bot.SendReply(e, "Unable to remove you from the raffle. Maybe you joined with a different character?");
            return;
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "glyphas":
                    return "Lists the looted glyphs during the raid, and the person who is holding it.\nFor a glyph to be registered, it must have been posted in the private channel by the looter.\n" +
                        "Usage: /tell " + bot.Character + " glyphs";
                case "glyphs list":
                    return "Clears the list of looted glyphs.\n" +
                        "Usage: /tell " + bot.Character + " glyphs clear";
                case "glyphs raffle":
                    return "Starts a raffle for [aban] Blue Glyphs of Aban, [enel] Blue Glyphs of Enel, and [ocra] Blue Glyphs of Ocra. The raffle lasts 1 minute.\n" +
                        "During this 1 minute period people can join the glyphs raffle for a glyph usable by their profession.\nAfter 1 minute the bot will randomly select winners from all the people that have joined the raffle\n" +
                        "Usage: /tell " + bot.Character + " glyphs raffle [aban] [enel] [ocra]";
                case "glyphs join":
                    return "Allows you to join the current glyphs raffle. You can only join the raffle for the glyph usable by your profession.\n" +
                        "Usage: /tell " + bot.Character + " glyphs join [id]";
                case "glyphs leave":
                    return "Allows you to leave the current glyphs raffle.\n" +
                        "Usage: /tell " + bot.Character + " glyphs leave";
            }
            return null;
        }
    }
}
