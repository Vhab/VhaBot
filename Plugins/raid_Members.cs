using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidEventArgs : EventArgs
    {
        public readonly string Raider = string.Empty;
        public RaidEventArgs(string Raider)
        {
            this.Raider = Raider;
        }
    }
    public delegate void RaidEventHandler(object sender, RaidEventArgs e);

    public class RaidMembers : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private System.Timers.Timer _timer;
        private BotShell _bot;

        private Dictionary<string, Raider> _bans = new Dictionary<string, Raider>();
        private Dictionary<string, Raider> _lds = new Dictionary<string, Raider>();
        private bool CreditsEnabled = false;
        private int CreditsMax = 9;
        private int CreditsType = -1;
        private int MainsLevelReq = 0;
        private int AltsLevelReq = 0;

        public event RaidEventHandler LeftEvent;
        public event RaidEventHandler KickedEvent;

        public RaidMembers()
        {
            this.Name = "Raid :: Members";
            this.InternalName = "RaidMembers";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Manages adding and removing members from the raid";
            this.Commands = new Command[] {
                new Command("raid join", true, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
                new Command("raid leave", true, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
                new Command("raid add", true, UserLevel.Leader),
                new Command("raid kick", true, UserLevel.Leader),
                new Command("raid bans", true, UserLevel.Leader),
                new Command("raid ban", true, UserLevel.Leader),
                new Command("raid unban", true, UserLevel.Leader),
                new Command("raid list", true, UserLevel.Leader),
                new Command("raid credits", true, UserLevel.Leader),
                new Command("credits", true, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
                new Command("credits reset", true, UserLevel.Admin),
                new Command("credits add", true, UserLevel.Admin),
                new Command("credits remove", true, UserLevel.Admin),
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

            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS credits (main VARCHAR(20) NOT NULL, type INT NOT NULL, credits INT NOT NULL, lastRaidID INT NOT NULL, PRIMARY KEY (main, type))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS credits_types (type INT NOT NULL, name VARCHAR(50) NOT NULL, description VARCHAR(255) NOT NULL, credits INT NOT NULL, PRIMARY KEY (type))");
            this._bot = bot;

            this._core.StoppedEvent += new EventHandler(StoppedEvent);
            this._core.StartedEvent += new EventHandler(StartedEvent);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(ConfigurationChangedEvent);
            bot.Events.UserLeaveChannelEvent += new UserLeaveChannelHandler(UserLeaveChannelEvent);
            bot.Events.UserJoinChannelEvent += new UserJoinChannelHandler(UserJoinChannelEvent);

            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "autoban", "Automatically Raid-Ban Kicked Users", false);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "banduraction", "Default Raid-Ban Duration", 5);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "creditsenabled", "Enable Raid Credits System", this.CreditsEnabled);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "creditsmax", "Maximum Amount of Raid Credits spent", this.CreditsMax);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "mainslevelreq", "Level Requirement to Join the Raid (Mains)", 0);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "altslevelreq", "Level Requirement to Join the Raid (Alts)", 0);
            this.ReloadConfig(bot);
            this._timer = new System.Timers.Timer();
            this._timer.Interval = 1000;
            this._timer.AutoReset = true;
            this._timer.Elapsed += new System.Timers.ElapsedEventHandler(TimerEvent);
            this._timer.Start();
        }

        public override void OnUnload(BotShell bot)
        {
            this._core.StoppedEvent -= new EventHandler(StoppedEvent);
            this._core.StartedEvent -= new EventHandler(StartedEvent);
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(ConfigurationChangedEvent);
            bot.Events.UserLeaveChannelEvent -= new UserLeaveChannelHandler(UserLeaveChannelEvent);
            bot.Events.UserJoinChannelEvent -= new UserJoinChannelHandler(UserJoinChannelEvent);
        }

        private void ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
            {
                this.ReloadConfig(bot);
            }
        }

        private void ReloadConfig(BotShell bot)
        {
            this.CreditsEnabled = bot.Configuration.GetBoolean(this.InternalName, "creditsenabled", this.CreditsEnabled);
            this.CreditsMax = bot.Configuration.GetInteger(this.InternalName, "creditsmax", this.CreditsMax);
            this.MainsLevelReq = bot.Configuration.GetInteger(this.InternalName, "mainslevelreq", 0);
            this.AltsLevelReq = bot.Configuration.GetInteger(this.InternalName, "altslevelreq", 0);
        }

        private void StoppedEvent(object sender, EventArgs e)
        {
            lock (this._bans)
                this._bans.Clear();
        }

        private void StartedEvent(object sender, EventArgs e)
        {
            if (this.CreditsEnabled)
            {
                if (!this._core.Locked)
                {
                    this._core.Locked = true;
                    //this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + "The raid has been " + HTML.CreateColorString(RichTextWindow.ColorRed, "locked") + " pending raid credits type selection");
                }
                this.CreditsType = -1;
                RichTextWindow window = new RichTextWindow(this._bot);
                window.AppendTitle("Raid Credits Selection");
                window.AppendHighlight("The raid credits system has been enabled.");
                window.AppendLineBreak();
                window.AppendHighlight("Please select which type of raid credits should be used for this raid.");
                window.AppendLineBreak(2);
                lock (this._database.Connection)
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT type, name, description, credits FROM credits_types";
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            window.AppendHeader(reader.GetString(1));
                            window.AppendHighlight(reader.GetString(2));
                            window.AppendLineBreak();
                            window.AppendNormal("[");
                            window.AppendBotCommand("Select", "raid credits " + reader.GetInt32(0));
                            window.AppendNormal("]");
                            window.AppendLineBreak(2);
                        }
                        reader.Close();
                    }
                }
                window.AppendHeader("No Credits");
                window.AppendHighlight("Selecting this option will cause no raid credits to be charged this raid.");
                window.AppendLineBreak();
                window.AppendHighlight("This option is useful for unannounced or unplanned raids.");
                window.AppendLineBreak();
                window.AppendNormal("[");
                window.AppendBotCommand("Select", "raid unlock");
                window.AppendNormal("]");
                window.AppendLineBreak(2);
                this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + "Please select raid credits type »» " + window.ToString());
            }
        }

        private void UserLeaveChannelEvent(BotShell bot, UserLeaveChannelArgs e)
        {
            if (!e.Local)
                return;
            if (this._core.IsRaider(e.Sender))
            {
                this._core.RemoveRaider(e.Sender, true);
                lock (this._lds)
                {
                    Raider raider = new Raider();
                    raider.Time = DateTime.Now;
                    raider.Character = e.Sender;
                    raider.Duration = 5;
                    this._lds.Add(e.Sender, raider);
                }
            }
        }

        private void UserJoinChannelEvent(BotShell bot, UserJoinChannelArgs e)
        {
            if (!e.Local)
                return;
            lock (this._lds)
            {
                if (this._lds.ContainsKey(e.Sender))
                {
                    if (this._core.AddRaider(e.Sender, false))
                    {
                        bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has rejoined the raid");
                        bot.SendPrivateMessage(e.SenderID, bot.ColorHighlight + "Welcome back. You have automatically rejoined the raid");
                        this._core.Log(e.Sender, null, this.InternalName, "raiders", string.Format("{0} has automatically rejoined the raid (Points: {1})", e.Sender, this._core.GetPoints(e.Sender)));
                    }
                    this._lds.Remove(e.Sender);
                }
            }
        }

        private void TimerEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<string> expire = new List<string>();
            lock (this._bans)
            {
                foreach (KeyValuePair<string, Raider> ban in this._bans)
                {
                    if (DateTime.Now > ban.Value.Time.AddMinutes(ban.Value.Duration))
                    {
                        expire.Add(ban.Key);
                    }
                }
                foreach (string raider in expire)
                {
                    this._bans.Remove(raider);
                    this._bot.SendPrivateMessage(raider, this._bot.ColorHighlight + "Your access to the raid has been restored");
                    this._core.Log(raider, null, this.InternalName, "bans", string.Format("{0} has been automatically unbanned from the raid", raider));
                }
            }
            expire.Clear();
            lock (this._lds)
            {
                foreach (KeyValuePair<string, Raider> ld in this._lds)
                {
                    if (DateTime.Now > ld.Value.Time.AddMinutes(ld.Value.Duration))
                    {
                        expire.Add(ld.Key);
                    }
                }
            }
            foreach (string raider in expire)
                this._lds.Remove(raider);
        }

        private void Events_BotStateChangedEvent(BotShell sender, BotStateChangedArgs e) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (!this._database.CheckDatabase(bot, e))
                return;

            switch (e.Command)
            {
                case "raid join":
                    this.OnRaidJoinCommand(bot, e);
                    break;
                case "raid leave":
                    this.OnRaidLeaveCommand(bot, e);
                    break;
                case "raid add":
                    this.OnRaidAddCommand(bot, e);
                    break;
                case "raid kick":
                    this.OnRaidKickCommand(bot, e);
                    break;
                case "raid bans":
                    this.OnRaidBansCommand(bot, e);
                    break;
                case "raid ban":
                    this.OnRaidBanCommand(bot, e);
                    break;
                case "raid unban":
                    this.OnRaidUnbanCommand(bot, e);
                    break;
                case "raid list":
                    this.OnRaidListCommand(bot, e);
                    break;
                case "raid credits":
                    this.OnRaidCreditsCommand(bot, e);
                    break;
                case "credits":
                    this.OnCreditsCommand(bot, e);
                    break;
                case "credits reset":
                    this.OnCreditsResetCommand(bot, e);
                    break;
                case "credits add":
                    this.OnCreditsAddCommand(bot, e);
                    break;
                case "credits remove":
                    this.OnCreditsRemoveCommand(bot, e);
                    break;
            }
        }

        private void OnRaidJoinCommand(BotShell bot, CommandArgs e)
        {
            if (!bot.PrivateChannel.IsOn(e.Sender))
            {
                bot.SendReply(e, "You're required to be on the private channel to join the raid");
                return;
            }
            if (!this._core.Running)
            {
                bot.SendReply(e, "There is currently no raid active");
                return;
            }
            if (this._core.IsRaider(e.Sender))
            {
                bot.SendReply(e, "You're already in the raid");
                return;
            }
            if (this._core.Locked)
            {
                bot.SendReply(e, "The raid is currently locked");
                return;
            }
            string username = bot.Users.GetMain(e.Sender);
            if (this._core.IsRaider(username))
            {
                bot.SendReply(e, "You already have a character on this raid");
                return;
            }
            lock (this._bans)
            {
                if (this._bans.ContainsKey(username))
                {
                    bot.SendReply(e, "Your access to the raid has been temporarily disabled");
                    return;
                }
            }
            if (this.AltsLevelReq > 0 || this.MainsLevelReq > 0)
            {
                int level = 0;
                WhoisResult whois = XML.GetWhois(e.Sender, bot.Dimension);
                if (whois != null && whois.Success)
                    level = whois.Stats.Level;
                if (bot.Users.IsAlt(e.Sender))
                {
                    if (level < this.AltsLevelReq)
                    {
                        bot.SendReply(e, "Alts are required to be at least level " + HTML.CreateColorString(bot.ColorHeaderHex, this.AltsLevelReq.ToString()) + " in order to join the raid");
                        return;
                    }
                }
                else
                {
                    if (level < this.MainsLevelReq)
                    {
                        bot.SendReply(e, "Mains are required to be at least level " + HTML.CreateColorString(bot.ColorHeaderHex, this.MainsLevelReq.ToString()) + " in order to join the raid");
                        return;
                    }
                }
            }
            int credits = 0;
            int max = 0;
            bool charged = false;
            if (this.CreditsEnabled && !this.WithdrawCredit(bot, username, out credits, out max, out charged))
            {
                bot.SendReply(e, "You don't have enough raid credits to join this raid");
                return;
            }
            this._core.AddRaider(e.Sender, true);
            if (charged)
            {
                bot.SendPrivateMessage(e.Sender, bot.ColorHighlight + "You have been charged a raid credit for this raid. You have used " + HTML.CreateColorString(bot.ColorHeaderHex, credits + "/" + max) + " raid credits for this type of raid");
                this._core.Log(e.Sender, null, this.InternalName, "credits", string.Format("{0} has been charged a raid credit (Credits Left: {1})", e.Sender, credits));
            }
        }

        private void OnRaidAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: raid add [username]");
                return;
            }
            if (!this._core.Running)
            {
                bot.SendReply(e, "There is currently no raid active");
                return;
            }
            string raider = Format.UppercaseFirst(e.Args[0]);
            if (!bot.PrivateChannel.IsOn(raider))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " is required to be on the private channel to join the raid");
                return;
            }
            if (this._core.IsRaider(raider))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " is already in the raid");
                return;
            }
            lock (this._bans)
            {
                string main = bot.Users.GetMain(raider);
                if (this._bans.ContainsKey(main))
                {
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " is currently banned from the raid");
                    return;
                }
            }

            int credits = 0;
            int max = 0;
            bool charged = false;
            if (this.CreditsEnabled && !this.WithdrawCredit(bot, raider, out credits, out max, out charged))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " doesn't have enough raid credits to join this raid");
                return;
            }

            this._core.AddRaider(raider, false);
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, raider) + " has been manually added to the raid");
            bot.SendPrivateMessage(raider, bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has added you to the raid");
            this._core.Log(raider, e.Sender, this.InternalName, "raiders", string.Format("{0} has been added the raid (Points: {1})", raider, this._core.GetPoints(raider)));
            if (charged)
            {
                bot.SendPrivateMessage(raider, bot.ColorHighlight + "You have been charged a raid credit for this raid. You have used " + HTML.CreateColorString(bot.ColorHeaderHex, credits + "/" + max) + " raid credits for this type of raid");
                this._core.Log(raider, e.Sender, this.InternalName, "credits", string.Format("{0} has been charged a raid credit (Credits Left: {1})", raider, credits));
            }
        }

        private void OnRaidLeaveCommand(BotShell bot, CommandArgs e)
        {
            if (!this._core.Running)
            {
                bot.SendReply(e, "There is currently no raid active");
                return;
            }
            if (!this._core.IsRaider(e.Sender))
            {
                bot.SendReply(e, "You're not in the raid");
                return;
            }
            this._core.RemoveRaider(e.Sender, true);
            if (this.LeftEvent != null)
                this.LeftEvent(this, new RaidEventArgs(e.Sender));
        }

        private void OnRaidKickCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: raid kick [username] [reason]");
                return;
            }
            if (!this._core.Running)
            {
                bot.SendReply(e, "There is currently no raid active");
                return;
            }
            string raider = Format.UppercaseFirst(e.Args[0]);
            if (!this._core.IsRaider(raider))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " is not in the raid");
                return;
            }
            string reason = "No reason specified";
            if (e.Words.Length > 1)
                reason = e.Words[1];
            this._core.RemoveRaider(raider, false);
            if (this.KickedEvent != null)
                this.KickedEvent(this, new RaidEventArgs(raider));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, raider) + " has been kicked from the raid");
            bot.SendPrivateMessage(raider, bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has kicked you to the raid with the following reason: " + HTML.CreateColorString(bot.ColorHeaderHex, reason));
            this._core.Log(raider, e.Sender, this.InternalName, "raiders", string.Format("{0} has been kicked from the raid (Points: {1})", raider, this._core.GetPoints(e.Sender)));
            if (bot.Configuration.GetBoolean(this.InternalName, "autoban", false))
            {
                string main = bot.Users.GetMain(raider);
                Raider banned = new Raider();
                banned.Character = raider;
                banned.Duration = bot.Configuration.GetInteger(this.InternalName, "banduration", 5);
                banned.Time = DateTime.Now;
                lock (this._bans)
                {
                    this._bans.Add(main, banned);
                }
                bot.SendPrivateMessage(raider, bot.ColorHighlight + "Your access to the raid has been automatically suspended for " + HTML.CreateColorString(bot.ColorHeaderHex, banned.Duration.ToString()) + " minutes");
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " has automatically been banned from the raid for " + HTML.CreateColorString(bot.ColorHeaderHex, banned.Duration.ToString()) + " minutes");
                this._core.Log(raider, e.Sender, this.InternalName, "bans", string.Format("{0} has automatically banned from the raid for {1} minutes", raider, banned.Duration));
            }
        }

        private void OnRaidBansCommand(BotShell bot, CommandArgs e)
        {
            if (!this._core.Running)
            {
                bot.SendReply(e, "There is currently no raid active");
                return;
            }
            lock (this._bans)
            {
                if (this._bans.Count < 1)
                {
                    bot.SendReply(e, "Nobody is banned from the raid");
                    return;
                }
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Raid Bans");
                foreach (KeyValuePair<string, Raider> kvp in this._bans)
                {
                    TimeSpan ts = (new TimeSpan(0, kvp.Value.Duration, 0)) - (DateTime.Now - kvp.Value.Time);
                    window.AppendHighlight(Math.Floor(ts.TotalMinutes).ToString("##00") + ":" + ts.Seconds.ToString("00"));
                    window.AppendNormal(" - ");
                    window.AppendHighlight(kvp.Value.Character + " ");
                    if (kvp.Value.Character != kvp.Key)
                        window.AppendNormal("(Main: " + kvp.Key + ") ");
                    window.AppendNormal("[");
                    window.AppendBotCommand("Unban", "raid unban " + kvp.Key);
                    window.AppendNormal("]");
                    window.AppendLineBreak();
                }
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, this._bans.Count.ToString()) + " Raid Bans »» ", window);
            }
        }

        private void OnRaidBanCommand(BotShell bot, CommandArgs e)
        {
            int duration = 0;
            try
            {
                if (e.Args.Length < 2)
                    throw new Exception();
                duration = Convert.ToInt32(e.Args[1]);
                if (duration < 1)
                    throw new Exception();
            }
            catch
            {
                bot.SendReply(e, "Correct Usage: raid ban [username] [minutes]");
                return;
            }
            string character = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(character) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, character));
                return;
            }
            string main = bot.Users.GetMain(character);
            Raider banned = new Raider();
            banned.Character = character;
            banned.Duration = duration;
            banned.Time = DateTime.Now;
            lock (this._bans)
            {
                if (this._bans.ContainsKey(main))
                {
                    this._bans.Remove(main);
                    bot.SendPrivateMessage(character, bot.ColorHighlight + "The duration of your suspension has been set to " + HTML.CreateColorString(bot.ColorHeaderHex, banned.Duration.ToString()) + " minutes by " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender));
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, character + "'s") + " raid ban duration has been updated to " + HTML.CreateColorString(bot.ColorHeaderHex, banned.Duration.ToString()) + " minutes");
                    this._core.Log(character, e.Sender, this.InternalName, "bans", string.Format("{0}'s raid ban duration has been updated to {1} minutes", character, banned.Duration));
                }
                else
                {
                    bot.SendPrivateMessage(character, bot.ColorHighlight + "Your access to the raid has been suspended for " + HTML.CreateColorString(bot.ColorHeaderHex, banned.Duration.ToString()) + " minutes by " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender));
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, character) + " has been banned from the raid for " + HTML.CreateColorString(bot.ColorHeaderHex, banned.Duration.ToString()) + " minutes");
                    this._core.Log(character, e.Sender, this.InternalName, "bans", string.Format("{0} has banned from the raid for {1} minutes", character, banned.Duration));
                }
                this._bans.Add(main, banned);
            }
        }

        private void OnRaidUnbanCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: raid unban [username]");
                return;
            }
            string character = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(character) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, character));
                return;
            }
            string main = bot.Users.GetMain(character);
            lock (this._bans)
            {
                if (!this._bans.ContainsKey(main))
                {
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, character) + " is not banned from this raid");
                    return;
                }
                character = this._bans[main].Character;
                this._bans.Remove(main);
                bot.SendPrivateMessage(character, this._bot.ColorHighlight + "Your access to the raid has been restored by " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender));
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, character) + " has been unbanned from the raid");
                this._core.Log(character, e.Sender, this.InternalName, "bans", string.Format("{0} has been unbanned from the raid", character));
            }
        }

        private void OnRaidListCommand(BotShell bot, CommandArgs e)
        {
            if (!this._core.Running)
            {
                bot.SendReply(e, "There is currently no raid active");
                return;
            }
            RaidCore.Raider[] raiders = this._core.GetRaiders();
            if (raiders.Length == 0)
            {
                bot.SendReply(e, "There is currently nobody on the raid");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Raiders List");
            SortedDictionary<string, Dictionary<RaidCore.Raider, WhoisResult>> sorted = new SortedDictionary<string, Dictionary<RaidCore.Raider, WhoisResult>>();
            foreach (RaidCore.Raider raider in raiders)
            {
                WhoisResult whois = XML.GetWhois(raider.Character, bot.Dimension);
                if (whois == null || !whois.Success)
                {
                    whois = new WhoisResult();
                    whois.Name = new WhoisResult_Name();
                    whois.Name.Nickname = raider.Character;
                    whois.Stats = new WhoisResult_Stats();
                    whois.Stats.Level = 0;
                    whois.Stats.Profession = "Unknown";
                    whois.Stats.DefenderLevel = 0;
                }
                if (!sorted.ContainsKey(whois.Stats.Profession))
                    sorted.Add(whois.Stats.Profession, new Dictionary<RaidCore.Raider, WhoisResult>());
                sorted[whois.Stats.Profession].Add(raider, whois);
            }
            foreach (KeyValuePair<string, Dictionary<RaidCore.Raider, WhoisResult>> kvp in sorted)
            {
                window.AppendHighlight(kvp.Key);
                window.AppendLineBreak();
                foreach (KeyValuePair<RaidCore.Raider, WhoisResult> raider in kvp.Value)
                {
                    window.AppendNormalStart();
                    window.AppendString("- " + raider.Key.Character);
                    if (raider.Key.Main != raider.Key.Character)
                        window.AppendString(" (" + raider.Key.Main + ")");
                    if (raider.Value.Stats.Profession != "Unknown")
                    {
                        window.AppendString(" (L " + raider.Value.Stats.Level + "/" + raider.Value.Stats.DefenderLevel + ")");
                    }
                    if (!raider.Key.OnRaid)
                    {
                        window.AppendString(" (");
                        window.AppendColorString(RichTextWindow.ColorRed, "Inactive");
                        window.AppendString(")");
                    }
                    window.AppendString(" [");
                    window.AppendCommand("Check", "/assist " + raider.Key.Character);
                    window.AppendString("] [");
                    window.AppendBotCommand("Kick", "raid kick " + raider.Key.Character);
                    window.AppendString("] ");
                    window.AppendColorEnd();
                    window.AppendLineBreak(true);
                }
                window.AppendLineBreak();
            }
            bot.SendReply(e, "Raiders List »» ", window);
        }

        private void OnRaidCreditsCommand(BotShell bot, CommandArgs e)
        {
            int type = -1;
            if (e.Args.Length < 1 || !Int32.TryParse(e.Args[0], out type))
            {
                bot.SendReply(e, "Correct Usage: raid credits [type]");
                return;
            }
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT name, credits FROM credits_types WHERE type = " + type;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        int credits = reader.GetInt32(1);
                        string name = reader.GetString(0);
                        this.CreditsType = type;
                        this._core.Locked = false;
                        bot.SendReply(e, "The following raid credits type has been selected: " + HTML.CreateColorString(bot.ColorHeaderHex, name));
                        bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, name) + " credits have been selected and the raid has been " + HTML.CreateColorString(RichTextWindow.ColorGreen, "unlocked"));
                    }
                    else
                    {
                        bot.SendReply(e, "Invalid raid credits type");
                    }
                    reader.Close();
                }
            }
        }

        private void OnCreditsCommand(BotShell bot, CommandArgs e)
        {
            string raider;
            bool other = false;
            if (e.Args.Length > 0 && (bot.Users.GetUser(e.Sender) > bot.Commands.GetRight(e.Command, e.Type) || bot.Users.GetUser(e.Sender) == UserLevel.SuperAdmin))
                other = true;
            if (other)
                raider = bot.Users.GetMain(e.Args[0]);
            else
                raider = bot.Users.GetMain(e.Sender);
            string credits;
            if (other)
                credits = HTML.CreateColorString(bot.ColorHeaderHex, raider) + " has ";
            else
                credits = "You have used:";
            bool found = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT t.name, t.credits, c.credits FROM credits_types t, credits c WHERE t.type = c.type AND c.main = '" + raider + "'";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        credits += " " + HTML.CreateColorString(bot.ColorHeaderHex, reader.GetInt32(2) + "/" + reader.GetInt32(1)) + " " + reader.GetString(0) + " credits,";
                        found = true;
                    }
                    reader.Close();
                }
            }
            if (!found)
            {
                bot.SendReply(e, "Unable to find raid credits information for " + HTML.CreateColorString(bot.ColorHeaderHex, raider));
                return;
            }
            bot.SendReply(e, credits.Trim(','));
        }

        private void OnCreditsResetCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length > 0 && e.Args[0].ToLower() == "confirm")
            {
                this._database.ExecuteNonQuery("DELETE FROM credits");
                bot.SendReply(e, "The raid credits have been reset");
                this._core.Log(e.Sender, e.Sender, this.InternalName, "credits", string.Format("{0} has reset the raid credits", e.Sender));
                return;
            }
            bot.SendReply(e, "This command will reset the raid credits of ALL raiders. If you wish to continue use: " + HTML.CreateColorString(bot.ColorHeaderHex, "credits reset confirm"));
        }

        private void OnCreditsAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 3)
            {
                bot.SendReply(e, "Correct Usage: credits add [username] [type] [amount]");
                return;
            }
            string raider = Format.UppercaseFirst(bot.Users.GetMain(e.Args[0]));
            if (bot.GetUserID(raider) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, raider));
                return;
            }
            int type = 0;
            try { type = Convert.ToInt32(e.Args[1]); }
            catch { }
            int amount = 0;
            try { amount = Convert.ToInt32(e.Args[2]); }
            catch { }
            if (amount < 1)
            {
                bot.SendReply(e, "You need to add at least 1 raid credit");
                return;
            }
            int max = this.GetCreditsMax(type);
            if (max < 1)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, type.ToString()) + " is not a valid raid credit type");
                return;
            }
            int credits = this.GetCredits(raider, type);
            if (credits >= max)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " has already been charged the maximum amount of raid credits");
                return;
            }
            if (credits + amount > max)
                amount = max - credits;
            credits += amount;
            this._database.ExecuteNonQuery("INSERT INTO credits SET credits = " + credits + ", main = '" + Config.EscapeString(raider) + "', type = " + type + ", lastRaidID = 0 ON DUPLICATE KEY UPDATE credits = " + credits);
            bot.SendReply(e, "You have added " + HTML.CreateColorString(bot.ColorHeaderHex, amount.ToString()) + " raid credit(s). " + HTML.CreateColorString(bot.ColorHeaderHex, raider) + " now has " + HTML.CreateColorString(bot.ColorHeaderHex, credits.ToString()) + " raid credit(s)");
            this._core.Log(raider, e.Sender, this.InternalName, "credits", string.Format("{1} raid credits have been added to {0}'s account. (Total Credits: {2})", raider, amount, credits));
        }

        private void OnCreditsRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 3)
            {
                bot.SendReply(e, "Correct Usage: credits remove [username] [type] [amount]");
                return;
            }
            string raider = Format.UppercaseFirst(bot.Users.GetMain(e.Args[0]));
            if (bot.GetUserID(raider) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, raider));
                return;
            }
            int type = 0;
            try { type = Convert.ToInt32(e.Args[1]); }
            catch { }
            int amount = 0;
            try { amount = Convert.ToInt32(e.Args[2]); }
            catch { }
            if (amount < 1)
            {
                bot.SendReply(e, "You need to remove at least 1 raid credit");
                return;
            }
            int max = this.GetCreditsMax(type);
            if (max < 1)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, type.ToString()) + " is not a valid raid credit type");
                return;
            }
            int credits = this.GetCredits(raider, type);
            if (credits <= 0)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " already has 0 raid credits");
                return;
            }
            if (credits - amount < 0)
                amount = credits;
            credits -= amount;
            this._database.ExecuteNonQuery("INSERT INTO credits SET credits = " + credits + ", main = '" + Config.EscapeString(raider) + "', type = " + type + ", lastRaidID = 0 ON DUPLICATE KEY UPDATE credits = " + credits);
            bot.SendReply(e, "You have removed " + HTML.CreateColorString(bot.ColorHeaderHex, amount.ToString()) + " raid credit(s). " + HTML.CreateColorString(bot.ColorHeaderHex, raider) + " now has " + HTML.CreateColorString(bot.ColorHeaderHex, credits.ToString()) + " raid credit(s)");
            this._core.Log(raider, e.Sender, this.InternalName, "credits", string.Format("{1} raid credits have been removed from {0}'s account. (Total Credits: {2})", raider, amount, credits));
        }

        #region Credits API
        public bool WithdrawCredit(BotShell bot, string raider) { int credits; int max; bool charged; return this.WithdrawCredit(bot, raider, out credits, out max, out charged); }
        public bool WithdrawCredit(BotShell bot, string raider, out int credits, out int typeMax, out bool charged)
        {
            credits = 0;
            charged = false;
            typeMax = 0;
            raider = bot.Users.GetMain(raider);
            if (!this._core.Running)
                return false;
            if (!this.CreditsEnabled)
                return true;
            if (this.CreditsType < 0)
                return true;

            int totalCredits = 0;
            int max = this.CreditsMax;
            int raidID = -1;
            bool exists = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT credits FROM credits_types WHERE type = " + this.CreditsType;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            typeMax = reader.GetInt32(0);
                    }
                    reader.Close();
                }
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT sum(credits) as credits FROM credits WHERE main = '" + raider + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            totalCredits = reader.GetInt32(0);
                    }
                    reader.Close();
                }
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT credits, lastRaidID FROM credits WHERE main = '" + raider + "' AND type = " + this.CreditsType;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        exists = true;
                        if (!reader.IsDBNull(0))
                            credits = reader.GetInt32(0);
                        if (!reader.IsDBNull(1))
                            raidID = reader.GetInt32(1);
                    }
                    reader.Close();
                }
            }
            if (raidID == this._core.RaidID)
                return true;
            if (exists && credits >= typeMax)
                return false;
            if (!exists && this.CreditsMax < 1)
                return false;
            if (exists && totalCredits >= this.CreditsMax)
                return false;

            credits++;
            charged = true;
            if (exists)
                this._database.ExecuteNonQuery("UPDATE credits SET credits = " + credits.ToString() + ", lastRaidID = " + this._core.RaidID + " WHERE main = '" + raider + "' AND type = " + this.CreditsType);
            else
                this._database.ExecuteNonQuery("INSERT INTO credits SET credits = " + credits.ToString() + ", lastRaidID = " + this._core.RaidID + ", main = '" + raider + "', type = " + this.CreditsType);
            return true;
        }

        public int GetCredits(string raider, int type)
        {
            int result = 0;
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT credits FROM credits WHERE main = '" + Config.EscapeString(raider) + "' AND type = " + type;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
            }
            return result;
        }

        public int GetCreditsType(string name)
        {
            int result = -1;
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT type FROM credits_types WHERE name = '" + Config.EscapeString(name) + "'";
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
            }
            return result;
        }

        public int GetCreditsMax(int type)
        {
            int result = -1;
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT credits FROM credits_types WHERE type = " + type;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
            }
            return result;
        }
        #endregion

        private class Raider
        {
            public string Character;
            public DateTime Time;
            public int Duration;
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "raid join":
                    return "Allows you to join an ongoing raid.\nThis command can only be used if the raid is not locked.\nIf the credits system is enabled, joining a raid will charge you 1 credit, but you won't be charged another credit should you leave and rejoin the same raid.\n" +
                        "Usage: /tell " + bot.Character + " raid join";
                case "raid leave":
                    return "Allows you to leave the current raid.\nYou can use this command even if the raid is locked.\nIf you leave and later rejoin the same raid, you won't be charged another credit for participation.\n" +
                        "Usage: /tell " + bot.Character + " raid leave";
                case "raid add":
                    return "Allows raid leaders to manually add [username] to the current raid, even if the raid is locked.\n" +
                        "Usage: /tell " + bot.Character + " raid add [username]";
                case "raid kick":
                    return "Allows raid leaders to remove [username] from the current raid. If configured, [username] won't be able to rejoin the raid for 5 minutes.\n" +
                        "Usage: /tell " + bot.Character + " raid kick [username]";
                case "raid bans":
                    return "Allows raid leaders to review all the current raid banned players, and the remaining time on their ban.\n" +
                        "Usage: /tell " + bot.Character + " raid bans";
                case "raid ban":
                    return "Bans [username] from the raid and prevents him/her from rejoining the current raid.\nAfter [minutes] minutes are up, the banned player can join the raid again.\nAll raid bans are cleared when the raid is stopped.\n" +
                        "Usage: /tell " + bot.Character + " raid ban [username] [minutes]";
                case "raid unban":
                    return "Allows a raidleader to unban [username]. The unbanned player can immediately join the current raid again.\n" +
                        "Usage: /tell " + bot.Character + " raid unban [username]";
                case "raid credits":
                    return "Allows you to set which type of credits to use for the current raid.\n" +
                        "Usage: /tell " + bot.Character + " raid credits [type]";
                case "raid list":
                    return "Displays a list of all players on the raid\n" +
                        "Usage: /tell " + bot.Character + " raid list";
                case "credits":
                    return "Allows you to see how many credits you have left for the current cycle of raids.\nUsing 'credits' will show your amount of credits, while using 'credits [username]' will show you how many credits [username] has left.\nYou need to have at least one credit to be able to join a raid, and each raid you join will cost you one credit.\n" +
                        "Usage: /tell " + bot.Character + " credits [[username]]";
                case "credits reset":
                    return "Allows raid leaders to reset all the raidbot members' amount of credits to the base amount, in preparation for the next cycle of raids.\n" +
                        "Usage: /tell " + bot.Character + " credits reset";
                case "credits add":
                    return "Allows raid leaders to manually add [amount] credits to [username]'s raid account.\n" +
                        "Usage: /tell " + bot.Character + " credits add [username] [type] [amount]";
                case "credits remove":
                    return "Allows raid leaders to manually remove [amount] credits to [username]'s raid account.\nIf [username]'s credits go down to zero, he/she can no longer join a new raid, but he/she's not kicked from the current raid if there is one.\n" +
                        "Usage: /tell " + bot.Character + " credits remove [username] [type] [amount]";
            }
            return null;
        }
    }
}
