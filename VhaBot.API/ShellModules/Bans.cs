using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;

namespace VhaBot.ShellModules
{
    public class Bans
    {
        private BotShell Parent;
        private Config _config;
        private List<string> _whitelistString;
        private List<UInt32> _whitelistUint;
        private bool _allowedClan = true;
        private bool _allowedNeutral = true;
        private bool _allowedOmni = true;
        private int _levelRequirement = 0;

        public bool AllowedClan { get { return this._allowedClan; } set { this._allowedClan = value; } }
        public bool AllowedNeutral { get { return this._allowedNeutral; } set { this._allowedNeutral = value; } }
        public bool AllowedOmni { get { return this._allowedOmni; } set { this._allowedOmni = value; } }
        public int LevelRequirement
        {
            get { return this._levelRequirement; }
            set
            {
                if (value < 0) value = 0;
                if (value > 220) value = 221;
                this._levelRequirement = value;
            }
        }

        public Bans(BotShell parent)
        {
            this.Parent = parent;
            this._whitelistString = new List<string>();
            this._whitelistUint = new List<uint>();
            this._config = new Config(this.Parent.ToString(), "bans");
            this._config.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CORE_Bans (Username VARCHAR(14) UNIQUE, UserID INTEGER UNIQUE, AddedBy VARCHAR(14), AddedOn INTEGER)");
        }

        public bool Add(string username) { return this.Add(username, "System", TimeStamp.Now); }
        public bool Add(string username, string addedBy) { return this.Add(username, addedBy, TimeStamp.Now); }
        public bool Add(string username, string addedBy, Int64 addedOn)
        {
            username = Format.UppercaseFirst(username);
            UInt32 userid = this.Parent.GetUserID(username);
            if (userid < 1)
                return false;

            int results = 0;
            results = this._config.ExecuteNonQuery(String.Format("INSERT INTO CORE_Bans (Username, UserID, AddedBy, AddedOn) VALUES ('{0}', {1}, '{2}', {3})", Config.EscapeString(username), userid, Config.EscapeString(addedBy), addedOn));
            
            lock (this._whitelistString)
                if (this._whitelistString.Contains(username))
                    this._whitelistString.Remove(username);
            lock (this._whitelistUint)
                if (this._whitelistUint.Contains(userid))
                    this._whitelistUint.Remove(userid);

            if (results > 0)
                return true;
            else
                return false;
        }

        public bool Remove(string username)
        {
            username = Format.UppercaseFirst(username);
            if (this._config.ExecuteNonQuery(String.Format("DELETE FROM CORE_Bans WHERE Username = '{0}'", Config.EscapeString(username))) > 0)
                return true;
            return false;
        }
        public bool Remove(UInt32 userid)
        {
            if (this._config.ExecuteNonQuery(String.Format("DELETE FROM CORE_Bans WHERE UserID = {0}", userid)) > 0)
                return true;
            return false;
        }

        public void RemoveAll()
        {
            this._config.ExecuteNonQuery(String.Format("DELETE FROM CORE_Bans"));
        }

        public bool IsAutoBanned(WhoisResult whois)
        {
            if (this._levelRequirement > 0)
            {
                if (whois == null || !whois.Success)
                    return true;
                if (whois.Stats.Level < this._levelRequirement)
                    return true;
            }
            if (this.AllowedClan && this.AllowedNeutral && this.AllowedOmni)
                return false;
            if (whois == null || !whois.Success)
                return true;
            if (whois.Name.Nickname.Equals(this.Parent.Admin, StringComparison.CurrentCultureIgnoreCase))
                return false;
            if (whois.Stats.Faction.ToLower() == "clan" && this.AllowedClan)
                return false;
            else if (whois.Stats.Faction.ToLower() == "neutral" && this.AllowedNeutral)
                return false;
            else if (whois.Stats.Faction.ToLower() == "omni" && this.AllowedOmni)
                return false;
            else
                return true;
        }
         

        public bool IsBanned(string username)
        {
            if (this.Parent.Admin.Equals(username, StringComparison.CurrentCultureIgnoreCase))
                return false;
            if (this.Parent.GetUserID(username) < 1)
                return false;
            if (this.Parent.IsBot(username))
                return false;

            username = Format.UppercaseFirst(username);
            lock (this._whitelistString)
                if (this._whitelistString.Contains(username))
                    return false;

            bool result = false;
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM CORE_Bans WHERE Username = '" + Config.EscapeString(username) + "'";
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = true;
                }
                else
                {
                    lock (this._whitelistString)
                        this._whitelistString.Add(username);
                }
                reader.Close();
            }
            return result;
        }

        public bool IsBanned(UInt32 userid)
        {
            if (this.Parent.Admin.Equals(this.Parent.GetUserName(userid), StringComparison.CurrentCultureIgnoreCase))
                return false;
            if (userid < 1)
                return false;

            lock (this._whitelistUint)
                if (this._whitelistUint.Contains(userid))
                    return false;

            bool result = false;
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM CORE_Bans WHERE UserID = " + userid;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result = true;
                }
                else
                {
                    lock (this._whitelistUint)
                        this._whitelistUint.Add(userid);
                }
                reader.Close();
            }
            return result;
        }

        public Ban[] List()
        {
            List<Ban> bans = new List<Ban>();
            using (IDbCommand command = this._config.Connection.CreateCommand())
            {
                command.CommandText = "SELECT Username, UserID, AddedBy, AddedOn FROM CORE_Bans";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    bans.Add(new Ban(Format.UppercaseFirst(reader.GetString(0)), (UInt32)reader.GetInt64(1), reader.GetString(2), reader.GetInt64(3)));
                }
                reader.Close();
            }
            return bans.ToArray();
        }
    }
}
