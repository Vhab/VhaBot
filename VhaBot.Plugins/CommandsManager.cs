using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class CommandManager : PluginBase
    {
        public CommandManager()
        {
            this.Name = "Commands Manager";
            this.InternalName = "VhCommandsManager";
            this.Author = "Vhab";
            this.Description = "Provides a UI for managing command access rights";
            this.DefaultState = PluginState.Core;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("commands", true, UserLevel.SuperAdmin),
                new Command("commands set", false, UserLevel.SuperAdmin),
                new Command("commands reset", false, UserLevel.SuperAdmin),
                new Command("commands map", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "commands":
                    if (e.Args.Length == 0)
                        this.OnCommandsCommand(bot, e);
                    else
                        this.OnCommandsDisplayCommand(bot, e);
                    break;
                case "commands set":
                    this.OnCommandsSetCommand(bot, e);
                    break;
                case "commands reset":
                    this.OnCommandsResetCommand(bot, e);
                    break;
                case "commands map":
                    this.OnCommandsMapCommand(bot, e);
                    break;
            }
        }

        private void OnCommandsCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Command Rights");
            foreach (string plugin in bot.Plugins.GetPlugins())
            {
                if (bot.Plugins.GetState(plugin) == PluginState.Loaded)
                {
                    string[] commands = bot.Commands.GetCommands(plugin);
                    if (commands.Length > 0)
                    {
                        PluginLoader loader = bot.Plugins.GetLoader(plugin);
                        window.AppendNormalStart();
                        window.AppendString("[");
                        window.AppendBotCommand("Configure", "commands " + plugin.ToLower());
                        window.AppendString("] ");
                        window.AppendColorEnd();

                        window.AppendHighlight(loader.Name);
                        window.AppendNormal(" (" + commands.Length + " commands)");
                        window.AppendLineBreak();
                    }
                }
            }
            bot.SendReply(e, "Commands »» ", window);
        }

        private void OnCommandsDisplayCommand(BotShell bot, CommandArgs e)
        {
            string internalName = e.Args[0].ToLower();
            if (!bot.Plugins.Exists(internalName))
            {
                bot.SendReply(e, "No such plugin!");
                return;
            }
            PluginState state = bot.Plugins.GetState(internalName);
            if (state == PluginState.Core)
            {
                bot.SendReply(e, "You can't change the command rights on Core plugins!");
                return;
            }
            if (state != PluginState.Loaded)
            {
                bot.SendReply(e, "A plugin is required to be loaded before configuring it!");
                return;
            }
            string[] commands = bot.Commands.GetCommands(internalName);
            if (commands.Length < 1)
            {
                bot.SendReply(e, "This plugin has no commands to configure");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            PluginLoader loader = bot.Plugins.GetLoader(internalName);
            window.AppendTitle("Rights");

            window.AppendHighlight("S: ");
            window.AppendNormal("Super Admin");
            window.AppendLineBreak();

            window.AppendHighlight("A: ");
            window.AppendNormal("Admin");
            window.AppendLineBreak();

            window.AppendHighlight("L: ");
            window.AppendNormal("Leader");
            window.AppendLineBreak();

            window.AppendHighlight("M: ");
            window.AppendNormal("Member");
            window.AppendLineBreak();

            window.AppendHighlight("G: ");
            window.AppendNormal("Guest");
            window.AppendLineBreak();

            window.AppendHighlight("D: ");
            window.AppendNormal("Disabled");
            window.AppendLineBreak(2);

            window.AppendHeader("Tell               Private Group  Organization  Command");
            foreach (string command in commands)
            {
                this.CreateSelectLine(bot, ref window, command);
                window.AppendLineBreak();
            }
            bot.SendReply(e, "Commands »» " + loader.Name + " »» ", window);
        }

        private void OnCommandsSetCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 3)
            {
                bot.SendReply(e, "Usage: commands set [command] [target] [level]");
                return;
            }
            string command = e.Args[0].ToLower().Replace("_", " ");
            if (!bot.Commands.Exists(command))
            {
                bot.SendReply(e, "No such command!");
                return;
            }
            if (bot.Plugins.GetState(bot.Commands.GetInternalName(command)) == PluginState.Core)
            {
                bot.SendReply(e, "You can't change the command rights on Core plugins!");
                return;
            }
            CommandType type = CommandType.Tell;
            string friendlyType = string.Empty;
            switch (e.Args[1].ToLower())
            {
                case "tell":
                    type = CommandType.Tell;
                    friendlyType = "tells";
                    break;
                case "pg":
                    type = CommandType.PrivateChannel;
                    friendlyType = "the private channel";
                    break;
                case "org":
                    type = CommandType.Organization;
                    friendlyType = "the organization channel";
                    break;
                default:
                    bot.SendReply(e, "Invalid target. Valid targets are: tell, pg, org");
                    return;
            }
            UserLevel right = UserLevel.SuperAdmin;
            try
            {
                right = (UserLevel)Enum.Parse(typeof(UserLevel), e.Args[2]);
            }
            catch
            {
                bot.SendReply(e, "Invalid level. Valid levels are: SuperAdmin, Admin, Leader, Member, Guest, Disabled");
                return;
            }
            bot.Commands.SetRight(command, type, right);
            if (right != UserLevel.Disabled)
                bot.SendReply(e, "The required userlevel for the command " + HTML.CreateColorString(bot.ColorHeaderHex, command) + " from " + friendlyType + " has been set to " + HTML.CreateColorString(bot.ColorHeaderHex, right.ToString()));
            else
                bot.SendReply(e, "Access to the command " + HTML.CreateColorString(bot.ColorHeaderHex, command) + " from " + friendlyType + " has been disabled");
        }

        private void OnCommandsResetCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Usage: commands reset [command]");
                return;
            }
            string command = e.Args[0].ToLower().Replace("_", " ");
            if (!bot.Commands.Exists(command))
            {
                bot.SendReply(e, "No such command!");
                return;
            }
            if (bot.Plugins.GetState(bot.Commands.GetInternalName(command)) == PluginState.Core)
            {
                bot.SendReply(e, "You can't reset the command rights on Core plugins!");
                return;
            }
            bot.Commands.ResetRights(command);
            CommandRights rights = bot.Commands.GetRights(command);
            bot.SendReply(e, "The required userlevel for the command " + HTML.CreateColorString(bot.ColorHeaderHex, command) + " has been reset. Tell: " + HTML.CreateColorString(bot.ColorHeaderHex, rights.PrivateMessage.ToString()) + ", Private Channel: " + HTML.CreateColorString(bot.ColorHeaderHex, rights.PrivateChannel.ToString()) + ", Organization: " + HTML.CreateColorString(bot.ColorHeaderHex, rights.Organization.ToString()));
        }

        private void CreateSelectLine(BotShell bot, ref RichTextWindow window, string command)
        {
            CommandRights rights = bot.Commands.GetRights(command);
            command = command.Replace(" ", "_");
            this.CreateSelectPanel(ref window, rights.PrivateMessage, "commands set " + command + " tell");
            window.AppendString(" ");
            this.CreateSelectPanel(ref window, rights.PrivateChannel, "commands set " + command + " pg");
            window.AppendString(" ");
            this.CreateSelectPanel(ref window, rights.Organization, "commands set " + command + " org");
            window.AppendHighlight(" " + Format.UppercaseFirst(command.Replace("_", " ")));

            window.AppendNormalStart();
            window.AppendString(" [");
            window.AppendBotCommand("Reset", "commands reset " + command);
            window.AppendString("]");
            window.AppendColorEnd();
        }

        private void CreateSelectPanel(ref RichTextWindow window, UserLevel right, string command)
        {
            command = command.Trim();
            window.AppendNormalStart();

            window.AppendString("[");
            window.AppendBotCommand("D", command + " Disabled", (right == UserLevel.Disabled));
            window.AppendString(" ");
            window.AppendBotCommand("G", command + " Guest", (right == UserLevel.Guest));
            window.AppendString(" ");
            window.AppendBotCommand("M", command + " Member", (right == UserLevel.Member));
            window.AppendString(" ");
            window.AppendBotCommand("L", command + " Leader", (right == UserLevel.Leader));
            window.AppendString(" ");
            window.AppendBotCommand("A", command + " Admin", (right == UserLevel.Admin));
            window.AppendString(" ");
            window.AppendBotCommand("S", command + " SuperAdmin", (right == UserLevel.SuperAdmin));
            window.AppendString("]");

            window.AppendColorEnd();
        }

        private void OnCommandsMapCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Commands Map");
            window.AppendNormal("This is a map of all commands currently available on this bot.");
            window.AppendLineBreak();
            window.AppendNormal("The commands are listed in the following format: [command] ([tell] [pg] [org] / [default tell] [default pg] [default org])");
            window.AppendLineBreak();
            window.AppendNormal("The letters represent the required userlevel you need for the command.");
            window.AppendLineBreak();
            window.AppendNormal("The color of the command represents whether the command has help or not. Red means there is no help available. Orange means there should be help available but this command was unable to retrieve it. Green means there is help available.");
            window.AppendLineBreak(2);

            foreach (string plugin in bot.Plugins.GetLoadedPlugins())
            {
                string[] commands = bot.Commands.GetCommands(plugin);
                PluginLoader loader = bot.Plugins.GetLoader(plugin);
                window.AppendHeader(loader.ToString());
                if (commands.Length > 0)
                {
                    foreach (string command in commands)
                    {
                        CommandRights rights = bot.Commands.GetRights(command);
                        if (rights.Help)
                            if (bot.Commands.GetHelp(command) != string.Empty)
                                window.AppendColorStart(RichTextWindow.ColorGreen);
                            else
                                window.AppendColorStart(RichTextWindow.ColorOrange);
                        else
                            window.AppendColorStart(RichTextWindow.ColorRed);
                        window.AppendString(Format.UppercaseFirst(command)); 
                        window.AppendColorEnd();

                        string tell = rights.PrivateMessage.ToString().Substring(0, 1).ToUpper();
                        string pg = rights.PrivateChannel.ToString().Substring(0, 1).ToUpper();
                        string org = rights.Organization.ToString().Substring(0, 1).ToUpper();
                        rights = bot.Commands.GetDefaultRights(command);
                        string d_tell = rights.PrivateMessage.ToString().Substring(0, 1).ToUpper();
                        string d_pg = rights.PrivateChannel.ToString().Substring(0, 1).ToUpper();
                        string d_org = rights.Organization.ToString().Substring(0, 1).ToUpper();

                        window.AppendNormal(" (" + tell + pg + org + "/" + d_tell + d_pg + d_org + ")");
                        window.AppendLineBreak();
                    }
                }
                else
                {
                    window.AppendNormal("No commands available");
                    window.AppendLineBreak();
                }
                window.AppendLineBreak();
            }
            bot.SendReply(e, "Commands Map »» ", window);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "commands":
                    return "Displays the commands configuration interface.\nFrom this interface you can change the user level requirements on all commands (Except for commands registered by Core Plugins).\n" +
                        "Usage: /tell " + bot.Character + " commands [[plugin]]";
                case "commands map":
                    return "Displays a map of all commands, whether they have help available and the user level requirements.\n" +
                        "Usage: /tell " + bot.Character + " commands map";
            }
            return null;
        }
    }
}