using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;
using VhaBot;
using VhaBot.ShellModules;
using VhaBot.Communication;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class VhanetMassInviteHost : PluginBase
    {
        private VhanetFriendsList _friendsList;
        public VhanetMassInviteHost()
        {
            this.Name = "Vhanet :: Mass Invite Host";
            this.InternalName = "VhanetMassInviteHost";
            this.Version = 100;
            this.Author = "Vhab";
            this.Dependencies = new string[] { "VhanetFriendsList" };
            this.DefaultState = PluginState.Disabled;
            this.Description = "Hosts the mass announcements and invites system";
            this.Commands = new Command[] {
                new Command("broadcast", false, UserLevel.Admin)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._friendsList = (VhanetFriendsList)bot.Plugins.GetPlugin("VhanetFriendsList"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Friends List' Plugin!"); }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Command != "broadcast")
                return;

            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: broadcast [message]");
                return;
            }
            string[] members = this._friendsList.GetOnlineMembers();
            string formattedMessage = bot.ColorHighlight + "Broadcast from " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " »» " + bot.ColorNormal + e.Words[0].Trim();
            bot.SendReply(e, "Sending out a broadcast to " + HTML.CreateColorString(bot.ColorHeaderHex, members.Length.ToString()) + " members");
            foreach (string member in members)
                bot.SendPrivateMessage(bot.GetUserID(member), formattedMessage, AoLib.Net.PacketQueue.Priority.Low, true);
        }

        public override void OnPluginMessage(BotShell bot, PluginMessage message)
        {
            try
            {
                if (message.Command != "announce")
                    return;
                if (message.Args.Length < 3)
                    return;

                string group = (string)message.Args[0];
                if (group == null || group == string.Empty)
                    return;
                string source = (string)message.Args[1];
                if (source == null || source == string.Empty)
                    return;
                string sender = (string)message.Args[2];
                if (sender == null || sender == string.Empty)
                    return;
                string msg = (string)message.Args[3];
                if (msg == null || msg == string.Empty)
                    return;

                string formattedMessage = bot.ColorHighlight + "Message from " + HTML.CreateColorString(bot.ColorHeaderHex, sender) + " on " + HTML.CreateColorString(bot.ColorHeaderHex, source) + " »» " + bot.ColorNormal + msg;
                string[] members = this._friendsList.GetOnlineMembers(group, true);
                bot.SendPrivateMessage(sender, bot.ColorHighlight + "Sending out an announcement to " + HTML.CreateColorString(bot.ColorHeaderHex, members.Length.ToString()) + " members");
                foreach (string member in members)
                    bot.SendPrivateMessage(bot.GetUserID(member), formattedMessage, AoLib.Net.PacketQueue.Priority.Low, true);
                members = this._friendsList.GetOnlineMembers(group, false);
                bot.SendReplyMessage(this.InternalName, message, (object)members);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new Exception("exception", ex);
            }
        }
    }
}
