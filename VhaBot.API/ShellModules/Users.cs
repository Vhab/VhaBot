using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;

namespace VhaBot.ShellModules
{

    public class Users : IUsers
    {
        private BotShell Parent;
        private Config _config;

        public Users(BotShell parent)
        {
            this.Parent = parent;
            this._config = new Config(this.Parent.ToString(), "users");
            this._config.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CORE_Members (Username VARCHAR(14) UNIQUE, UserID INTEGER UNIQUE, UserLevel VARCHAR(255), AddedBy VARCHAR(14), AddedOn INTEGER)");
            this._config.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CORE_Alts (Username VARCHAR(14) UNIQUE, UserID INTEGER UNIQUE, Main VARCHAR(14))");
        }

        public override void RemoveAll()
        {
            this._config.ExecuteNonQuery("DELETE FROM CORE_Members");
            this._config.ExecuteNonQuery("DELETE FROM CORE_Alts");
        }

        public override bool AddUser(string username, UserLevel userlevel) { return this.AddUser(username, userlevel, "System", TimeStamp.Now); }
        public override bool AddUser(string username, UserLevel userlevel, string addedBy) { return this.AddUser(username, userlevel, addedBy, TimeStamp.Now); }
        public override bool AddUser(string username, UserLevel userlevel, string addedBy, long addedOn)
        {
            UInt32 userid = this.Parent.GetUserID(username);
            if (userid < 1)
                return false;
            if (this.Parent.IsBot(username))
                return false;

            username = Format.UppercaseFirst(username);

            if (this.UserExists(username, userid))
                return true;

            this._config.ExecuteNonQuery("REPLACE INTO CORE_Members VALUES ('" + Config.EscapeString(username) + "', " + userid + ", '" + userlevel.ToString() + "', '" + Format.UppercaseFirst(Config.EscapeString(addedBy)) + "', " + addedOn + ")");
            return true;
        }

        public override bool AddAlt(string mainname, string altname)
        {
            altname = Format.UppercaseFirst(altname);
            mainname = Format.UppercaseFirst(mainname);
            UInt32 altid = this.Parent.GetUserID(altname);
            if (altid < 1)
                return false;
            if (this.Parent.IsBot(altname))
                return false;

            if (this.UserExists(altname))
            {
                return false;
            }
            if (this.UserExists(mainname))
            {
                if (!this.IsAlt(mainname))
                {
                    this._config.ExecuteNonQuery("REPLACE INTO CORE_Alts VALUES ('" + Config.EscapeString(altname) + "', " + altid + ", '" + Config.EscapeString(mainname) + "')");
                }
            }
            return false;
        }

        public override bool UserExists(string username) { return this.UserExists(username, 0, 0, false); }
        public override bool UserExists(string username, UInt32 userid) { return this.UserExists(username, userid, 1, false); }
        private bool UserExists(string username, UInt32 userid, Int16 mode, bool alt)
        {
            username = Format.UppercaseFirst(username);
            if (this.Parent.Admin == username && !alt)
                return true;

            bool result = false;
            string query1;
            string query2;
            switch (mode)
            {
                case 0:
                    query1 = String.Format("SELECT Username FROM CORE_Members WHERE Username = '{0}'", username);
                    query2 = String.Format("SELECT Username FROM CORE_Alts WHERE Username = '{0}'", username);
                    break;
                case 1:
                    query1 = String.Format("SELECT Username FROM CORE_Members WHERE Username = '{0}' AND UserID = {1}", username, userid);
                    query2 = String.Format("SELECT Username FROM CORE_Alts WHERE Username = '{0}' AND UserID = {1}", username, userid);
                    break;
                default:
                    return false;
            }
            if (alt == false)
            {
                using (IDbCommand command = this._config.Connection.CreateCommand())
                {
                    command.CommandText = query1;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        result = true;
                    }
                    reader.Close();
                }
            }
            else
            {
                using (IDbCommand command = this._config.Connection.CreateCommand())
                {
                    command.CommandText = query2;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        public override bool IsAlt(string username)
        {
            username = Format.UppercaseFirst(username);
            return this.UserExists(username, 0, 0, true);
        }

        public override string GetMain(string altname)
        {
            UInt32 altid = this.Parent.GetUserID(altname);
            altname = Format.UppercaseFirst(altname);

            string result = altname;
            string query = String.Format("SELECT Main FROM CORE_Alts WHERE Username = '{0}'", altname);
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetString(0);
                }
                reader.Close();
            }
            return result;
        }

        public override string[] GetAlts(string username)
        {
            List<string> alts = new List<string>();
            string query = "SELECT Username FROM CORE_Alts WHERE Main = '" + Config.EscapeString(username) + "'";
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        alts.Add(Format.UppercaseFirst(reader.GetString(0)));
                    }
                    catch { }
                }
                reader.Close();
            }
            return alts.ToArray();
        }

        public override SortedDictionary<string, string> GetAllAlts()
        {
            SortedDictionary<string, string> alts = new SortedDictionary<string, string>();
            string query = "SELECT Username, Main FROM CORE_Alts";
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        alts.Add(Format.UppercaseFirst(reader.GetString(0)), Format.UppercaseFirst(reader.GetString(1)));
                    }
                    catch { }
                }
                reader.Close();
            }
            return alts;
        }

        public override void RemoveUser(string username)
        {
            username = Format.UppercaseFirst(username);
            string query = String.Format("DELETE FROM CORE_Members WHERE Username = '{0}'", username);
            this._config.ExecuteNonQuery(query);
            query = String.Format("DELETE FROM CORE_Alts WHERE Main = '{0}'", username);
            this._config.ExecuteNonQuery(query);
        }

        public override void RemoveAlt(string username)
        {
            username = Format.UppercaseFirst(username);
            string query = String.Format("DELETE FROM CORE_Alts WHERE Username = '{0}'", username);
            this._config.ExecuteNonQuery(query);
            if (!this.UserExists(username))
            {
                this.AddUser(username, UserLevel.Member);
            }
        }

        public override UserLevel GetUser(string username)
        {
            UInt32 userid = this.Parent.GetUserID(username);
            username = Format.UppercaseFirst(username);
            if (this.UserExists(username, userid, 1, true))
            {
                username = Format.UppercaseFirst(this.GetMain(username));
                userid = this.Parent.GetUserID(username);
            }
            if (username == this.Parent.Admin)
            {
                return UserLevel.SuperAdmin;
            } 
            if (!this.UserExists(username, userid))
            {
                return UserLevel.Guest;
            }
            string query = "SELECT UserLevel FROM CORE_Members WHERE Username = '" + username + "' AND UserID = " + userid;
            UserLevel result = UserLevel.Guest;
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    try
                    {
                        result = (UserLevel)Enum.Parse(typeof(UserLevel), reader.GetString(0));
                    }
                    catch { }
                }
                reader.Close();
            }
            return result;
        }

        public override User GetUserInformation(string username)
        {
            username = this.GetMain(username);
            if (username == this.Parent.Admin)
            {
                return new User(username, this.Parent.GetUserID(username), UserLevel.SuperAdmin, "Core", 0, this.GetAlts(username));
            }
            if (!this.UserExists(username))
            {
                return null;
            }
            string query = "SELECT UserLevel, UserID, AddedBy, AddedOn FROM CORE_Members WHERE Username = '" + username + "'";
            User result = null;
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    try
                    {
                        result = new User(
                            username,
                            (UInt32)reader.GetInt64(1),
                            (UserLevel)Enum.Parse(typeof(UserLevel), reader.GetString(0)),
                            reader.GetString(2),
                            reader.GetInt64(3),
                            this.GetAlts(username)
                        );
                    }
                    catch { }
                }
                reader.Close();
            }
            return result;
        }

        public override SortedDictionary<string, UserLevel> GetUsers()
        {
            SortedDictionary<string, UserLevel> users = new SortedDictionary<string, UserLevel>();
            string query = "SELECT Username, UserLevel FROM CORE_Members";
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        UserLevel userlevel = UserLevel.Guest;
                        try { userlevel = (UserLevel)Enum.Parse(typeof(UserLevel), reader.GetString(1)); }
                        catch { }
                        users.Add(reader.GetString(0), userlevel);
                    }
                    catch { }
                }
                reader.Close();
            }
            users.Add(Format.UppercaseFirst(this.Parent.Admin), UserLevel.SuperAdmin);
            return users;
        }

        public override bool SetUser(string username, UserLevel userlevel)
        {
            if (this.UserExists(username))
            {
                this._config.ExecuteNonQuery(string.Format("UPDATE CORE_Members SET UserLevel = '{1}' WHERE Username = '{0}'", username, userlevel));
                if (this.GetUser(username) == userlevel)
                    return true;
            }
            return false;
        }

        public override bool Authorized(string username, UserLevel userlevel)
        {
            UserLevel level = this.GetUser(username);
            if ((int)userlevel <= (int)level)
                return true;
            else
                return false;
        }
    }
}
