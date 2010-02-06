using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;
using AoLib.Net;
using AoLib.Utils;

namespace VhaBot.ShellModules
{
    public class FriendList
    {
        public static readonly int FRIENDMAX = 990;

        private Dictionary<UInt32, Friend> OnlineFriends = new Dictionary<UInt32, Friend>();
        private Dictionary<UInt32, Friend> OfflineFriends = new Dictionary<UInt32, Friend>();
        private Dictionary<string, List<string>> Friends = new Dictionary<string, List<string>>();
        private Config Config;
        private bool Syncing = false;
        private BotShell Parent;

        public FriendList(BotShell parent)
        {
            this.Parent = parent;
            this.Config = new Config(this.Parent.ToString(), "notify");
            this.Config.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CORE_Notify (Section VARCHAR(255), Username VARCHAR(255), LastOnline INTEGER)");
            this.Parent.Events.BotStateChangedEvent += new BotStateChangedHandler(BotStateChangedEvent);
            this.RebuildCache();
        }

        private void BotStateChangedEvent(BotShell sender, BotStateChangedArgs e)
        {
            if (e.State != BotState.Connected)
            {
                lock (this.OnlineFriends)
                {
                    List<UInt32> remove = new List<UInt32>();
                    foreach (KeyValuePair<UInt32, Friend> kvp in this.OnlineFriends)
                        if (e.IsSlave)
                        {
                            if (kvp.Value.OnSlave && kvp.Value.BotID == e.ID)
                                remove.Add(kvp.Value.UserID);
                        }
                        else
                            if (!kvp.Value.OnSlave)
                                remove.Add(kvp.Value.UserID);
                    foreach (UInt32 userID in remove)
                    {
                        lock (this.OfflineFriends)
                        {
                            this.OfflineFriends.Add(userID, this.OnlineFriends[userID]);
                            this.OnlineFriends.Remove(userID);
                        }
                    }
                }
            }
        }

        public void OnNotifyAction(object sender, BuddyStatusEventArgs e)
        {
            Chat bot = (Chat)sender;
            string user = bot.GetUserName(e.CharacterID);
            if (this.Parent.IsBot(user))
            {
                lock (this.Config)
                    this.Config.ExecuteNonQuery("DELETE FROM CORE_Notify WHERE Username = '" + user.ToLower() + "'");
                this.RebuildCache();
            }
            List<string> sections = this.GetSections(user);
            if (sections.Count < 1)
            {
                bot.SendFriendRemove(e.CharacterID);
                lock (this.OnlineFriends)
                    if (this.OnlineFriends.ContainsKey(e.CharacterID))
                        this.OnlineFriends.Remove(e.CharacterID);
                lock (this.OfflineFriends)
                    if (this.OfflineFriends.ContainsKey(e.CharacterID))
                        this.OfflineFriends.Remove(e.CharacterID);
                return;
            }
            bool onSlave = false;
            Int32 slaveID = 0;
            if (bot.Character != this.Parent.Character)
            {
                onSlave = true;
                slaveID = this.Parent.GetSlaveID(bot.Character);
            }

            Friend friend = new Friend(bot.GetUserName(e.CharacterID), e.CharacterID, DateTime.Now, onSlave, slaveID);

            bool first = true;
            lock (this.OnlineFriends)
            {
                if (this.OnlineFriends.ContainsKey(e.CharacterID))
                {
                    first = false;
                    if (!e.Online)
                        this.OnlineFriends.Remove(e.CharacterID);
                    else
                        return;
                }
            }
            lock (this.OfflineFriends)
            {
                if (this.OfflineFriends.ContainsKey(e.CharacterID))
                {
                    first = false;
                    if (e.Online)
                        this.OfflineFriends.Remove(e.CharacterID);
                    else
                        return;
                }
            }
            if (e.First)
                first = true;

            if (e.Online)
            {
                lock (this.OnlineFriends)
                    this.OnlineFriends.Add(e.CharacterID, friend);
                UserLogonArgs args = new UserLogonArgs(this.Parent, friend.UserID, friend.User, first, sections);
                this.Parent.Events.OnUserLogon(this.Parent, args);

            }
            else
            {
                lock (this.OfflineFriends)
                    this.OfflineFriends.Add(e.CharacterID, friend);
                UserLogoffArgs args = new UserLogoffArgs(this.Parent, friend.UserID, friend.User, first, sections);
                this.Parent.Events.OnUserLogoff(this.Parent, args);

            }
            if (e.Online || (!e.Online && !first))
            {
                lock (this.Config)
                    this.Config.ExecuteNonQuery("UPDATE CORE_Notify SET LastOnline = " + TimeStamp.Now + " WHERE Username = '" + bot.GetUserName(e.CharacterID).ToLower() + "'");
            }
        }

        private void DatabaseAdd(string section, string user)
        {
            section = section.ToLower();
            user = user.ToLower();
            lock (this.Friends)
            {
                if (!this.Friends.ContainsKey(user))
                    this.Friends.Add(user, new List<string>());

                if (!this.Friends[user].Contains(section))
                    this.Friends[user].Add(section);
            }
            lock (this.Config)
                this.Config.ExecuteNonQuery("INSERT INTO CORE_Notify VALUES ('" + Config.EscapeString(section) + "', '" + user + "', 0)");
        }

        private void DatabaseRemove(string section, string user)
        {
            section = section.ToLower();
            user = user.ToLower();
            lock (this.Friends)
            {
                if (this.Friends.ContainsKey(user))
                {
                    if (this.Friends[user].Contains(section))
                        this.Friends[user].Remove(section);

                    if (this.Friends[user].Count == 0)
                        this.Friends.Remove(user);
                }
            }
            lock (this.Config)
                this.Config.ExecuteNonQuery("DELETE FROM CORE_Notify WHERE Section = '" + Config.EscapeString(section) + "' AND Username = '" + user + "'");
        }

        public List<string> GetSections(string user)
        {
            user = user.ToLower();
            lock (this.Friends)
            {
                if (!this.Friends.ContainsKey(user))
                    return new List<string>();
                else
                    return this.Friends[user];
            }
        }

        public Friend GetFriend(string user) { return this.GetFriend(this.Parent.GetUserID(user)); }
        public Friend GetFriend(UInt32 userid)
        {
            lock (this.OnlineFriends)
                if (this.OnlineFriends.ContainsKey(userid))
                    return this.OnlineFriends[userid];

            lock (this.OfflineFriends)
                if (this.OfflineFriends.ContainsKey(userid))
                    return this.OfflineFriends[userid];

            return null;
        }

        public void Add(string section, string user)
        {
            if (this.Parent.IsBot(user))
                return;

            section = section.ToLower();
            user = user.ToLower();
            UInt32 userID = this.Parent.GetUserID(user);
            if (userID == 0)
                return;

            List<string> sections = this.GetSections(user);
            if (sections.Contains(section))
                return;

            this.DatabaseAdd(section, user);
            this.Add(userID);
        }

        private void Add(UInt32 userID)
        {
            Chat bot = null;
            Int32 size = -1;
            if (this.Parent.GetMainBot().GetTotalFriends() < FriendList.FRIENDMAX && this.Parent.GetMainBot().State == ChatState.Connected)
                bot = this.Parent.GetMainBot();
            else
            {
                foreach (KeyValuePair<int, Chat> slave in this.Parent.Slaves)
                    if (slave.Value.GetTotalFriends() < FriendList.FRIENDMAX && slave.Value.State == ChatState.Connected && slave.Value.GetTotalFriends() > size)
                    {
                        size = slave.Value.GetTotalFriends();
                        bot = slave.Value;
                    }
            }

            if (bot == null)
                throw new Exception("No friend slot available!");

            bot.SendFriendAdd(userID);
        }

        public void Remove(string section, string user)
        {
            section = section.ToLower();
            user = user.ToLower();
            UInt32 userID = this.Parent.GetUserID(user);
            if (userID == 0)
                return;

            List<string> sections = this.GetSections(user);
            if (!sections.Contains(section))
                return;

            this.DatabaseRemove(section, user);
            if (this.GetSections(user).Count == 0)
                this.Remove(userID);
        }
        private void Remove(UInt32 userID)
        {
            Friend friend = null;
            lock (this.OnlineFriends)
                if (this.OnlineFriends.ContainsKey(userID))
                {
                    friend = this.OnlineFriends[userID];
                    this.OnlineFriends.Remove(userID);
                }

            lock (this.OfflineFriends)
                if (this.OfflineFriends.ContainsKey(userID))
                {
                    friend = this.OfflineFriends[userID];
                    this.OfflineFriends.Remove(userID);
                }

            if (friend == null)
                return;

            if (friend.OnSlave)
                if (this.Parent.Slaves.ContainsKey(friend.BotID))
                    this.Parent.Slaves[friend.BotID].SendFriendRemove(friend.UserID);
                else
                    BotShell.Output("[Error] Unable to remove friend (" + friend.User + ") located on nonexisting slave (" + friend.BotID + ")!");
            else
                this.Parent.GetMainBot().SendFriendRemove(friend.UserID);
        }

        public string[] Online(string section)
        {
            List<string> users = new List<string>();
            foreach (string user in this.List(section))
                if (this.IsFriend(user))
                    if (this.IsOnline(user) == OnlineState.Online)
                        users.Add(Format.UppercaseFirst(user));
            users.Sort();
            return users.ToArray();
        }

        public string[] Offline(string section)
        {
            List<string> users = new List<string>();
            foreach (string user in this.List(section))
                if (this.IsFriend(user))
                    if (this.IsOnline(user) != OnlineState.Online)
                        users.Add(user);
            
            users.Sort();
            return users.ToArray();
        }

        public string[] List(string section)
        {
            List<string> users = new List<string>();
            section = section.ToLower();
            lock (this.Friends)
            {
                foreach (KeyValuePair<string, List<string>> kvp in this.Friends)
                {
                    if (kvp.Value.Contains(section))
                        users.Add(kvp.Key);
                }
            }

            users.Sort();
            return users.ToArray();
        }

        public OnlineState IsOnline(string friend) { return this.IsOnline(this.Parent.GetUserID(friend)); }
        public OnlineState IsOnline(UInt32 friend)
        {
            if (friend < 1)
                return OnlineState.Unknown;

            string user = this.Parent.GetUserName(friend);
            OnlineState result = OnlineState.Timeout;
            bool lookup = true;
            for (int i = 0; i < 15; i++)
            {
                lock (this.OfflineFriends)
                    if (this.OfflineFriends.ContainsKey(friend))
                        result = OnlineState.Offline;

                lock (this.OnlineFriends)
                    if (this.OnlineFriends.ContainsKey(friend))
                        result = OnlineState.Online;

                if (result == OnlineState.Timeout && lookup == true)
                {
                    this.Add("tmp", user);
                    lookup = false;
                }
                if (result == OnlineState.Timeout)
                    Thread.Sleep(1000);
                else
                    break;
            }
            if (lookup == false)
                this.Remove("tmp", user);

            return result;
        }

        public bool IsFriend(string section, string friend)
        {
            return this.GetSections(friend).Contains(section.ToLower());
        }
        public bool IsFriend(string friend) { return this.IsFriend(this.Parent.GetUserID(friend)); }
        public bool IsFriend(UInt32 friend)
        {
            lock (this.OfflineFriends)
                if (this.OfflineFriends.ContainsKey(friend))
                    return true;

            lock (this.OnlineFriends)
                if (this.OnlineFriends.ContainsKey(friend))
                    return true;

            return false;
        }

        public Int64 Seen(string friend)
        {
            if (this.IsFriend(friend) && this.IsOnline(friend) == OnlineState.Online)
                return TimeStamp.Now;

            Int64 result = 0;
            try
            {
                lock (this.Config)
                {
                    using (IDbCommand command = this.Config.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT LastOnline FROM CORE_Notify WHERE Username = '" + friend.ToLower() + "'";
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            Int64 tmp = reader.GetInt64(0);
                            if (tmp > result)
                                result = tmp;
                        }
                        reader.Close();
                    }
                }
            }
            catch { }
            return result;
        }

        public void Sync()
        {
            if (this.Syncing)
                return;

            this.Syncing = true;
            try
            {
                this.RebuildCache();
                lock (this.Friends)
                {
                    foreach (string user in this.Friends.Keys)
                    {
                        UInt32 userid = this.Parent.GetUserID(user);
                        if (userid < 1)
                            continue;

                        Friend friend = this.GetFriend(userid);
                        if (friend == null)
                            this.Add(userid);
                        else
                        {
                            Chat bot = null;
                            if (friend.OnSlave)
                                lock (this.Parent.Slaves)
                                    if (this.Parent.Slaves.ContainsKey(friend.BotID))
                                        bot = this.Parent.Slaves[friend.BotID];
                                    else
                                    {
                                        this.Remove(userid);
                                        this.Add(userid);
                                    }
                            else
                                bot = this.Parent.GetMainBot();

                            if (bot != null)
                                if (!bot.GetOfflineFriends().ContainsKey(userid) && !bot.GetOnlineFriends().ContainsKey(userid))
                                {
                                    this.Remove(userid);
                                    this.Add(userid);
                                }
                        }
                    }
                }
                List<Chat> bots;
                List<UInt32> userids = new List<UInt32>();
                lock (this.Parent.Slaves)
                    bots = new List<Chat>(this.Parent.Slaves.Values);
                bots.Add(this.Parent.GetMainBot());
                foreach (Chat bot in bots)
                {
                    List<UInt32> ids = new List<UInt32>();
                    ids.AddRange(bot.GetOnlineFriends().Keys);
                    ids.AddRange(bot.GetOfflineFriends().Keys);
                    foreach (UInt32 id in ids)
                    {
                        if (userids.Contains(id))
                        {
                            bot.SendFriendRemove(id);
                            continue;
                        }
                        lock (this.Friends)
                        {
                            string user = bot.GetUserName(id).ToLower();
                            if (!this.Friends.ContainsKey(user))
                                bot.SendFriendRemove(id);
                        }
                        userids.Add(id);
                    }
                }
            }
            catch { }
            this.Syncing = false;
        }

        public void RebuildCache()
        {
            try
            {
                lock (this.Friends)
                {
                    this.Friends.Clear();
                    lock (this.Config)
                    {
                        using (IDbCommand command = this.Config.Connection.CreateCommand())
                        {
                            command.CommandText = "SELECT Section, Username FROM CORE_Notify ORDER BY Section, Username";
                            IDataReader reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                try
                                {
                                    string section = reader.GetString(0).ToLower();
                                    string user = reader.GetString(1).ToLower();
                                    if (!this.Friends.ContainsKey(user))
                                        this.Friends.Add(user, new List<string>());

                                    if (!this.Friends[user].Contains(section))
                                        this.Friends[user].Add(section);
                                }
                                catch { }
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch { }
        }

        [Obsolete("You shouldn't be using this list except for very exceptional situations! Use Online(string section) instead")]
        public Dictionary<UInt32, Friend> OnlineAll()
        {
            Dictionary<UInt32, Friend> friends;
            lock (this.OnlineFriends)
                friends = new Dictionary<UInt32, Friend>(this.OnlineFriends);

            return friends;
        }

        [Obsolete("You shouldn't be using this list except for very exceptional situations! Use Offline(string section) instead")]
        public Dictionary<UInt32, Friend> OfflineAll()
        {
            Dictionary<UInt32, Friend> friends;
            lock (this.OfflineFriends)
                friends = new Dictionary<UInt32, Friend>(this.OfflineFriends);

            return friends;
        }

        [Obsolete("You shouldn't be using this list except for very exceptional situations! Use List(string section) instead")]
        public Dictionary<UInt32, Friend> ListAll()
        {
            Dictionary<UInt32, Friend> friends;
            lock (this.OfflineFriends)
                friends = new Dictionary<UInt32, Friend>(this.OfflineFriends);
            lock (this.OnlineFriends)
            {
                foreach (KeyValuePair<UInt32, Friend> kvp in this.OnlineFriends)
                {
                    friends.Add(kvp.Key, kvp.Value);
                }
            }
            return friends;
        }

        public Int32 UsedSlots
        {
            get
            {
                Int32 total = 0;
                lock (this.OnlineFriends)
                    total += this.OnlineFriends.Count;
                lock (this.OfflineFriends)
                    total += this.OfflineFriends.Count;

                return total;
            }
        }

        public Int32 TotalSlots
        {
            get
            {
                return FriendList.FRIENDMAX + (this.Parent.Slaves.Count * FriendList.FRIENDMAX);
            }
        }
    }
}
