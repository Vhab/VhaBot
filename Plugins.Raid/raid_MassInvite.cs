using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidMassInvite : PluginBase
    {
        private BotShell _bot;

        public RaidMassInvite()
        {
            this.Name = "Raid :: Mass Invite";
            this.InternalName = "RaidMassInvite";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Description = "Sends Out Announcements and Mass Invites";
            this.Commands = new Command[] {
                new Command("announce", true, UserLevel.Leader),
                new Command("mass", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            lock (this)
            {
                switch (e.Command)
                {
                    case "announce":
                        this.OnMassCommand(bot, e, false);
                        break;
                    case "mass":
                        this.OnMassCommand(bot, e, true);
                        break;
                }
            }
        }

        public string[] GetOnlineMembers()
        {
            List<string> members = new List<string>();
            foreach (string alt in this._bot.Users.GetAllAlts().Keys)
                members.Add(alt.ToLower());
            foreach (string member in this._bot.Users.GetUsers().Keys)
                members.Add(member.ToLower());

            List<string> online = new List<string>();
            foreach (string member in members)
            {
                if (this._bot.FriendList.IsOnline(member) == OnlineState.Online)
                    online.Add(member);
                if (!this._bot.FriendList.IsFriend(this.InternalName, member))
                    this._bot.FriendList.Add(this.InternalName, member);
            }

            foreach (string friend in this._bot.FriendList.List(this.InternalName))
                if (!members.Contains(friend))
                    this._bot.FriendList.Remove(this.InternalName, friend);

            return online.ToArray();
        }

        private void OnMassCommand(BotShell bot, CommandArgs e, bool invite)
        {
            if (e.Words.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: " + e.Command + " [message]");
                return;
            }
            string[] members = this.GetOnlineMembers();
            if (invite)
                bot.SendReply(e, "Sending out an mass invite to " + HTML.CreateColorString(bot.ColorHeaderHex, members.Length.ToString()) + " members");
            else
                bot.SendReply(e, "Sending out an announcement to " + HTML.CreateColorString(bot.ColorHeaderHex, members.Length.ToString()) + " members");
            string message = bot.ColorHighlight + "Message from " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " »» " + bot.ColorNormal + e.Words[0];
            foreach (string member in members)
            {
                if (invite && !bot.PrivateChannel.IsOn(member))
                    bot.PrivateChannel.Invite(member);
            }
            foreach (string member in members)
                bot.SendPrivateMessage(bot.GetUserID(member), message, AoLib.Net.PacketQueue.Priority.Low, true);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "announce":
                    return "Allows you to send out an announcement to all online members of this bot.\n" +
                        "Usage: /tell " + bot.Character + " announce [message]";
                case "mass":
                    return "Allows you to send out an announcement to all online members of this bot and invite them to the private channel.\n" +
                        "Usage: /tell " + bot.Character + " mass [message]";
            }
            return null;
        }
    }
}
