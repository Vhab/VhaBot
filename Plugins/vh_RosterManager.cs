using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class RosterManager : PluginBase
    {
        private enum RosterState
        {
            Idle,
            FetchingMembers,
            CrossCheckingMembers,
            AddingMembers,
            RemovingMembers
        }

        private int _rosterInterval = 8;
        private DateTime _lastUpdated =  DateTime.Now;
        private bool _rosterEnabled = false;
        private RosterState _state = RosterState.Idle;
        private int _progressValue = 0;
        private int _progressMax = 1;
        private Config _database;
        private BotShell _bot;

        public RosterManager()
        {
            this.Name = "Roster Manager";
            this.InternalName = "vhRosterManager";
            this.Author = "Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("roster", true, UserLevel.Admin),
                new Command("roster add", true, UserLevel.Admin),
                new Command("roster remove", false, UserLevel.Admin),
                new Command("roster update", true, UserLevel.Leader),
                new Command("roster reset", false, UserLevel.Admin)
            };
        }

        public override void OnLoad(BotShell bot) {
            this._bot = bot;
            this._database = new Config(bot.ID, this.InternalName);
            this.LoadSettings(bot);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS organizations (ID INTEGER PRIMARY KEY, GuildName VARCHAR(255), LastUpdated INTEGER)");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS members (Username VARCHAR(255) PRIMARY KEY, LastSeen INTEGER)");
            bot.Timers.Hour += new EventHandler(UpdateTimer_Elapsed);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "interval", "The interval in hours between each roster update", this._rosterInterval, 8, 16, 24, 48);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "enabled", "Enables/disables the automated roster update", this._rosterEnabled);
        }

        public override void OnUnload(BotShell bot) { }

        public override void OnUninstall(BotShell bot)
        {
            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("DROP TABLE organizations");
            this._database.ExecuteNonQuery("DROP TABLE members");
        }

        private void LoadSettings(BotShell bot)
        {
            this._rosterInterval = bot.Configuration.GetInteger(this.InternalName, "interval", this._rosterInterval);
            this._rosterEnabled = bot.Configuration.GetBoolean(this.InternalName, "enabled", this._rosterEnabled);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "roster":
                    this.OnRosterCommand(bot, e);
                    break;
                case "roster add":
                    this.OnRosterAddCommand(bot, e);
                    break;
                case "roster remove":
                    this.OnRosterRemoveCommand(bot, e);
                    break;
                case "roster update":
                    this.OnRosterUpdateCommand(bot, e);
                    break;
                case "roster reset":
                    this.OnRosterResetCommand(bot, e);
                    break;
            }
        }

        private void OnRosterCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length > 0)
            {
                switch (e.Args[0])
                {
                    case "enable":
                        bot.Configuration.SetBoolean(this.InternalName, "enabled", true);
                        this.LoadSettings(bot);
                        bot.SendReply(e, "Automated roster updating enabled");
                        break;
                    case "disable":
                        bot.Configuration.SetBoolean(this.InternalName, "enabled", false);
                        this.LoadSettings(bot);
                        bot.SendReply(e, "Automated roster updating disabled");
                        break;
                    case "interval":
                        if (e.Args.Length > 1)
                        {
                            try
                            {
                                int interval = Convert.ToInt32(e.Args[1]);
                                bot.Configuration.SetInteger(this.InternalName, "interval", interval);
                                this.LoadSettings(bot);
                                bot.SendReply(e, "Automated roster updating interval changed");
                            }
                            catch
                            {
                                bot.SendReply(e, "Correct Usage: roster interval [interval]");
                            }
                        }
                        else
                        {
                            bot.SendReply(e, "Correct Usage: roster interval [interval]");
                            return;
                        }
                        break;
                    default:
                        break;
                }
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Automated Roster Updater");
            window.AppendHighlight("Enabled: ");
            if (this._rosterEnabled)
            {
                window.AppendNormal("Yes [");
                window.AppendBotCommand("Disable", "roster disable");
                window.AppendNormal("]");
            }
            else
            {
                window.AppendNormal("No [");
                window.AppendBotCommand("Enable", "roster enable");
                window.AppendNormal("]");
            }
            window.AppendLineBreak();

            window.AppendHighlight("Update Interval: ");
            window.AppendNormal(this._rosterInterval + " hours [");
            window.AppendBotCommand("8", "roster interval 8");
            window.AppendNormal("] [");
            window.AppendBotCommand("16", "roster interval 16");
            window.AppendNormal("] [");
            window.AppendBotCommand("24", "roster interval 24");
            window.AppendNormal("] [");
            window.AppendBotCommand("48", "roster interval 48");
            window.AppendNormal("]");
            window.AppendLineBreak();

            window.AppendHighlight("Organizations:");
            window.AppendLineBreak();

            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT ID,GuildName FROM organizations";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        int id = (int)reader.GetInt64(0);
                        string name = reader.GetString(1);
                        window.AppendNormal("  " + name + " [");
                        window.AppendBotCommand("Remove", "roster remove " + id);
                        window.AppendNormal("]");
                        window.AppendLineBreak();
                    }
                    catch { }
                }
                reader.Close();
            }
            window.AppendHighlight("State: ");
            window.AppendNormal(this._state.ToString());
            window.AppendLineBreak();
            if (this._state == RosterState.AddingMembers || this._state == RosterState.RemovingMembers || this._state == RosterState.CrossCheckingMembers)
            {
                window.AppendHighlight("Progress: ");
                window.AppendProgressBar(this._progressValue, this._progressMax, 50);
            }

            bot.SendReply(e, "Roster »» " + window.ToString());
        }

        private void OnRosterAddCommand(BotShell bot, CommandArgs e)
        {
            if (this._state != RosterState.Idle)
            {
                bot.SendReply(e, "Roster update is currently in progress. Please try again later");
                return;
            }

            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM organizations";
                IDataReader reader = command.ExecuteReader();
                int i = 0;
                while (reader.Read())
                    i++;

                reader.Close();
                if (i > 4)
                {
                    bot.SendReply(e, "You can't add more than 5 guilds!");
                    return;
                }
            }

            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: roster add [username]");
                return;
            }
            WhoisResult whois = XML.GetWhois(e.Args[0], bot.Dimension, false, true);
            if (whois == null || whois.Organization == null || whois.Organization.Name == null || whois.Organization.ID < 1)
            {
                bot.SendReply(e, "Unable to add an organization based on that user");
                return;
            }
            this._database.ExecuteNonQuery(String.Format("INSERT INTO organizations VALUES ({0}, '{1}', 0)", whois.Organization.ID, Config.EscapeString(whois.Organization.Name)));
            bot.SendReply(e, "Added " + HTML.CreateColorString(bot.ColorHeaderHex, whois.Organization.Name) + " to the auto-update list");
        }

        private void OnRosterRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (this._state != RosterState.Idle)
            {
                bot.SendReply(e, "Roster update is currently in progress. Please try again later");
                return;
            }

            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: roster remove [guild id]");
                return;
            }

            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT GuildName FROM organizations WHERE ID = '" + e.Args[0] + "'";
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    try
                    {
                        string name = reader.GetString(0);
                        reader.Close();
                        this._database.ExecuteNonQuery("DELETE FROM organizations WHERE ID = '" + e.Args[0] + "'");
                        bot.SendReply(e, "Removed " + HTML.CreateColorString(bot.ColorHeaderHex, name) + " from the auto-update list");
                    }
                    catch { }
                }
                else
                {
                    bot.SendReply(e, "There is no organization present in the database with that ID");
                }
                if (!reader.IsClosed)
                    reader.Close();
            }
        }

        private void OnRosterResetCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length > 0)
            {
                if (e.Args[0].ToLower() == "confirm")
                {
                    this._database.ExecuteNonQuery("DELETE FROM members");
                    bot.SendReply(e, "Roster list cleared");
                    return;
                }
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Clear Cached Roster List");
            window.AppendNormal("Warning! This will clear the local roster list!");
            window.AppendLineBreak();
            window.AppendNormal("This list is used to determine changes in the organization's member list.");
            window.AppendLineBreak();
            window.AppendNormal("Clearing this list will NOT remove any members from the bot!");
            window.AppendLineBreak();
            window.AppendNormal("After clearing this list, the next roster update will see every member of the organization as a new member.");
            window.AppendLineBreak();
            window.AppendNormal("Resetting the cached roster list can be used to resolve some sync issues but may also leave 'ghost' members behind.");
            window.AppendLineBreak();
            window.AppendNormal("Use this command with caution!");
            window.AppendLineBreak(2);
            window.AppendBotCommand("Reset List Now", "roster reset confirm");
            bot.SendReply(e, "Roster Reset »» " + window.ToString());
        }

        private void OnRosterUpdateCommand(BotShell bot, CommandArgs e)
        {
            if (this._state != RosterState.Idle)
            {
                bot.SendReply(e, "Roster update is currently in progress. Please try again later");
                return;
            }
            bot.SendReply(e, "Updating Roster... (This may take several minutes)");
            string[] removed;
            string[] added;
            RichTextWindow window;
            this._state = RosterState.FetchingMembers;
            try
            {
                if (this.UpdateRoster(out removed, out added, out window))
                {
                    string[] pages = window.ToStrings();
                    Int32 i = 0;
                    foreach (string page in pages)
                    {
                        i++;
                        string count = string.Empty;
                        if (pages.Length > 1)
                            count = " (" + i + " of " + pages.Length + ")";

                        bot.SendReply(e, "Roster Update Report »» " + page + count);
                    }
                    this._lastUpdated = DateTime.Now;
                }
                else
                {
                    bot.SendReply(e, "An error has occured while trying to update the roster. Please try again later");
                }
            }
            catch { }
            this._state = RosterState.Idle;
        }

        private void UpdateTimer_Elapsed(object sender, EventArgs e)
        {
            if (this._rosterEnabled)
            {
                if (this._rosterInterval < 1)
                    return;

                if (DateTime.Now > this._lastUpdated.AddHours(this._rosterInterval))
                {
                    if (this._state != RosterState.Idle)
                        return;

                    string[] removed;
                    string[] added;
                    RichTextWindow window;
                    this._state = RosterState.FetchingMembers;
                    try
                    {
                        if (this.UpdateRoster(out removed, out added, out window))
                        {
                            if (removed.Length > 0 || added.Length > 0)
                            {
                                string[] pages = window.ToStrings();
                                Int32 i = 0;
                                foreach (string page in pages)
                                {
                                    i++;
                                    string count = string.Empty;
                                    if (pages.Length > 1)
                                        count = " (" + i + " of " + pages.Length + ")";

                                    this._bot.SendOrganizationMessage(this._bot.ColorHighlight + "Roster Update Report »» " + page + count);
                                }
                            }
                        }
                        this._lastUpdated = DateTime.Now;
                    }
                    catch { }
                    this._state = RosterState.Idle;
                }
            }
        }

        private bool UpdateRoster(out string[] removed, out string[] added, out RichTextWindow window)
        {
            this._state = RosterState.FetchingMembers;
            removed = new string[0];
            added = new string[0];
            window = null;

            DateTime startTime = DateTime.Now;
            List<string> oldMembers = new List<string>();
            List<string> newMembers = new List<string>();
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT ID FROM organizations";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        int id = (int)reader.GetInt64(0);
                        OrganizationResult org = XML.GetOrganization(id, this._bot.Dimension, false, true);
                        if (org == null || org.Members == null || org.Members.Items == null || org.Members.Items.Length == 0)
                            throw new Exception();

                        foreach (OrganizationMember member in org.Members.Items)
                            newMembers.Add(member.Nickname);
                    }
                    catch
                    {
                        reader.Close(); return false;
                    }
                }
                reader.Close();
            }

            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT Username FROM members";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        string name = reader.GetString(0);
                        oldMembers.Add(name);
                    }
                    catch { }
                }
                reader.Close();
            }
            this._state = RosterState.CrossCheckingMembers;
            this._progressMax = newMembers.Count + oldMembers.Count;
            this._progressValue = 0;
            List<string> addMembers = new List<string>();
            List<string> removeMembers = new List<string>();

            foreach (string member in newMembers)
            {
                if (!oldMembers.Contains(member))
                {
                    addMembers.Add(member);
                    this._bot.GetMainBot().SendNameLookup(member);
                }
                this._progressValue++;
            }
            foreach (string member in oldMembers)
            {
                if (!newMembers.Contains(member))
                    removeMembers.Add(member);
                this._progressValue++;
            }   

            this._state = RosterState.RemovingMembers;
            this._progressMax = removeMembers.Count;
            this._progressValue = 0;
            foreach (string member in removeMembers)
            {
                this._bot.Users.RemoveUser(member);
                this._bot.Users.RemoveAlt(member);
                this._bot.FriendList.Remove("notify", member);
                this._database.ExecuteNonQuery("DELETE FROM members WHERE Username = '" + member + "'");
                this._progressValue++;
            }

            this._state = RosterState.AddingMembers;
            this._progressMax = addMembers.Count;
            this._progressValue = 0;

            List<string> failed = new List<string>();
            foreach (string member in addMembers)
            {
                if (this._bot.Users.AddUser(member, UserLevel.Member))
                {
                    this._bot.FriendList.Add("notify", member);
                    this._database.ExecuteNonQuery("INSERT INTO members VALUES ('" + member + "', 0)");
                }
                else
                    failed.Add(member);
                this._progressValue++;
            }
            foreach (string member in failed)
                addMembers.Remove(member);

            added = addMembers.ToArray();
            removed = removeMembers.ToArray();
            TimeSpan elapsed = DateTime.Now - startTime;
            window = new RichTextWindow(this._bot);
            window.AppendTitle("Roster Update Report");
            window.AppendHighlight("Processing Time: ");
            window.AppendNormal(String.Format("{0:00}:{1:00}:{2:00}", Math.Floor(elapsed.TotalHours), elapsed.Minutes, elapsed.Seconds));
            window.AppendLineBreak();
            window.AppendHighlight("Members Added: ");
            window.AppendNormal(added.Length.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Members Removed: ");
            window.AppendNormal(removed.Length.ToString());
            window.AppendLineBreak();
            if (added.Length > 0)
            {
                window.AppendLineBreak();
                window.AppendHeader("Added Members");
                foreach (string member in added)
                {
                    window.AppendHighlight(member);
                    window.AppendLineBreak();
                }
            }
            if (removed.Length > 0)
            {
                window.AppendLineBreak();
                window.AppendHeader("Removed Members");
                foreach (string member in removed)
                {
                    window.AppendHighlight(member);
                    window.AppendLineBreak();
                }
            }
            this._state = RosterState.Idle;
            return true;
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "roster":
                    return "Displays the roster manager.\nFrom this display you can change the settings and remove organizations.\n" +
                        "Usage: /tell " + bot.Character + " roster";
                case "roster add":
                    return "Allows you to add an organization to the roster manager.\nIn order to add an organization you need to supply the name of a member of the organization.\n" +
                        "Usage: /tell " + bot.Character + " roster add [username]";
                case "roster update":
                    return "Initiates a forced update of the roster.\n" +
                        "Usage: /tell " + bot.Character + " roster update";
            }
            return null;
        }
    }
}
