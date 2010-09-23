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
    public class VhanetMembersClient : PluginBase
    {
        private BotShell _bot;
        private VhanetDatabase _database;

        public VhanetMembersClient()
        {
            this.Name = "Vhanet :: Central Members Client";
            this.InternalName = "VhanetMembersClient";
            this.Version = 100;
            this.Author = "Vhab";
            this.Dependencies = new string[] { "VhanetDatabase" };
            this.DefaultState = PluginState.Disabled;
            this.Description = "Provides access to the central members system";
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
            try { this._database = (VhanetDatabase)bot.Plugins.GetPlugin("VhanetDatabase"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Database' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            bot.Configuration.Register(ConfigType.String, this.InternalName, "group", "Members Group", this._bot.Character);
            string group = this._bot.Configuration.GetString(this.InternalName, "group", this._bot.Character);
            this._bot.UsersOverride(new VhanetMembers(this._bot, group, this._database));
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
                if (e.Key == "group")
                    this._bot.UsersOverride(new VhanetMembers(this._bot, (string)e.Value, this._database));
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            bot.UsersRestore();
        }
    }

    public class VhanetMembers : IUsers
    {
        private BotShell _bot;
        private string _group;
        private VhanetDatabase _database;
        private IDbConnection Connection
        {
            get
            {
                if (this._database.Connected)
                    return this._database.Connection;
                else
                    return null;
            }
        }

        public VhanetMembers(BotShell bot, string group, VhanetDatabase database)
        {
            this._bot = bot;
            this._group = group;
            this._database = database;
        }

        public override bool AddAlt(string mainname, string altname) { return false; }
        public override bool AddUser(string username, UserLevel userlevel) { return false; }
        public override bool AddUser(string username, UserLevel userlevel, string addedBy) { return false; }
        public override bool AddUser(string username, UserLevel userlevel, string addedBy, long addedOn) { return false; }
        public override void RemoveAll() { return; }
        public override void RemoveAlt(string username) { return; }
        public override void RemoveUser(string username) { return; }
        public override bool SetUser(string username, UserLevel userlevel) { return false; }

        public override SortedDictionary<string, UserLevel> GetUsers()
        {
            SortedDictionary<string, UserLevel> users = new SortedDictionary<string, UserLevel>();
            //users.Add(this._bot.Admin, UserLevel.SuperAdmin);
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT members.username, members_levels.userLevel FROM members, members_levels, members_access WHERE members.username = members_levels.username AND members_levels.`group` = '" + this._group + "' AND members_access.username = members.username AND members_access.`group` = '" + this._group + "'";
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            if (!users.ContainsKey(reader.GetString(0)))
                                users.Add(reader.GetString(0), (UserLevel)reader.GetInt32(1));
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return users;
        }

        public override SortedDictionary<string, string> GetAllAlts()
        {
            SortedDictionary<string, string> alts = new SortedDictionary<string, string>();
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT alts.altname, alts.username FROM alts, members_access WHERE alts.username = members_access.username AND members_access.`group` = '" + this._group + "'";
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            alts.Add(reader.GetString(0), reader.GetString(1));
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return alts;
        }

        public override UserLevel GetUser(string username)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            /*if (username == this._bot.Admin)
                return UserLevel.SuperAdmin;*/
            string group = Config.EscapeString(Format.UppercaseFirst(this._group));
            UserLevel result = UserLevel.Guest;
            bool enabled = this.IsEnabled(username);
            if (!enabled)
                return result;
            string main = this.GetMain(username);
            /*if (main == this._bot.Admin && enabled)
                return UserLevel.SuperAdmin;*/
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        if (username != main)
                            command.CommandText = "SELECT members_levels.userLevel FROM members_levels, alts WHERE alts.username = members_levels.username AND alts.altname = '" + username + "' AND members_levels.`group` = '" + group + "'";
                        else
                            command.CommandText = "SELECT userLevel FROM members_levels WHERE username = '" + username + "' AND `group` = '" + group + "'";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            try { result = (UserLevel)reader.GetInt32(0); }
                            catch { }
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return result;
        }

        public override string GetMain(string altname)
        {
            altname = Format.UppercaseFirst(Config.EscapeString(altname));
            /*if (altname == this._bot.Admin)
                return altname;*/
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT username FROM alts WHERE altname = '" + altname + "'";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            altname = Format.UppercaseFirst(reader.GetString(0));
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return altname;
        }

        public override string[] GetAlts(string username)
        {
            username = Format.UppercaseFirst(Config.EscapeString(username));
            List<string> alts = new List<string>();
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT altname FROM alts WHERE username = '" + username + "'";
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            alts.Add(Format.UppercaseFirst(reader.GetString(0)));
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return alts.ToArray();
        }

        public override bool IsAlt(string username)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            /*if (username == this._bot.Admin)
                return false;*/
            if (!this.IsEnabled(username))
                return false;
            bool result = false;
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM alts WHERE altname = '" + username + "'";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            result = true;
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return result;
        }

        public override User GetUserInformation(string username)
        {
            if (!this.IsEnabled(username))
                return null;
            User result = null;
            username = Format.UppercaseFirst(Config.EscapeString(username));
            /*if (username == this._bot.Admin)
            {
                return new User(username, this._bot.GetUserID(username), UserLevel.SuperAdmin, "Core", 0, this.GetAlts(username));
            }*/
            string[] alts = this.GetAlts(username);
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT members.username, members.userID, members.addedOn, members.addedBy, members_levels.userLevel FROM members, members_levels WHERE members.username = '" + username + "' AND members.username = members_levels.username AND members_levels.`group` = '" + this._group + "'";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            result = new User(reader.GetString(0), (UInt32)reader.GetInt64(1), (UserLevel)reader.GetInt32(4), reader.GetString(3), reader.GetInt64(2), alts);
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return result;
        }

        public override bool UserExists(string username)
        {
            User user = this.GetUserInformation(username);
            return (user != null);
        }

        public override bool UserExists(string username, uint userid)
        {
            User user = this.GetUserInformation(username);
            return (user != null && user.UserID == this._bot.GetUserID(username));
        }

        public override bool Authorized(string username, UserLevel userlevel)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            string group = Config.EscapeString(Format.UppercaseFirst(this._group));
            UserLevel result = UserLevel.Guest;
            if (username == this._bot.Admin)
                return true;
            UInt32 userID = this._bot.GetUserID(username);

            if (this.IsEnabled(username))
            {
                try
                {
                    lock (this.Connection)
                    {
                        using (IDbCommand command = this.Connection.CreateCommand())
                        {
                            if (this.IsAlt(username))
                                command.CommandText = "SELECT members_levels.userLevel FROM members_levels, alts WHERE alts.username = members_levels.username AND alts.altname = '" + username + "' AND members_levels.`group` = '" + group + "' AND alts.altID = " + userID;
                            else
                                command.CommandText = "SELECT members_levels.userLevel FROM members_levels, members WHERE members_levels.username = '" + username + "' AND members_levels.`group` = '" + group + "' AND members_levels.username = members.username AND members.userID = " + userID;
                            IDataReader reader = command.ExecuteReader();
                            if (reader.Read())
                            {
                                try { result = (UserLevel)reader.GetInt32(0); }
                                catch { }
                            }
                            reader.Close();
                        }
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return (result >= userlevel);
        }

        public bool IsEnabled(string username)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            string group = Config.EscapeString(Format.UppercaseFirst(this._group));
            /*if (username == this._bot.Admin)
                return true;*/
            bool result = false;
            lock (this.Connection)
            {
                try
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM members_access WHERE username = '" + username + "' AND `group` = '" + group + "'";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            result = true;
                        }
                        reader.Close();
                    }
                }
                catch { this._database.RebuildConnection(false); }
            }
            return result;
        }
    }
}
