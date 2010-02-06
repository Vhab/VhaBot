using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;
using System.Diagnostics;
using VhaBot.Communication;

namespace VhaBot.Plugins
{
    /*public class Test : PluginBase
    {
        public Test()
        {
            this.Name = "Testing Plugin";
            this.InternalName = "vhTest";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Core;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("test pluginmessage", false, UserLevel.SuperAdmin),
                new Command("test remotepluginmessage", false, UserLevel.SuperAdmin)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }
        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            int messageID;
            ReplyMessage reply;
            switch (e.Command)
            {
                case "test pluginmessage":
                    bot.SendPluginMessage(this.InternalName, "vhTest", "test", out messageID, e.Sender);
                    reply = bot.GetReplyMessage(messageID);
                    if (reply == null)
                        bot.SendReply(e, "No reply...");
                    else
                        bot.SendReply(e, "Reply: " + reply.Args[0]);
                    break;
                case "test remotepluginmessage":
                    bot.SendRemotePluginMessage(this.InternalName, "vbxb1@atlantean", "vhTest", "test", out messageID, e.Sender);
                    reply = bot.GetReplyMessage(messageID);
                    if (reply == null)
                        bot.SendReply(e, "No reply...");
                    else
                        bot.SendReply(e, "Reply: " + reply.Args[0]);
                    break;
            }
        }

        public override void OnPluginMessage(BotShell bot, PluginMessage message)
        {
            switch (message.Command)
            {
                case "test":
                    if (message.Args.Length == 0) return;
                    bot.SendPrivateMessage((string)message.Args[0], "Thanks for calling...");
                    bot.SendReplyMessage(this.InternalName, message, "Hi " + message.Args[0]);
                    break;
            }
        }
    }*/
}
