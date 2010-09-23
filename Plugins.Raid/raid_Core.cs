using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using AoLib.Utils;
using VhaBot;
using VhaBot.Communication;

namespace VhaBot.Plugins
{
    public class RaidCore : PluginBase
    {
        private RaidDatabase _database;

        private bool _running = false;
        private bool _paused = false;
        private bool _locked = false;
        private int _id = 0;
        private string _description = string.Empty;
        private bool _descriptionAnnounce = false;
        private int _descriptionAnnounceCounter = 0;

        private int _timeRunning = 0;
        private int _timePaused = 0;

        private BotShell _bot;
        private System.Timers.Timer _timer;

        public bool Running { get { return this._running; } }
        public string RaidDescription { get { return this._description; } }
        public bool RaidDescriptionAnnounce { get { return this._descriptionAnnounce; } }

        public TimeSpan TimeRunning { get { return new TimeSpan(0, 0, this._timeRunning); } }
        public TimeSpan TimePaused { get { return new TimeSpan(0, 0, this._timePaused); } }
        public int RaidID { get { return this._id; } }
        public readonly double MinimumPoints = -1000;

        public event EventHandler StartedEvent;
        public event EventHandler StoppedEvent;
        public event EventHandler RestoredEvent;
        public event EventHandler PausedEvent;
        public event EventHandler UnpausedEvent;
        public event EventHandler LockedEvent;
        public event EventHandler UnlockedEvent;

        public bool Paused
        {
            get { return this._paused; }
            set
            {
                if (this._paused == value) return;
                this._paused = value;
                if (this._paused)
                {
                    if (this.PausedEvent != null)
                        try { this.PausedEvent(this, new EventArgs()); }
                        catch { }
                }
                else
                {
                    if (this.UnpausedEvent != null)
                        try { this.UnpausedEvent(this, new EventArgs()); }
                        catch { }
                }
            }
        }
        public bool Locked
        {
            get { return this._locked; }
            set
            {
                if (this._locked == value) return;
                this._locked = value;
                if (this._locked)
                {
                    if (this.LockedEvent != null)
                        try { this.LockedEvent(this, new EventArgs()); }
                        catch { }
                }
                else
                {
                    if (this.UnlockedEvent != null)
                        try { this.UnlockedEvent(this, new EventArgs()); }
                        catch { }
                }
            }
        }

        public RaidCore()
        {
            this.Name = "Raid :: Core";
            this.InternalName = "RaidCore";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase" };
            this.Description = "The core of the raid system";
            this.Commands = new Command[] {
                new Command("raid", true, UserLevel.Member, UserLevel.Leader, UserLevel.Leader),
                new Command("raid start", true, UserLevel.Leader),
                new Command("raid description", true, UserLevel.Leader),
                new Command("raid description announce", false, UserLevel.Leader),
                new Command("raid stop", true, UserLevel.Leader),
                new Command("raid pause", true, UserLevel.Leader),
                new Command("raid unpause", true, UserLevel.Leader),
                new Command("raid lock", true, UserLevel.Leader),
                new Command("raid unlock", true, UserLevel.Leader),
                new Command("raid restart", true, UserLevel.Leader),
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Events.BotStateChangedEvent += new BotStateChangedHandler(Events_BotStateChangedEvent);
            this._bot = bot;

            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");

            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS raids (id INT NOT NULL AUTO_INCREMENT, startTime INT NOT NULL, startAdmin VARCHAR(20), stopTime INT NOT NULL, stopAdmin VARCHAR(20), activity INT NOT NULL, description TEXT, PRIMARY KEY (id))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS raiders (raidID INT NOT NULL, main VARCHAR(20) NOT NULL, `character` VARCHAR(20) NOT NULL, joinTime INT NOT NULL, activity INT NOT NULL, onRaid INT NOT NULL, PRIMARY KEY (raidID, main))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS points (main VARCHAR(20) NOT NULL, points INT NOT NULL, activity INT NOT NULL, PRIMARY KEY (main))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS logs (id INT NOT NULL AUTO_INCREMENT, raidID INT NULL, time INT NOT NULL, main VARCHAR(20) NULL, `character` VARCHAR(20) NULL, admin VARCHAR(20) NULL, plugin VARCHAR(255), type VARCHAR(50), message TEXT, PRIMARY KEY (id))");

            this._timer = new System.Timers.Timer();
            this._timer.Interval = 1000*10;
            this._timer.Elapsed += new System.Timers.ElapsedEventHandler(Events_Timer);
            this._timer.Start();

            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "autopause", "Automatically Pause the Raid When Starting", true);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "autolock", "Automatically Lock the Raid When Starting", false);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "announcedelay", "The delay between the raid description announcements in minutes", 5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 30);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.BotStateChangedEvent -= new BotStateChangedHandler(Events_BotStateChangedEvent);
        }

        private void Events_BotStateChangedEvent(BotShell sender, BotStateChangedArgs e)
        {
            // Close raid when the bot loses connection
            if (e.State != BotState.Connected && !e.IsSlave)
            {
                if (this._running != true)
                    return;
                this._running = false;
                this._paused = false;
                this._locked = false;
                this._timeRunning = 0;
                this._timePaused = 0;
                this._database.ExecuteNonQuery("UPDATE raiders SET onRaid = 0");
                if (this.StoppedEvent != null)
                    this.StoppedEvent(this, new EventArgs());
            }
            // Restore an unfinished raid after losing connection or a crash
            if (e.State == BotState.Connected && !e.IsSlave)
            {
                this._database.ExecuteNonQuery("UPDATE raiders SET onRaid = 0");
                lock (this._database.Connection)
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT id, stopTime, activity, description FROM raids ORDER BY startTime DESC";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            if (reader.GetInt32(1) == 0)
                            {
                                this._id = reader.GetInt32(0);
                                this._running = true;
                                this._paused = true;
                                this._locked = true;
                                this._timeRunning = reader.GetInt32(2);
                                this._timePaused = 0;
                                this._description = reader.GetString(3);
                                this._descriptionAnnounce = false;
                                this._descriptionAnnounceCounter = 0;
                            }
                        }
                        reader.Close();
                        if (this.Running)
                        {
                            this.Log(null, null, this.InternalName, "raid", "The raid has been automatically restored (Total Time: " + this.TimeRunning + ")");
                            if (this.RestoredEvent != null)
                                this.RestoredEvent(this, new EventArgs());
                        }
                    }
                }
            }
        }


        private void Events_Timer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!this.Running)
                return;

            if (this._description != null && this._description != string.Empty && this._descriptionAnnounce)
            {
                if (this._descriptionAnnounceCounter <= 0)
                {
                    this._descriptionAnnounceCounter = this._bot.Configuration.GetInteger(this.InternalName, "announcedelay", 5) * 60;
                }
                this._descriptionAnnounceCounter -= 10;
                if (this._descriptionAnnounceCounter <= 0)
                {
                    this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + "Raid Description: " + HTML.CreateColorString(this._bot.ColorHeaderHex, this._description));
                }
            }

            this._timeRunning += 10;
            if (this.Paused)
            {
                this._timePaused += 10;
                return;
            }
            this._database.ExecuteNonQuery("UPDATE raids SET activity = activity + 10 WHERE id = " + this.RaidID);
            this._database.ExecuteNonQuery("UPDATE raiders SET activity = activity + 10 WHERE raidID = " + this.RaidID + " AND onRaid = 1");
            this._database.ExecuteNonQuery("UPDATE points, raiders SET points.activity = points.activity + 10 WHERE raiders.raidID = " + this.RaidID + " AND raiders.onRaid = 1 AND raiders.main = points.main");

        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (!this._database.CheckDatabase(bot, e))
                return;

            switch (e.Command)
            {
                case "raid":
                    this.OnRaidCommand(bot, e);
                    break;
                case "raid start":
                    this.OnRaidStartCommand(bot, e);
                    break;
                case "raid stop":
                    this.OnRaidStopCommand(bot, e);
                    break;
                case "raid lock":
                    this.OnRaidLockCommand(bot, e);
                    break;
                case "raid unlock":
                    this.OnRaidUnlockCommand(bot, e);
                    break;
                case "raid pause":
                    this.OnRaidPauseCommand(bot, e);
                    break;
                case "raid unpause":
                    this.OnRaidUnpauseCommand(bot, e);
                    break;
                case "raid description":
                    this.OnRaidDescriptionCommand(bot, e);
                    break;
                case "raid description announce":
                    this.OnRaidDescriptionAnnounceCommand(bot, e);
                    break;
                case "raid restart":
                    this.OnRaidRestartCommand(bot, e);
                    break;
            }
        }

        private void OnRaidCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Raid Control Interface");
            // Status
            window.AppendHighlight("Raid Status: ");
            if (this.Paused && this.Running)
            {
                window.AppendColorString(RichTextWindow.ColorOrange, "Paused");
                window.AppendNormal(" [");
                window.AppendBotCommand("Unpause", "raid unpause");
                window.AppendNormal("]");
            }
            else if (this.Running)
            {
                window.AppendColorString(RichTextWindow.ColorGreen, "Running");
                window.AppendNormal(" [");
                window.AppendBotCommand("Pause", "raid pause");
                window.AppendNormal("]");
            }
            else
            {
                window.AppendColorString(RichTextWindow.ColorRed, "Stopped");
                window.AppendNormal(" [");
                window.AppendBotCommand("Start", "raid start");
                window.AppendNormal("]");
            }
            window.AppendLineBreak();
            if (this.Running)
            {
                // More Raid Details
                window.AppendHighlight("Raid Running: ");
                window.AppendNormal(Math.Floor(this.TimeRunning.TotalMinutes) + " minutes (" + Math.Floor(this.TimePaused.TotalMinutes) + " minutes paused)");
                window.AppendLineBreak();

                if (bot.Commands.Exists("raid join") && bot.Commands.Exists("raid leave"))
                {
                    window.AppendHighlight("Status: ");
                    if (!bot.PrivateChannel.IsOn(e.SenderID))
                    {
                        window.AppendNormal("You are not on the raid channel");
                    }
                    else if (this.IsRaider(e.Sender))
                    {
                        window.AppendNormal("You are on this raid");
                        window.AppendNormal(" [");
                        window.AppendBotCommand("Leave", "raid leave");
                        window.AppendNormal("]");
                        Raider raider = this.GetRaider(e.Sender);
                        if (raider != null)
                        {
                            window.AppendLineBreak();
                            window.AppendHighlight("Contribution: ");
                            window.AppendNormal(Math.Floor((float)raider.Activity / 60) + " minutes (" + ((float)((float)raider.Activity / ((float)this.TimeRunning.TotalSeconds - (float)this.TimePaused.TotalSeconds) * 100)).ToString("##0.0") + "%)");
                        }
                    }
                    else
                    {
                        window.AppendNormal("You are not on this raid");
                        window.AppendNormal(" [");
                        window.AppendBotCommand("Join", "raid join");
                        window.AppendNormal("]");
                    }
                    window.AppendLineBreak();
                }

                RaidersCount count = this.GetRaidersCount();
                window.AppendHighlight("Raiders: ");
                    window.AppendNormal(count.Total + " (" + count.Active + " active)");
                window.AppendLineBreak();
                window.AppendHighlight("Average Contribution: ");
                window.AppendNormal(count.AverageActivity.ToString("##0.0") + "%");
                window.AppendLineBreak();

                window.AppendHighlight("State: ");
                if (this.Locked)
                {
                    window.AppendColorString(RichTextWindow.ColorRed, "Locked");
                    window.AppendNormal(" [");
                    window.AppendBotCommand("Unlock", "raid unlock");
                    window.AppendNormal("]");
                }
                else
                {
                    window.AppendColorString(RichTextWindow.ColorGreen, "Unlocked");
                    window.AppendNormal(" [");
                    window.AppendBotCommand("Lock", "raid lock");
                    window.AppendNormal("]");
                }
                window.AppendLineBreak();
                if (this.RaidDescription != null && this.RaidDescription != string.Empty)
                {
                    window.AppendHighlight("Description: ");
                    window.AppendNormal(this.RaidDescription);
                    if (!this.RaidDescriptionAnnounce)
                    {
                        window.AppendNormal(" [");
                        window.AppendBotCommand("Enable Auto-Announce", "raid description announce");
                        window.AppendNormal("]");
                    }
                    else
                    {
                        window.AppendNormal(" [");
                        window.AppendBotCommand("Disable Auto-Announce", "raid description announce");
                        window.AppendNormal("]");
                    }
                    if (bot.Commands.Exists("mass"))
                    {
                        window.AppendNormal(" [");
                        window.AppendBotCommand("Mass", "mass " + this.RaidDescription);
                        window.AppendNormal("]");
                    }
                    if (bot.Commands.Exists("announce"))
                    {
                        window.AppendNormal(" [");
                        window.AppendBotCommand("Announce", "announce " + this.RaidDescription);
                        window.AppendNormal("]");
                    }
                    window.AppendLineBreak();
                }
            }
            bot.SendReply(e, "Raid Interface »» ", window);
        }

        private void OnRaidStartCommand(BotShell bot, CommandArgs e)
        {
            if (this._running)
            {
                bot.SendReply(e, "There is already a raid active");
                return;
            }
            if (!this.CheckChannel(bot, e))
                return;

            this._running = true;
            this._paused = false;
            this._locked = false;
            this._timeRunning = 0;
            this._timePaused = 0;
            this._description = string.Empty;
            this._descriptionAnnounce = false;

            bool raidLock = bot.Configuration.GetBoolean(this.InternalName, "autolock", false);
            bool raidPause = bot.Configuration.GetBoolean(this.InternalName, "autopause", true);

            if (e.Args.Length > 0)
            {
                foreach (string arg in e.Args)
                {
                    switch (arg.ToLower())
                    {
                        case "unpaused":
                            raidPause = false;
                            break;
                        case "paused":
                            raidPause = true;
                            break;
                        case "locked":
                            raidLock = true;
                            break;
                        case "unlocked":
                            raidLock = false;
                            break;
                    }
                }
            }
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = string.Format("INSERT INTO raids (startTime, startAdmin, stopTime, stopAdmin, activity, description) VALUES ({0}, '{1}', 0, '', 0, '')", TimeStamp.Now, Format.UppercaseFirst(e.Sender));
                    command.ExecuteNonQuery();
                    command.CommandText = "SELECT LAST_INSERT_ID() as lastID FROM raids";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        this._id = reader.GetInt32(0);
                        reader.Close();
                        this.Log(e.Sender, e.Sender, this.InternalName, "raid", "The raid has been started");
                    }
                    else
                    {
                        reader.Close();
                        bot.SendReply(e, "Unable to get RaidID! Please check the database for errors");
                        this._running = false;
                        this.Log(e.Sender, e.Sender, this.InternalName, "raid", "Unable to start the raid");
                        return;
                    }
                }
            }
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Sender)) + " has " + HTML.CreateColorString(RichTextWindow.ColorGreen, "started") + " the raid");
            if (this.StartedEvent != null)
                this.StartedEvent(this, new EventArgs());
            if (raidPause)
                this.OnRaidPauseCommand(bot, e);
            if (raidLock)
                this.OnRaidLockCommand(bot, e);
            this.OnRaidCommand(bot, e);
        }

        private void OnRaidStopCommand(BotShell bot, CommandArgs e) {
            if (!this.CheckRaid(bot, e) || !this.CheckChannel(bot, e))
                return;

            this._database.ExecuteNonQuery(String.Format("UPDATE raids SET stopTime = {0}, stopAdmin = '{1}' WHERE id = {2}", TimeStamp.Now, Format.UppercaseFirst(e.Sender), this._id));

            foreach (Raider raider in this.GetActiveRaiders())
                this.Log(raider.Character, e.Sender, this.InternalName, "raiders", string.Format("{0} has finished the raid (Points: {1})", raider.Character, this.GetPoints(raider.Main)));
            this.Log(e.Sender, e.Sender, this.InternalName, "raid", string.Format("The raid has been stopped (Total Time: {0} / Paused Time: {1})", this.TimeRunning, this.TimePaused));

            this._database.ExecuteNonQuery("UPDATE raiders SET onRaid = 0");
            this._running = false;
            this._paused = false;
            this._locked = false;
            this._timeRunning = 0;
            this._timePaused = 0;
            this._id = 0;

            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Sender)) + " has " + HTML.CreateColorString(RichTextWindow.ColorRed, "stopped") + " the raid");
            if (this.StoppedEvent != null)
                this.StoppedEvent(this, new EventArgs());
        }

        private void OnRaidPauseCommand(BotShell bot, CommandArgs e)
        {
            if (!this.CheckRaid(bot, e) || !this.CheckChannel(bot, e))
                return;
            if (this._paused)
            {
                bot.SendReply(e, "The raid is already paused");
                return;
            }
            this._paused = true;
            this.Log(e.Sender, e.Sender, this.InternalName, "raid", string.Format("The raid has been paused (Total Time: {0} / Paused Time: {1})", this.TimeRunning, this.TimePaused));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Sender)) + " has " + HTML.CreateColorString(RichTextWindow.ColorOrange, "paused") + " the raid");
            if (this.PausedEvent != null)
                this.PausedEvent(this, new EventArgs());
        }

        private void OnRaidUnpauseCommand(BotShell bot, CommandArgs e)
        {
            if (!this.CheckRaid(bot, e) || !this.CheckChannel(bot, e))
                return;
            if (!this._paused)
            {
                bot.SendReply(e, "The raid is already unpaused");
                return;
            }
            this._paused = false;
            this.Log(e.Sender, e.Sender, this.InternalName, "raid", string.Format("The raid has been unpaused (Total Time: {0} / Paused Time: {1})", this.TimeRunning, this.TimePaused));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Sender)) + " has " + HTML.CreateColorString(RichTextWindow.ColorGreen, "unpaused") + " the raid");
            if (this.UnpausedEvent != null)
                this.UnpausedEvent(this, new EventArgs());
        }

        private void OnRaidLockCommand(BotShell bot, CommandArgs e) {
            if (!this.CheckRaid(bot, e) || !this.CheckChannel(bot, e))
                return;
            if (this.Locked)
            {
                bot.SendReply(e, "The raid is already locked");
                return;
            }
            this._locked = true;
            this.Log(e.Sender, e.Sender, this.InternalName, "raid", string.Format("The raid has been locked (Total Time: {0} / Paused Time: {1})", this.TimeRunning, this.TimePaused));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Sender)) + " has " + HTML.CreateColorString(RichTextWindow.ColorRed, "locked") + " the raid");
            if (this.LockedEvent != null)
                this.LockedEvent(this, new EventArgs());
        }

        private void OnRaidUnlockCommand(BotShell bot, CommandArgs e) {
            if (!this.CheckRaid(bot, e) || !this.CheckChannel(bot, e))
                return;
            if (!this.Locked)
            {
                bot.SendReply(e, "The raid is already unlocked");
                return;
            }
            this._locked = false;
            this.Log(e.Sender, e.Sender, this.InternalName, "raid", string.Format("The raid has been unlocked (Total Time: {0} / Paused Time: {1})", this.TimeRunning, this.TimePaused));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Sender)) + " has " + HTML.CreateColorString(RichTextWindow.ColorGreen, "unlocked") + " the raid");
            if (this.UnlockedEvent != null)
                this.UnlockedEvent(this, new EventArgs());
        }

        private void OnRaidDescriptionCommand(BotShell bot, CommandArgs e)
        {
            if (!this.CheckRaid(bot, e) || !this.CheckChannel(bot, e))
                return;
            if (e.Words.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: raid description [description]");
                return;
            }
            string description = e.Words[0];
            this._description = description;
            this._descriptionAnnounceCounter = 0;
            this._database.ExecuteNonQuery("UPDATE raids SET description = '" + Config.EscapeString(description) + "' WHERE id = " + this.RaidID);
            this.Log(e.Sender, e.Sender, this.InternalName, "raid", string.Format("The raid description has been set to: {0}", description));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + "The raid description has been set to '" + HTML.CreateColorString(bot.ColorHeaderHex, description) + "' by " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender));
            bot.SendReply(e, "You updated the raid description");
        }

        private void OnRaidDescriptionAnnounceCommand(BotShell bot, CommandArgs e)
        {
            if (!this.CheckRaid(bot, e) || !this.CheckChannel(bot, e))
                return;
            if (this.RaidDescriptionAnnounce)
            {
                this._descriptionAnnounce = false;
                this._descriptionAnnounceCounter = 0;
                bot.SendReply(e, "You " + HTML.CreateColorString(RichTextWindow.ColorRed, "disabled") + " the raid description announcements");
                this.Log(e.Sender, e.Sender, this.InternalName, "raid", "The raid description announcements have been disabled");
            }
            else
            {
                this._descriptionAnnounce = true;
                this._descriptionAnnounceCounter = 0;
                bot.SendReply(e, "You " + HTML.CreateColorString(RichTextWindow.ColorGreen, "enabled") + " the raid description announcements");
                this.Log(e.Sender, e.Sender, this.InternalName, "raid", "The raid description announcements have been enabled");
            }
        }

        public bool CheckRaid(BotShell bot, CommandArgs e)
        {
            if (this.Running)
                return true;
            bot.SendReply(e, "There is currently no raid active");
            return false;
        }

        public bool CheckChannel(BotShell bot, CommandArgs e)
        {
            if (bot.PrivateChannel.IsOn(e.SenderID))
                return true;
            bot.SendReply(e, "You need to be on the private channel to use this command");
            return false;
        }

        #region Commands to manage who is on the raid
        public bool AddRaider(string username, bool verbose)
        {
            if (!this.Running)
                return false;
            username = Format.UppercaseFirst(username);

            if (this._bot.GetUserID(username) < 100)
                return false;

            string main = this._bot.Users.GetMain(username);
            List<string> characters = new List<string>(this._bot.Users.GetAlts(main));
            characters.Add(main);
            foreach (string character in characters)
                if (this.IsRaider(character))
                    return false;

            this._database.ExecuteNonQuery(String.Format("INSERT INTO raiders VALUES ({0}, '{1}', '{2}', {3}, 0, 1) ON DUPLICATE KEY UPDATE `character` = '{2}', onRaid = 1", this.RaidID, main, username, TimeStamp.Now));
            this._database.ExecuteNonQuery("INSERT IGNORE INTO points VALUES ('" + main + "', 0, 0)");

            string extra = string.Empty;
            if (username != main)
                extra = " (Main: " + HTML.CreateColorString(this._bot.ColorHeaderHex, Format.UppercaseFirst(main)) + ")";
            if (verbose)
            {
                this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + HTML.CreateColorString(this._bot.ColorHeaderHex, Format.UppercaseFirst(username)) + extra + " has joined the raid");
                this.Log(username, null, this.InternalName, "raiders", string.Format("{0} has joined the raid (Points: {1})", username, this.GetPoints(username)));
            }
            return true;
        }

        public bool RemoveRaider(string username, bool verbose)
        {
            if (!this.Running)
                return false;
            if (!this.IsRaider(username))
                return false;

            username = Format.UppercaseFirst(username);
            this._database.ExecuteNonQuery("UPDATE raiders SET onRaid = 0 WHERE `character` = '" + username + "' AND raidID = " + this.RaidID);
            if (verbose)
            {
                this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + HTML.CreateColorString(this._bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " has left the raid");
                this.Log(username, null, this.InternalName, "raiders", string.Format("{0} has left the raid (Points: {1})", username, this.GetPoints(username)));
            }
            return true;
        }

        public bool IsRaider(string username)
        {
            if (!this.Running)
                return false;
            username = Format.UppercaseFirst(username);
            bool exists = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM raiders WHERE `character` = '" + username + "' AND raidID = " + this.RaidID + " AND onRaid = 1";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        exists = true;
                    }
                    reader.Close();
                }
            }
            return exists;
        }

        public Raider[] GetRaiders()
        {
            if (!this.Running)
                return new Raider[0];
            return this.GetRaiders(this.RaidID);
        }
        public Raider[] GetRaiders(int raidID)
        {
            List<Raider> raiders = new List<Raider>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT main, `character`, joinTime, activity, onRaid FROM raiders WHERE raidID = " + raidID + " ORDER BY `character`";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        raiders.Add(new Raider(raidID, reader.GetString(1), reader.GetString(0), reader.GetInt32(2), reader.GetInt32(3), (reader.GetInt32(4) == 1)));
                    }
                    reader.Close();
                }
            }
            return raiders.ToArray();
        }

        public Raider[] GetActiveRaiders()
        {
            if (!this.Running)
                return new Raider[0];
            List<Raider> raiders = new List<Raider>();
            foreach (Raider raider in this.GetRaiders(this.RaidID))
            {
                if (raider.OnRaid)
                    raiders.Add(raider);
            }
            return raiders.ToArray();
        }

        public RaidersCount GetRaidersCount()
        {
            Raider[] raiders = this.GetRaiders();
            int total = 0;
            int active = 0;
            Int64 activity = 0;
            foreach (Raider raider in raiders)
            {
                total++;
                if (raider.OnRaid)
                    active++;
                activity += raider.Activity;
            }
            float average = 0;
            if (activity > 0)
                average = (float)activity/(float)((this._timeRunning-this._timePaused)*total)*100;
            return new RaidersCount(total, active, activity, average);
        }

        public Raider GetRaider(string username)
        {
            username = Format.UppercaseFirst(username);
            Raider raider = null;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT main, `character`, joinTime, activity, onRaid FROM raiders WHERE `character` = '" + username + "' AND raidID = " + this.RaidID;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        raider = new Raider(this.RaidID, reader.GetString(1), reader.GetString(0), reader.GetInt64(2), reader.GetInt32(3), (reader.GetInt32(4) == 1));
                    }
                    reader.Close();
                }
            }
            return raider;
        }

        public Raider GetRaiderByMain(string username)
        {
            username = this._bot.Users.GetMain(username);
            Raider raider = null;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT `character` FROM raiders WHERE main = '" + username + "' AND raidID = " + this.RaidID;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        string alt = reader.GetString(0);
                        reader.Close();
                        raider = this.GetRaider(alt);
                    }
                    else
                        reader.Close();
                }
            }
            return raider;
        }

        public class Raid
        {
            public readonly int RaidID;
            public readonly Int64 StartTime;
            public readonly string StartAdmin;
            public readonly Int64 StopTime;
            public readonly string StopAdmin;
            public readonly int Activity;
            public readonly string Description;
            public readonly int Raiders;

            public Raid(int raidID, Int64 startTime, string startAdmin, Int64 stopTime, string stopAdmin, int activity, string description, int raiders)
            {
                this.RaidID = raidID;
                this.StartTime = startTime;
                this.StartAdmin = startAdmin;
                this.StopTime = stopTime;
                this.StopAdmin = stopAdmin;
                this.Activity = activity;
                this.Description = description;
                this.Raiders = raiders;
            }
        }

        public Raid[] GetRaids()
        {
            List<Raid> raids = new List<Raid>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT t1.id, t1.startTime, t1.startAdmin, t1.stopTime, t1.stopAdmin, t1.activity, t1.description, count(t2.raidID) FROM raids t1, raiders t2 WHERE t1.id = t2.raidID GROUP BY t2.raidID ORDER BY startTime";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        raids.Add(new Raid(reader.GetInt32(0), reader.GetInt64(1), reader.GetString(2), reader.GetInt64(3), reader.GetString(4), reader.GetInt32(5), reader.GetString(6), reader.GetInt32(7)));
                    }
                    reader.Close();
                }
            }
            return raids.ToArray();
        }

        public class Raider
        {
            public readonly int RaidID;
            public readonly string Character;
            public readonly string Main;
            public readonly Int64 JoinTime;
            public readonly int Activity;
            public readonly bool OnRaid;

            public Raider(int raidID, string character, string main, Int64 joinTime, int activity, bool onRaid)
            {
                this.RaidID = raidID;
                this.Character = character;
                this.Main = main;
                this.JoinTime = joinTime;
                this.Activity = activity;
                this.OnRaid = onRaid;
            }
        }

        public class RaidersCount
        {
            public readonly int Total;
            public readonly int Active;
            public readonly Int64 TotalActivity;
            public readonly float AverageActivity;

            public RaidersCount(int total, int active, Int64 totalActivity, float averageActivity)
            {
                this.Total = total;
                this.Active = active;
                this.TotalActivity = totalActivity;
                this.AverageActivity = averageActivity;
            }
        }
        #endregion

        #region Commands to manage points
        public double GetPoints(string username)
        {
            username = this._bot.Users.GetMain(username);
            int points = (int)(this.MinimumPoints * 10) - 1;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT points FROM points WHERE main = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        points = reader.GetInt32(0);
                    }
                    reader.Close();
                }
            }
            return ((double)points)/10;
        }

        public Dictionary<string,double> GetAllPoints()
        {
            Dictionary<string, double> points = new Dictionary<string, double>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT main, points FROM points ORDER BY points DESC";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        points.Add(reader.GetString(0), ((double)reader.GetInt32(1))/10);
                    }
                    reader.Close();
                }
            }
            return points;
        }

        public void AddPoints(string username, double points) { this.AddPoints(username, points, false); }
        public void AddPoints(string username, double points, bool activity)
        {
            username = this._bot.Users.GetMain(username);
            if (points < 0)
                return;
            string query;
            if (activity)
                query = String.Format("INSERT INTO points VALUES ('{0}', {1}, {1}) ON DUPLICATE KEY UPDATE `points` = `points`+{1}, `activity` = `activity`+{1}", username, (int)(points * 10));
            else
                query = String.Format("INSERT INTO points VALUES ('{0}', {1}, 0) ON DUPLICATE KEY UPDATE `points` = `points`+{1}", username, (int)(points * 10));
            this._database.ExecuteNonQuery(query);
        }

        public void RemovePoints(string username, double points)
        {
            username = this._bot.Users.GetMain(username);
            if (points < 0)
                return;
            points = this.GetPoints(username) - points;
            if (points < this.MinimumPoints)
                points = this.MinimumPoints;
            this._database.ExecuteNonQuery(String.Format("UPDATE points SET points = {1} WHERE main = '{0}'", username, (int)(points * 10)));
        }

        public int GetActivity(string username)
        {
            username = this._bot.Users.GetMain(username);
            int activity = -1;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT activity FROM points WHERE main = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        activity = reader.GetInt32(0);
                    }
                    reader.Close();
                }
            }
            return activity;
        }

        public void AddActivity(string username, int activity)
        {
            username = this._bot.Users.GetMain(username);
            if (activity < 0)
                return;
            this._database.ExecuteNonQuery(String.Format("INSERT INTO points VALUES ('{0}', 0, {1}) ON DUPLICATE KEY UPDATE `activity` = `activity`+{1}", username, activity));
        }
        #endregion

        #region Logging
        public bool Log(string character, string admin, string plugin, string type, string message)
        {
            string ircmessage = "";
            string query = "INSERT INTO logs (`raidID`, `time`, `character`, `main`, `admin`, `plugin`, `type`, `message`) VALUES (";
            // Raid ID
            if (this.Running)
            {
                query += this.RaidID + ", ";
                ircmessage = "Raid ID: " + this.RaidID;
            }
            else
                query += "NULL, ";

            // Time
            query += TimeStamp.Now + ", ";

            // Character
            if (character != null && character != string.Empty)
            {
                query += "'" + Config.EscapeString(Format.UppercaseFirst(character)) + "', ";
                ircmessage += ", Character: " + Format.UppercaseFirst(character);
            }
            else
                query += "NULL, ";

            // Main
            if (character != null && character != string.Empty)
            {
                query += "'" + Config.EscapeString(this._bot.Users.GetMain(character)) + "', ";
                ircmessage += " / " + Format.UppercaseFirst(this._bot.Users.GetMain(character));
            }
            else
                query += "NULL, ";

            // Admin
            if (admin != null && admin != string.Empty)
            {
                query += "'" + Config.EscapeString(this._bot.Users.GetMain(admin)) + "', ";
                ircmessage += ", Admin: " + Config.EscapeString(this._bot.Users.GetMain(admin));
            } else
                query += "NULL, ";

            // Plugin
            query += "'" + Config.EscapeString(plugin) + "', ";

            // Type
            query += "'" + Config.EscapeString(type) + "', ";

            // Message
            query += "'" + Config.EscapeString(message) + "')";
            ircmessage += " - " + message;

            this._bot.SendPluginMessageAndWait(this.InternalName, "irccore", "IrcLog", 100, ircmessage);
            return (this._database.ExecuteNonQuery(query) > 0);
        }

        public LogEntry[] GetLogs(int max)
        {
            string query = "ORDER BY `time` DESC";
            if (max > 0) query += " LIMIT " + max;
            return this.GetLogs(query);
        }
        public LogEntry[] GetLogs(int max, int raidID)
        {
            string query = "WHERE raidID = " + raidID + " ORDER BY `time` ASC";
            if (max > 0) query += " LIMIT " + max;
            return this.GetLogs(query);
        }
        public LogEntry[] GetLogs(int max, string main)
        {
            string query = "WHERE main = '" + Config.EscapeString(main) + "' OR admin = '" + Config.EscapeString(main) + "' ORDER BY `time` ASC";
            if (max > 0) query += " LIMIT " + max;
            return this.GetLogs(query);
        }
        private LogEntry[] GetLogs(string query)
        {
            List<LogEntry> results = new List<LogEntry>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT `id`, `time`, `raidID`, `main`, `character`, `admin`, `type`, `message` FROM `logs` " + query;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            results.Add(new LogEntry(reader.GetInt32(0), reader.GetInt64(1), reader.GetInt32(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7)));
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return results.ToArray();
        }

        public class LogEntry
        {
            public readonly int ID;
            public readonly DateTime Time;
            public readonly int RaidID;
            public readonly string Main;
            public readonly string Character;
            public readonly string Admin;
            public readonly string Type;
            public readonly string Message;

            public LogEntry(int id, Int64 time, int raidID, string main, string character, string admin, string type, string message)
            {
                this.ID = id;
                this.Time = TimeStamp.ToDateTime(time);
                this.RaidID = raidID;
                this.Main = main;
                this.Character = character;
                this.Admin = admin;
                this.Type = type;
                this.Message = message;
            }
        }
        #endregion

        public void OnRaidRestartCommand(BotShell bot, CommandArgs e)
        {
            if (this._running)
            {
                bot.SendReply(e, "There is already a raid active");
                return;
            }
            if (!this.CheckChannel(bot, e))
                return;
            int raidID = -1;
            if (e.Words.Length < 1 || !int.TryParse(e.Args[0], out raidID))
            {
                bot.SendReply(e, "Correct Usage: raid restart [raid]");
                return;
            }

            //this._database.ExecuteNonQuery("UPDATE raiders SET onRaid = 0");
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT id, stopTime, activity, description FROM raids WHERE id = " + raidID;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {

                        this._id = reader.GetInt32(0);
                        this._running = true;
                        this._paused = true;
                        this._locked = true;
                        this._timeRunning = reader.GetInt32(2);
                        this._timePaused = 0;
                        this._description = reader.GetString(3);
                        this._descriptionAnnounce = false;
                        this._descriptionAnnounceCounter = 0;
                    }
                    reader.Close();
                    if (this._running == true)
                    {
                        if (this.RestoredEvent != null)
                            this.RestoredEvent(this, new EventArgs());
                        this._database.ExecuteNonQuery("UPDATE raids SET stopTime = 0, stopAdmin = '' WHERE id = " + this._id);
                        bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Sender)) + " has " + HTML.CreateColorString(RichTextWindow.ColorGreen, "restarted") + " raid #" + raidID);
                        this.Log(e.Sender, e.Sender, this.InternalName, "raid", "The raid has been restarted");
                    }
                    else
                    {
                        bot.SendReply(e, "Unable to restart raid #" + raidID);
                    }
                }
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "raid":
                    return "Displays the main raid interface.\nUsing this interface you can start/lock/unlock/pause/join/leave the raid.\nIt will also tell you alot of other useful information.\n" +
                        "Usage: /tell " + bot.Character + " raid";
                case "raid start":
                    return "Starts a new raid, and allows players who join the raid to be charged one credit for participation.\nThe raid is paused when it starts, and will last until a raidleader uses the Raid stop command.\nIf the raidbot crashes during a raid, there is no need to restart the raid: the current raid is automatically resumed when the bot gets back online.\n" +
                        "You can optionally specify how you wish to start the raid using one of the following key words after 'raid join': paused, unpaused, locked and/or unlocked.\n" +
                        "Usage: /tell " + bot.Character + " raid start [options]";
                case "raid stop":
                    return "Stops the current raid.\nYou won't be able to reward points to the raiders beyond this point.\nAunctions and raffles can still be done after a raid is stopped.\n" +
                        "Usage: /tell " + bot.Character + " raid stop";
                case "raid pause":
                    return "Pauses the current raid until a raidleader uses the raid unpause command.\nPaused time within a raid is not taken into account for a raider's 'Raid Contribution'.\n" +
                        "Usage: /tell " + bot.Character + " raid pause";
                case "raid unpause":
                    return "Unpauses the current raid.\n" +
                        "Usage: /tell " + bot.Character + " raid unpause";
                case "raid lock":
                    return "Prevents any player not already in the raid from joining the raid.\nPlayers already in the raid can still leave the raid.\n" +
                        "Usage: /tell " + bot.Character + " raid lock";
                case "raid unlock":
                    return "Unlocks the current raid\n" +
                        "Usage: /tell " + bot.Character + " raid unlock";
                case "raid description":
                    return "Allows raid leaders to set the description of the raid.\nThis description can be viewed with the 'raid' command and can be automatically spammed to the private channel on a configured interval.\n" +
                        "Usage: /tell " + bot.Character + " raid description [description]\n" +
                        "Example: /tell " + bot.Character + " raid description Raiding The Beast (Lock/Check at 20:25, Moving at 20:30)";
                case "raid restart":
                    return "Allows you to restart a previously closed raid\n" +
                        "Usage: /tell " + bot.Character + " raid restart [raid id]";
            }
            return null;
        }

        /*public override object OnDataRequest(params string[] data)
        {
            if (data.Length < 2)
                return null;
            switch (data[0].ToLower())
            {
                case "raider remove":
                    this.RemoveRaider(data[1], true);
                    break;
                case "raider add":
                    this.AddRaider(data[1], true);
                    break;
            }
            return null;
        }*/

        public override void OnPluginMessage(BotShell bot, PluginMessage message)
        {
            Console.WriteLine(message.Command);
            switch (message.Command.ToLower())
            {
                case "getactiveraiders":
                    List<string> activeRaiders = new List<string>();
                    foreach (Raider raider in this.GetActiveRaiders())
                        activeRaiders.Add(raider.Character);
                    bot.SendReplyMessage(this.InternalName, message, activeRaiders.ToArray());
                    break;
                case "getraiders":
                    List<string> raiders = new List<string>();
                    foreach (Raider raider in this.GetRaiders())
                        raiders.Add(raider.Character);
                    bot.SendReplyMessage(this.InternalName, message, raiders.ToArray());
                    break;
                case "addraider":
                    if (message.Args.Length < 1)
                        return;
                    bot.SendReplyMessage(this.InternalName, message, this.AddRaider((string)message.Args[0], true));
                    break;
                case "removeraider":
                    if (message.Args.Length < 1)
                        return;
                    bot.SendReplyMessage(this.InternalName, message, this.RemoveRaider((string)message.Args[0], true));
                    break;
            }
        }
    }
}