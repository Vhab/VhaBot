using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;
using VhaBot;
using VhaBot.ShellModules;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class VhanetFriendsList : PluginBase
    {
        private BotShell _bot;
        private VhanetDatabase _database;
        private VhanetMembersHost _host;
        private readonly string _friendsList = "notify";
        private bool _busy = false;

        private List<string> QueueOnline = new List<string>();
        private List<string> QueueOffline = new List<string>();

        public VhanetFriendsList()
        {
            this.Name = "Vhanet :: Automated Friends List";
            this.InternalName = "VhanetFriendsList";
            this.Version = 100;
            this.Author = "Vhab";
            this.Dependencies = new string[] { "VhanetDatabase", "VhanetMembersHost" };
            this.DefaultState = PluginState.Disabled;
            this.Description = "Automatically manages the vhanet friends list and syncs against the member list";
            this.Commands = new Command[] {
                new Command("members sync", false, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
            try { this._database = (VhanetDatabase)bot.Plugins.GetPlugin("VhanetDatabase"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Database' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            try { this._host = (VhanetMembersHost)bot.Plugins.GetPlugin("VhanetMembersHost"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Members Host' Plugin!"); }

            bot.Timers.Minute += new EventHandler(Timers_Minute);
            bot.Timers.EightHours += new EventHandler(OnTimer);
            bot.Events.UserLogonEvent += new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.UserLogoffEvent += new UserLogoffHandler(Events_UserLogoffEvent);
            this._database.ExecuteNonQuery("UPDATE members SET online = 0");
            this._database.ExecuteNonQuery("UPDATE alts SET online = 0");
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Timers.Minute -= new EventHandler(Timers_Minute);
            bot.Timers.EightHours -= new EventHandler(OnTimer);
            bot.Events.UserLogonEvent -= new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.UserLogoffEvent -= new UserLogoffHandler(Events_UserLogoffEvent);
        }

        private void Timers_Minute(object sender, EventArgs e)
        {
            try
            {
                lock (this.QueueOnline)
                {
                    List<string> nicknames = new List<string>();
                    foreach (string nickname in this.QueueOnline)
                        nicknames.Add("'" + nickname.ToLower() + "'");
                    this._database.ExecuteNonQuery("UPDATE members SET online = 1 WHERE username IN (" + string.Join(", ", nicknames.ToArray()) + ")");
                    this._database.ExecuteNonQuery("UPDATE alts SET online = 1 WHERE altname IN (" + string.Join(", ", nicknames.ToArray()) + ")");
                    this.QueueOnline.Clear();
                }
                lock (this.QueueOffline)
                {
                    List<string> nicknames = new List<string>();
                    foreach (string nickname in this.QueueOffline)
                        nicknames.Add("'" + nickname.ToLower() + "'");
                    this._database.ExecuteNonQuery("UPDATE members SET online = 0 WHERE username IN (" + string.Join(", ", nicknames.ToArray()) + ")");
                    this._database.ExecuteNonQuery("UPDATE alts SET online = 0 WHERE altname IN (" + string.Join(", ", nicknames.ToArray()) + ")");
                    this.QueueOffline.Clear();
                }
            }
            catch { }
        }

        private void Events_UserLogoffEvent(BotShell bot, UserLogoffArgs e)
        {
            lock (this.QueueOffline)
                this.QueueOffline.Add(e.Sender);
        }

        private void Events_UserLogonEvent(BotShell bot, UserLogonArgs e)
        {
            lock (this.QueueOnline)
                this.QueueOnline.Add(e.Sender);
        }


        public void OnTimer(object sender, EventArgs e)
        {
            this.Sync();
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (!this._database.CheckDatabase(bot, e))
                return;
            switch (e.Command)
            {
                case "members sync":
                    if (this._busy)
                    {
                        bot.SendReply(e, "The members list is already being synchronized");
                        return;
                    }
                    bot.SendReply(e, "Synchronizing the members list now...");
                    this.Sync();
                    bot.SendReply(e, "The members list has been synchronized");
                    break;
            }
        }

        public void Sync()
        {
            if (!this._database.Connected)
                return;
            lock (this)
            {
                if (this._busy)
                    return;
                this._busy = true;
            }
            try
            {
                List<string> members = new List<string>();
                members.Add(this._bot.Admin.ToLower());
                lock (this._database.Connection)
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT username FROM members";
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            if (!members.Contains(reader.GetString(0).ToLower()))
                                members.Add(reader.GetString(0).ToLower());
                        }
                        reader.Close();
                    }
                }
                lock (this._database.Connection)
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT altname FROM alts";
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            if (!members.Contains(reader.GetString(0).ToLower()))
                                members.Add(reader.GetString(0).ToLower());
                        }
                        reader.Close();
                    }
                }
                List<string> friends = new List<string>(this._bot.FriendList.List(this._friendsList));
                foreach (string member in members)
                    if (!friends.Contains(member))
                        this._bot.FriendList.Add(this._friendsList, member);
                foreach (string friend in friends)
                    if (!members.Contains(friend))
                        this._bot.FriendList.Remove(this._friendsList, friend);
                this._bot.FriendList.Sync();
                string[] _onlineMembers = this._bot.FriendList.Online(this._friendsList);
                List<string> onlineMembers = new List<string>();
                foreach (string member in _onlineMembers)
                    onlineMembers.Add("'" + Format.UppercaseFirst(member) + "'");
                this._database.ExecuteNonQuery("UPDATE members SET online = 0");
                this._database.ExecuteNonQuery("UPDATE members SET online = 1 WHERE username IN (" + string.Join(", ", onlineMembers.ToArray()) + ")");
                this._database.ExecuteNonQuery("UPDATE alts SET online = 0");
                this._database.ExecuteNonQuery("UPDATE alts SET online = 1 WHERE altname IN (" + string.Join(", ", onlineMembers.ToArray()) + ")");
            }
            catch { }
            lock (this)
                this._busy = false;
        }

        public string[] GetOnlineMembers(string group, bool extended)
        {
            group = Format.UppercaseFirst(group);
            string query;
            if (extended)
            {
                query = "SELECT t1.username as username FROM members t1, members_levels t2 WHERE t1.username = t2.username AND t1.online = 1 AND t2.group = '" + group + "' " +
                    "UNION " +
                    "SELECT t3.altname as username FROM members t1, members_levels t2, alts t3 WHERE t1.username = t2.username AND t3.online = 1 AND t2.group = '" + group + "' AND t1.username = t3.username " +
                    "GROUP BY username ORDER BY username";
            }
            else
            {
                query = "SELECT t1.username as username FROM members t1, members_access t2 WHERE t1.username = t2.username AND t1.online = 1 AND t2.group = '" + group + "' " +
                    "UNION " +
                    "SELECT t1.altname as username FROM alts t1, members_access t2 WHERE t1.altname = t2.username AND t1.online = 1 AND t2.group = '" + group + "' " +
                    "GROUP BY username ORDER BY username";
            }
            return this.GetList(query);
        }

        public string[] GetOnlineMembers()
        {
            string query = "SELECT t1.username as username FROM members t1 WHERE t1.online = 1 " +
                "UNION " +
                "SELECT t1.altname as username FROM alts t1 WHERE t1.online = 1 " +
                "GROUP BY username ORDER BY username";
            return this.GetList(query);
        }

        private string[] GetList(string query)
        {
            List<string> list = new List<string>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = query;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            if (!list.Contains(reader.GetString(0)))
                                list.Add(reader.GetString(0));
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return list.ToArray();
        }
    }
}
