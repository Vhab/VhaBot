using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Net;

namespace VhaBot.ShellModules
{
    public class PrivateChannel
    {
        private BotShell Parent;

        public PrivateChannel(BotShell parent)
        {
            this.Parent = parent;
        }

        public void Invite(string user)
        {
            this.Parent.GetMainBot().SendPrivateChannelKick(user);
            this.Parent.GetMainBot().SendPrivateChannelInvite(user);
        }
        public void Invite(UInt32 userID)
        {
            this.Invite(this.Parent.GetUserName(userID));
        }
        public void Invite(string user, Int32 slaveID)
        {
            lock (this.Parent.Slaves)
                if (this.Parent.Slaves.ContainsKey(slaveID))
                {
                    this.Parent.Slaves[slaveID].SendPrivateChannelKick(user);
                    this.Parent.Slaves[slaveID].SendPrivateChannelInvite(user);
                }
        }
        public void Invite(UInt32 userID, Int32 slaveID)
        {
            this.Invite(this.Parent.GetUserName(userID), slaveID);
        }

        public void Kick(string user)
        {
            this.Parent.GetMainBot().SendPrivateChannelKick(user);
        }
        public void Kick(UInt32 userID)
        {
            this.Parent.GetMainBot().SendPrivateChannelKick(userID);
        }
        public void Kick(string user, Int32 slaveID)
        {
            lock (this.Parent.Slaves)
                if (this.Parent.Slaves.ContainsKey(slaveID))
                    this.Parent.Slaves[slaveID].SendPrivateChannelKick(user);
        }
        public void Kick(UInt32 userID, Int32 slaveID)
        {
            lock (this.Parent.Slaves)
                if (this.Parent.Slaves.ContainsKey(slaveID))
                    this.Parent.Slaves[slaveID].SendPrivateChannelKick(userID);
        }

        public void KickAll()
        {
            this.Parent.GetMainBot().SendPrivateChannelKickAll();
        }
        public void KickAll(Int32 slaveID)
        {
            lock (this.Parent.Slaves)
                if (this.Parent.Slaves.ContainsKey(slaveID))
                    this.Parent.Slaves[slaveID].SendPrivateChannelKickAll();
        }

        public Dictionary<UInt32, Friend> List() { return this.List(false, 0); }
        public Dictionary<UInt32, Friend> List(Int32 slaveID) { return this.List(true, slaveID); }
        private Dictionary<UInt32, Friend> List(bool slave, Int32 slaveID)
        {
            Chat bot;
            lock (this.Parent.Slaves)
                if (slave)
                    if (this.Parent.Slaves.ContainsKey(slaveID))
                        bot = this.Parent.Slaves[slaveID];
                    else
                        return new Dictionary<UInt32, Friend>();
                else
                    bot = this.Parent.GetMainBot();
            Dictionary<UInt32, DateTime> rawlist = bot.GetPrivateChannelMembers();
            Dictionary<UInt32, Friend> list = new Dictionary<UInt32, Friend>();
            foreach (KeyValuePair<UInt32, DateTime> item in rawlist)
                list.Add(item.Key, new Friend(bot.GetUserName(item.Key), item.Key, item.Value, slave, slaveID));

            return list;
        }

        public bool IsOn(string user) { return this.IsOn(this.Parent.GetUserID(user)); }
        public bool IsOn(UInt32 userID)
        {
            return this.Parent.GetMainBot().GetPrivateChannelMembers().ContainsKey(userID);
        }
        public bool IsOn(string user, Int32 slaveID) { return this.IsOn(this.Parent.GetUserID(user), slaveID); }
        public bool IsOn(UInt32 userID, Int32 slaveID)
        {
            lock (this.Parent.Slaves)
                if (this.Parent.Slaves.ContainsKey(slaveID))
                    return this.Parent.Slaves[slaveID].GetPrivateChannelMembers().ContainsKey(userID);
                else
                    return false;
        }
    }
}
