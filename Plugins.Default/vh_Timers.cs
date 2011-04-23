using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Timers;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class Timers : PluginBase
    {
        private Config _database;
        private Dictionary<int, CachedTimer> _timers;
        private Timer _timer;
        private BotShell _bot;

        public Timers()
        {
            this.Name = "Timers";
            this.InternalName = "vhTimers";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Description = "Sets and tracks various timers";
            this.Commands = new Command[] {
                new Command("timers", true, UserLevel.Member),
                new Command("timers add", true, UserLevel.Member),
                new Command("timer", "timers add"),
                new Command("timers remove", true, UserLevel.Member)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS timers (id INTEGER PRIMARY KEY AUTOINCREMENT, source VARCHAR(14), owner VARCHAR(14), start INTEGER, end INTEGER, duration INTEGER, description TEXT)");
            this._database.ExecuteNonQuery("DELETE FROM timers WHERE end < " + TimeStamp.Now);
            this._timers = new Dictionary<int, CachedTimer>();
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT id, source, owner, start, end, description FROM timers ORDER BY id";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string source = reader.GetString(1);
                    string owner = reader.GetString(2);
                    DateTime start = TimeStamp.ToDateTime(reader.GetInt64(3));
                    DateTime end = TimeStamp.ToDateTime(reader.GetInt64(4));
                    string description = reader.GetString(5);
                    this._timers.Add(id, new CachedTimer(id, source, owner, start, end, (end - start), description));
                }
            }
            this._timer = new Timer(1000);
            this._timer.AutoReset = true;
            this._timer.Elapsed += new ElapsedEventHandler(this.OnTimerTick);
            this._timer.Start();
        }

        public override void OnUnload(BotShell bot)
        {
            this._timer.Stop();
            this._timer.Dispose();
            this._database.Close();
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            List<int> remove = new List<int>();
            lock (this._timers)
            {
                foreach (CachedTimer timer in this._timers.Values)
                {
                    if (timer.End > DateTime.UtcNow) continue;
                    string message = this._bot.ColorHighlight + "Timer " + HTML.CreateColorString(this._bot.ColorHeaderHex, "#" + timer.ID) + " owned by " + HTML.CreateColorString(this._bot.ColorHeaderHex, timer.Owner) + " has expired: " + HTML.CreateColorString(this._bot.ColorHeaderHex, timer.Description);
                    if (timer.Source == "pg")
                        this._bot.SendPrivateChannelMessage(message);
                    else if (timer.Source == "gc")
                        this._bot.SendOrganizationMessage(message);
                    else
                        this._bot.SendPrivateMessage(timer.Source, message);
                    remove.Add(timer.ID);
                }
                foreach (int id in remove)
                    this._timers.Remove(id);
            }
            foreach (int id in remove)
            {
                lock (this._database) this._database.ExecuteNonQuery("DELETE FROM timers WHERE id = " + id);
            }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "timers":
                    this.OnTimersCommand(bot, e);
                    break;
                case "timers add":
                    this.OnTimersAddCommand(bot, e);
                    break;
                case "timers remove":
                    this.OnTimersRemoveCommand(bot, e);
                    break;
            }
        }

        public void OnTimersCommand(BotShell bot, CommandArgs e)
        {
            lock (this._timers)
            {
                // If the user's level is higher than the required level he can see all timers
                bool all = false;
                if (bot.Users.GetUser(bot.Users.GetMain(e.Sender)) > bot.Commands.GetRight("timers", e.Type)) all = true;

                // Format output
                int count = 0;
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Active Timers");
                foreach (CachedTimer timer in this._timers.Values)
                {
                    if (!all)
                    {
                        if (timer.Owner != e.Sender && timer.Source != "pg" && timer.Source != "gc")
                            continue;
                    }
                    count++;
                    TimeSpan remaining = timer.End - DateTime.UtcNow;
                    window.AppendHighlight(Format.Time(remaining, FormatStyle.Medium));
                    window.AppendNormal(" #" + timer.ID + " - " + timer.Description + " (" + timer.Owner + ")");
                    window.AppendLineBreak();
                }
                if (count > 0)
                    bot.SendReply(e, "Timers »» ", window);
                else
                    bot.SendReply(e, "There are currently no timers active");
            }
        }

        public void OnTimersAddCommand(BotShell bot, CommandArgs e)
        {
            // Check arguments
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: timers add [duration] [description]");
                return;
            }

            // Parse duration
            DateTime end = DateTime.UtcNow;
            DateTime start = DateTime.UtcNow;
            string[] parts = e.Args[0].Split(':');
            int depth = 0;
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                uint part;
                if (!uint.TryParse(parts[i], out part))
                {
                    bot.SendReply(e, "Invalid duration syntax given. The correct syntax is [[[[days:]hours:]minutes:]seconds]");
                    return;
                }
                depth++;
                switch (depth)
                {
                    case 1:
                        end = end.AddSeconds(part);
                        break;
                    case 2:
                        end = end.AddMinutes(part);
                        break;
                    case 3:
                        end = end.AddHours(part);
                        break;
                    case 4:
                        end = end.AddDays(part);
                        break;
                }
            }
            TimeSpan duration = end - start;

            // Save timer
            long endStamp = TimeStamp.FromDateTime(end);
            long startStamp = TimeStamp.FromDateTime(start);
            long durationStamp = (long)Math.Ceiling(duration.TotalSeconds);
            string description = e.Words[1];
            string source = e.Sender;
            if (e.Type == CommandType.Organization) source = "gc";
            if (e.Type == CommandType.PrivateChannel) source = "pg";
            int id = -1;
            lock (this._database)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = string.Format("INSERT INTO timers (owner, source, start, end, duration, description) VALUES ('{0}', '{1}', {2}, {3}, {4}, '{5}')", e.Sender, source, startStamp, endStamp, durationStamp, Config.EscapeString(description));
                    if (command.ExecuteNonQuery() < 1)
                    {
                        bot.SendReply(e, "An error has occurred while adding the timer to the database");
                        return;
                    }
                    command.CommandText = "SELECT LAST_INSERT_ROWID()";
                    id = (int)(Int64)command.ExecuteScalar();
                }
            }

            // Save cache
            CachedTimer timer = new CachedTimer(id, source, e.Sender, start, end, duration, description);
            lock(this._timers) this._timers.Add(id, timer);
            bot.SendReply(e, "Timer " + HTML.CreateColorString(bot.ColorHeaderHex, "#" + id) + " with a duration of " + HTML.CreateColorString(bot.ColorHeaderHex, Format.Time(duration, FormatStyle.Large)) + " has been added");
        }

        public void OnTimersRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: timers remove [id]");
                return;
            }
            int id = 0;
            lock (this._timers)
            {
                if (!int.TryParse(e.Args[0], out id) || !this._timers.ContainsKey(id))
                {
                    bot.SendReply(e, "Invalid timer id");
                    return;
                }
                this._timers.Remove(id);
            }
            lock (this._database) this._database.ExecuteNonQuery("DELETE FROM timers WHERE id = " + id);
            bot.SendReply(e, "Timer " + HTML.CreateColorString(bot.ColorHeaderHex, "#" + id) + " has been removed");
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "timers":
                    return "Displays all your timers and all public timers.\nHigher ranked members of the bot can also view other user's timers.\n" +
                        "Usage: /tell " + bot.Character + " timers";
                case "timers add":
                    return "Allows you to add a new timer.\n" +
                        "Timers added by sending a private message to the bot are marked as private timers.\n" +
                        "Timers added through organization chat or the private channel are marked as public timers. These timers will also output to the channel it was set through.\n" +
                        "Usage: /tell " + bot.Character + " timers add [duration] [description]\n" +
                        "Example: /tell " + bot.Character + " timers add 18:0:0 Beast Spawn!";
                case "timers remove":
                    return "Allows you to remove a timer.\n" +
                        "Usage: /tell " + bot.Character + " timers remove [id]";
            }
            return null;
        }

        public class CachedTimer
        {
            public readonly int ID;
            public readonly string Source;
            public readonly string Owner;
            public readonly DateTime Start;
            public readonly DateTime End;
            public readonly TimeSpan Duration;
            public readonly string Description;

            public CachedTimer(int id, string source, string owner, DateTime start, DateTime end, TimeSpan duration, string description)
            {
                this.ID = id;
                this.Source = source;
                this.Owner = owner;
                this.Start = start;
                this.End = end;
                this.Duration = duration;
                this.Description = description;
            }
        }
    }
}
