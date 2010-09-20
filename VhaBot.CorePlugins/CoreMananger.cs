using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using AoLib.Utils;
using AoLib.Net;
using VhaBot.Communication;

namespace VhaBot.CorePlugins
{
    public class CoreManager : PluginBase
    {
        public CoreManager()
        {
            this.Name = "The Core";
            this.InternalName = "VhCoreManager";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Core;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("shutdown", false, UserLevel.SuperAdmin),
                new Command("restart", false, UserLevel.SuperAdmin),
                new Command("reboot", "restart"),
                new Command("core", false, UserLevel.SuperAdmin),
                new Command("core shutdown", false, UserLevel.SuperAdmin),
                new Command("core start", false, UserLevel.SuperAdmin),
                new Command("core restart", false, UserLevel.SuperAdmin),
                new Command("core clean", false, UserLevel.SuperAdmin)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.String, this.InternalName, "syntax", "Command Syntax", "!", "!", "@", "#", "?", "$", "%", "^", "*", "~", "|");
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "maxwindowsize_privatemessage", "Maximum Window Size for Private Messages", bot.MaxWindowSizePrivateMessage);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "maxwindowsize_privatechannel", "Maximum Window Size for Private Channel Messages", bot.MaxWindowSizePrivateChannel);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "maxwindowsize_organization", "Maximum Window Size for Organization Messages", bot.MaxWindowSizeOrganization);

            bot.Configuration.Register(ConfigType.Color, this.InternalName, "color_header", "Header Color", bot.ColorHeaderHex);
            bot.Configuration.Register(ConfigType.Color, this.InternalName, "color_header_detail", "Header Detail Color", bot.ColorHeaderDetailHex);
            bot.Configuration.Register(ConfigType.Color, this.InternalName, "color_highlight", "Highlight Color", bot.ColorHighlightHex);
            bot.Configuration.Register(ConfigType.Color, this.InternalName, "color_normal", "Normal Color", bot.ColorNormalHex);

            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            bot.Events.BotStateChangedEvent += new BotStateChangedHandler(Events_BotStateChangedEvent);
            bot.Events.ChannelJoinEvent += new ChannelJoinEventHandler(Events_ChannelJoinEvent);

            bot.CommandSyntax = bot.Configuration.GetString(this.InternalName, "syntax", bot.CommandSyntax);
            bot.MaxWindowSizePrivateMessage = bot.Configuration.GetInteger(this.InternalName, "maxwindowsize_privatemessage", bot.MaxWindowSizePrivateMessage);
            bot.MaxWindowSizePrivateChannel = bot.Configuration.GetInteger(this.InternalName, "maxwindowsize_privatechannel", bot.MaxWindowSizePrivateChannel);
            bot.MaxWindowSizeOrganization = bot.Configuration.GetInteger(this.InternalName, "maxwindowsize_organization", bot.MaxWindowSizeOrganization);

            bot.ColorHeaderHex = bot.Configuration.GetColor(this.InternalName, "color_header", bot.ColorHeaderHex);
            bot.ColorHeaderDetailHex = bot.Configuration.GetColor(this.InternalName, "color_header_detail", bot.ColorHeaderDetailHex);
            bot.ColorHighlightHex = bot.Configuration.GetColor(this.InternalName, "color_highlight", bot.ColorHighlightHex);
            bot.ColorNormalHex = bot.Configuration.GetColor(this.InternalName, "color_normal", bot.ColorNormalHex);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            bot.Events.BotStateChangedEvent -= new BotStateChangedHandler(Events_BotStateChangedEvent);
            bot.Events.ChannelJoinEvent -= new ChannelJoinEventHandler(Events_ChannelJoinEvent);
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName.ToLower())
            {
                switch (e.Key)
                {
                    case "syntax":
                        bot.CommandSyntax = (string)e.Value;
                        break;
                    case "maxwindowsize_privatemessage":
                        bot.MaxWindowSizePrivateMessage = (int)e.Value;
                        break;
                    case "maxwindowsize_privatechannel":
                        bot.MaxWindowSizePrivateChannel = (int)e.Value;
                        break;
                    case "maxwindowsize_organization":
                        bot.MaxWindowSizeOrganization = (int)e.Value;
                        break;
                    case "color_header":
                        bot.ColorHeaderHex = (string)e.Value;
                        break;
                    case "color_header_detail":
                        bot.ColorHeaderDetailHex = (string)e.Value;
                        break;
                    case "color_highlight":
                        bot.ColorHighlightHex = (string)e.Value;
                        break;
                    case "color_normal":
                        bot.ColorNormalHex = (string)e.Value;
                        break;
                }
            }
        }

        private void Events_BotStateChangedEvent(BotShell bot, BotStateChangedArgs e)
        {
            if (e.IsSlave) return;
            if (e.State == BotState.Connected)
                bot.SendPrivateChannelMessage(bot.ColorHighlight + "System »» Online");
        }

        private void Events_ChannelJoinEvent(BotShell bot, ChannelJoinEventArgs e)
        {
            //Console.WriteLine("Joining channel: " + e.GroupType.ToString());
            if (e.GroupType == ChannelType.Organization)
                bot.SendOrganizationMessage(bot.ColorHighlight + "System »» Online");
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            // make !status display various stats of the bot and the current state of all slaves and their friendslists, also include links to relog etc
            switch (e.Command)
            {
                case "shutdown":
                    bot.SendOrganizationMessage(bot.ColorHighlight + "System »» Shutting Down");
                    bot.SendPrivateMessage(e.SenderID, bot.ColorHighlight + "System »» Shutting Down", AoLib.Net.PacketQueue.Priority.Urgent, true);
                    bot.SendPrivateChannelMessage(bot.ColorHighlight + "System »» Shutting Down");
                    System.Threading.Thread.Sleep(1000);
                    bot.Shutdown();
                    break;
                case "restart":
                    if (!bot.CoreConnected)
                    {
                        bot.SendReply(e, "Restart is not available");
                        return;
                    }
                    bot.SendOrganizationMessage(bot.ColorHighlight + "System »» Rebooting");
                    bot.SendPrivateMessage(e.SenderID, bot.ColorHighlight + "System »» Rebooting", AoLib.Net.PacketQueue.Priority.Urgent, true);
                    bot.SendPrivateChannelMessage(bot.ColorHighlight + "System »» Rebooting");
                    System.Threading.Thread.Sleep(1000);
                    bot.Restart();
                    break;
                case "core":
                    this.OnCoreCommand(bot, e);
                    break;
                case "core shutdown":
                case "core start":
                case "core restart":
                    if (e.Args.Length < 1 || !e.Args[0].Contains("@"))
                    {
                        bot.SendReply(e, "Usage: " + e.Command + " [bot@dimension]");
                        return;
                    }
                    if (!bot.Master)
                    {
                        bot.SendReply(e, "This bot is not a master bot");
                        return;
                    }
                    CoreCommand command;
                    if (e.Command == "core shutdown") command = CoreCommand.Shutdown;
                    else if (e.Command == "core start") command = CoreCommand.Start;
                    else command = CoreCommand.Restart;
                    MessageResult result = bot.SendCoreMessage(this.InternalName, e.Args[0].ToLower(), command);
                    switch(result)
                    {
                        case MessageResult.Success:
                            bot.SendReply(e, "Your command has been relayed to the core and will be executed shortly");
                            break;
                        case MessageResult.InvalidTarget:
                            bot.SendReply(e, "The target you specified appeared to be invalid");
                            break;
                        case MessageResult.NotConnected:
                            bot.SendReply(e, "Unable to connect to the core in order to issue this command");
                            break;
                        default:
                            bot.SendReply(e, "An unknown error prevented your command from being executed");
                            break;
                    }
                    break;
                case "core clean":
                    bot.Clean();
                    bot.SendReply(e, "Memory cleaning routines have been executed");
                    break;
            }
        }

        private void OnCoreCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("BotShell Status");
            window.AppendHighlight("Current Thread: ");
            window.AppendNormal(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Owner: ");
            window.AppendNormal(bot.Admin);
            window.AppendLineBreak();
            window.AppendHighlight("Registered Commands: ");
            window.AppendNormal(bot.Commands.GetCommandsCount().ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Command Syntax: ");
            window.AppendNormal(bot.CommandSyntax);
            window.AppendLineBreak();
            window.AppendHighlight("Max Window Size Private Message: ");
            window.AppendNormal(bot.MaxWindowSizePrivateMessage.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Max Window Size Private Channel: ");
            window.AppendNormal(bot.MaxWindowSizePrivateChannel.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Max Window Size Organization: ");
            window.AppendNormal(bot.MaxWindowSizeOrganization.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Friendslist Usage: ");
            window.AppendNormal(bot.FriendList.UsedSlots + "/" + bot.FriendList.TotalSlots);
            window.AppendLineBreak();
            window.AppendHighlight("Runtime Version: ");
            window.AppendNormal(Environment.Version.ToString());
            try
            {
                // Non-priviliged accounts and mono might not like this, but I do \o/
                Process process = Process.GetCurrentProcess();
                window.AppendLineBreak();
                window.AppendHighlight("Process ID: ");
                window.AppendNormal(process.Id.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Threads: ");
                window.AppendNormal(process.Threads.Count.ToString());
                foreach (ProcessThread thread in process.Threads)
                {
                    window.AppendLineBreak();
                    window.AppendHighlight("Threads #" + thread.Id + ": ");
                    window.AppendNormal(thread.ThreadState.ToString() + " (S: " + Format.Date(thread.StartTime, FormatStyle.Compact) + " " + Format.Time(thread.StartTime, FormatStyle.Medium) + " / U: " + Format.Time(thread.TotalProcessorTime, FormatStyle.Compact) + (thread.ThreadState == ThreadState.Wait ? " / W: " + thread.WaitReason.ToString() : "") + ")");
                }
            }
            catch { }
            window.AppendLineBreak(2);

            List<Chat> chats = new List<Chat>();
            chats.Add(bot.GetMainBot());
            for (int i = 1; i <= bot.GetSlavesCount(); i++)
            {
                Chat slave = bot.GetSlaveBot(i);
                if (slave != null)
                    chats.Add(slave);
            }
            foreach (Chat chat in chats)
            {
                window.AppendHeader(chat.Character);
                window.AppendHighlight("Status: ");
                window.AppendNormal(chat.State.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Fast Queue Count: ");
                window.AppendNormal(chat.FastQueueCount.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Slow Queue Count: ");
                window.AppendNormal(chat.SlowQueueCount.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Friends: ");
                window.AppendNormal(chat.GetTotalFriends().ToString());
                window.AppendLineBreak();
                window.AppendHighlight("ID: ");
                window.AppendNormal(chat.ID.ToString());
                window.AppendLineBreak();
                if (chat.Organization != null && chat.Organization != string.Empty)
                {
                    window.AppendHighlight("Organization: ");
                    window.AppendNormal(chat.Organization);
                    window.AppendLineBreak();
                    window.AppendHighlight("Organization Channel: ");
                    window.AppendNormal(chat.OrganizationID.IntValue().ToString());
                    window.AppendLineBreak();
                }
                window.AppendLineBreak();
            }
            bot.SendReply(e, "BotShell Status »» " + window.ToString());
        }
    }
}
