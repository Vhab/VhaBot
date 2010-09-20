using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.CorePlugins
{
    public class HelpManager : PluginBase
    {
        public HelpManager()
        {
            this.Name = "Help Manager";
            this.InternalName = "VhHelpManager";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Core;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("help", false, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot) { }

        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length == 0)
                this.OnHelpCommand(bot, e);
            else
                this.OnHelpDisplayCommand(bot, e);
        }

        private void OnHelpCommand(BotShell bot, CommandArgs e) {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("VhaBot Help");
            bool found = false;
            foreach (string plugin in bot.Plugins.GetPlugins())
            {
                if (bot.Plugins.IsLoaded(plugin))
                {
                    string[] commands = bot.Commands.GetCommands(plugin);
                    List<string> helpCommands = new List<string>();
                    foreach (string command in commands)
                    {
                        CommandRights rights = bot.Commands.GetRights(command);
                        if (rights.Help && !rights.IsAlias)
                            helpCommands.Add(command);
                    }
                    helpCommands.Sort();
                    if (helpCommands.Count > 0)
                    {
                        PluginLoader loader = bot.Plugins.GetLoader(plugin);
                        window.AppendHighlight(loader.Name);
                        window.AppendLineBreak();
                        window.AppendNormalStart();
                        int i = 0;
                        foreach (string command in helpCommands)
                        {
                            window.AppendBotCommand(Format.UppercaseFirst(command), "help " + command);
                            i++;
                            if (i < helpCommands.Count)
                                window.AppendString(", ");
                        }
                        window.AppendColorEnd();
                        window.AppendLineBreak(2);
                        found = true;
                    }
                }
            }
            if (found)
                bot.SendReply(e, "VhaBot Help »» ", window);
            else
                bot.SendReply(e, "No help available");
        }

        private void OnHelpDisplayCommand(BotShell bot, CommandArgs e)
        {
            string command = e.Words[0];
            command = bot.Commands.GetMainCommand(command);
            if (!bot.Commands.Exists(command) || !bot.Commands.GetRights(command).Help)
            {
                bot.SendReply(e, "No such help topic");
                return;
            }
            
            PluginLoader loader = bot.Plugins.GetLoader(bot.Commands.GetInternalName(command));
            CommandRights rights = bot.Commands.GetRights(command);
            UserLevel level = bot.Users.GetUser(e.Sender);

            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Information");
            window.AppendHighlight("Command: ");
            window.AppendNormal(Format.UppercaseFirst(command));
            window.AppendLineBreak();
            window.AppendHighlight("Plugin: ");
            window.AppendNormal(loader.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Private Message Access: ");
            if (level >= rights.PrivateMessage)
                window.AppendColorString(RichTextWindow.ColorGreen, rights.PrivateMessage.ToString());
            else
                window.AppendColorString(RichTextWindow.ColorRed, rights.PrivateMessage.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Private Channel Access: ");
            if (level >= rights.PrivateChannel)
                window.AppendColorString(RichTextWindow.ColorGreen, rights.PrivateChannel.ToString());
            else
                window.AppendColorString(RichTextWindow.ColorRed, rights.PrivateChannel.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("Organization Access: ");
            if (level >= rights.Organization)
                window.AppendColorString(RichTextWindow.ColorGreen, rights.Organization.ToString());
            else
                window.AppendColorString(RichTextWindow.ColorRed, rights.Organization.ToString());
            window.AppendLineBreak(2);

            window.AppendHeader("Help");
            window.AppendHighlightStart();
            string help = bot.Commands.GetHelp(command);
            if (help == null || help.Trim() == string.Empty)
                window.AppendString("No additional help available for this command");
            else
                window.AppendRawString(help);
            window.AppendColorEnd();

            bot.SendReply(e, "VhaBot Help »» " + Format.UppercaseFirst(command) + " »» ", window);
        }
    }
}