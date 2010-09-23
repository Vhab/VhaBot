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
    public class VhanetMassInviteClient : PluginBase
    {
        private VhanetMembersClient _client;
        public VhanetMassInviteClient()
        {
            this.Name = "Vhanet :: Mass Invite Client";
            this.InternalName = "VhanetMassInviteClient";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Description = "Client to the vhanet mass announcements and invites system";
            this.Dependencies = new string[] { "VhanetMembersClient" };
            this.Commands = new Command[] {
                new Command("announce", true, UserLevel.Leader),
                new Command("mass", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._client = (VhanetMembersClient)bot.Plugins.GetPlugin("VhanetMembersClient"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Members Client' Plugin!"); }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "announce":
                case "mass":
                    bool invite = (e.Command == "mass");
                    if (e.Words.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: " + e.Command + " [message]");
                        return;
                    }
                    string message = e.Words[0].Trim();
                    string group = bot.Configuration.GetString(this._client.InternalName, "group", bot.Character);
                    bot.SendReply(e, "Contacting the Vhanet announce system. Please stand by...");
                    
                    int messageID;
                    MessageResult messageResult;
                    messageResult = bot.SendRemotePluginMessage(this.InternalName, "vhanet@atlantean", "VhanetMassInviteHost", "announce", out messageID, group, bot.Character, e.Sender, message);

                    if (messageResult != MessageResult.Success)
                    {
                        bot.SendReply(e, "Unable to contact the Vhanet announce system. Return code was: " + HTML.CreateColorString(bot.ColorHeaderHex, messageResult.ToString()));
                        return;
                    }
                    ReplyMessage messageReply = bot.GetReplyMessage(messageID, 60000);
                    if (messageReply == null)
                    {
                        bot.SendReply(e, "Didn't receive a reply from the Vhanet announce system");
                        return;
                    }
                    string[] members = (string[])messageReply.Args[0];
                    if (invite)
                    {
                        
                        List<string> massed = new List<string>();
                        foreach (string member in members)
                        {
                            if (!bot.PrivateChannel.IsOn(member))
                            {
                                bot.PrivateChannel.Invite(member);
                                massed.Add(member);
                            }
                        }
                        RichTextWindow window = new RichTextWindow(bot);
                        window.AppendTitle("Members");
                        window.AppendHighlight(string.Join(", ", massed.ToArray()));
                        bot.SendReply(e, "Sending out an mass invite to " + HTML.CreateColorString(bot.ColorHeaderHex, massed.Count.ToString()) + " members »» ", window);
                    }
                    break;
            }
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
