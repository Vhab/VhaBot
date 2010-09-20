using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.CorePlugins
{
    public class PluginManager : PluginBase
    {
        public PluginManager()
        {
            this.Name = "Plugin Manager";
            this.InternalName = "VhPluginManager";
            this.Author = "Vhab";
            this.Description = "Provides a UI to manage plugins";
            this.DefaultState = PluginState.Core;
            this.Version = 103;
            this.Commands = new Command[] {
                new Command("plugins", true, UserLevel.SuperAdmin),
                new Command("plugins overview", true, UserLevel.SuperAdmin)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Command == "plugins overview")
            {
                this.OnPluginsOverviewCommand(bot, e);
                return;
            }
            if (e.Args.Length > 1)
            {
                if (!bot.Plugins.Exists(e.Args[1]))
                {
                    bot.SendReply(e, "No such plugin: " + e.Args[1]);
                    return;
                }
                switch (e.Args[0])
                {
                    case "install":
                        if (bot.Plugins.Install(e.Args[1]))
                        {
                            bot.SendReply(e, "Installed: " + e.Args[1]);
                            break;
                        }
                        else
                        {
                            bot.SendReply(e, "Unable to install plugin");
                            return;
                        }
                    case "load":
                        PluginLoadResult result = bot.Plugins.Load(e.Args[1]);
                        if (result == PluginLoadResult.Ok)
                        {
                            bot.SendReply(e, "Loaded: " + e.Args[1]);
                            break;
                        }
                        else
                        {
                            bot.SendReply(e, "Unable to load plugin. Returned Code: " + result.ToString());
                            return;
                        }
                    case "uninstall":
                        if (bot.Plugins.Uninstall(e.Args[1]))
                        {
                            bot.SendReply(e, "Uninstalled: " + e.Args[1]);
                            break;
                        }
                        else
                        {
                            bot.SendReply(e, "Unable to uninstall plugin");
                            return;
                        }
                    case "unload":
                        if (bot.Plugins.Unload(e.Args[1]))
                        {
                            bot.SendReply(e, "Unloaded: " + e.Args[1]);
                            break;
                        }
                        else
                        {
                            bot.SendReply(e, "Unable to unload plugin");
                            return;
                        }
                    case "info":
                        this.OnPluginsInfoCommand(bot, e);
                        return;
                    default:
                        bot.SendReply(e, "Unknown Parameter: " + e.Args[0]);
                        return;
                }
            }
            this.OnPluginsCommand(bot, e);
        }

        public void OnPluginsInfoCommand(BotShell bot, CommandArgs e)
        {
            PluginLoader info = bot.Plugins.GetLoader(e.Args[1]);
            if (info == null)
            {
                bot.SendReply(e, "Unable to get plugin information");
                return;
            }
            RichTextWindow infoWindow = new RichTextWindow(bot);
            infoWindow.AppendTitle("Plugin Information");
            infoWindow.AppendHighlight("Name: ");
            infoWindow.AppendNormal(info.Name);
            infoWindow.AppendLineBreak();
            infoWindow.AppendHighlight("Version: ");
            infoWindow.AppendNormal(info.Version.ToString());
            infoWindow.AppendLineBreak();
            infoWindow.AppendHighlight("Author: ");
            infoWindow.AppendNormal(info.Author);
            infoWindow.AppendLineBreak();
            infoWindow.AppendHighlight("Internal Name: ");
            infoWindow.AppendNormal(info.InternalName);
            infoWindow.AppendLineBreak();
            if (info.Description != null)
            {
                infoWindow.AppendHighlight("Description: ");
                infoWindow.AppendNormal(info.Description);
                infoWindow.AppendLineBreak();
            }
            infoWindow.AppendHighlight("State: ");
            infoWindow.AppendNormal(bot.Plugins.GetState(info.InternalName).ToString());
            infoWindow.AppendLineBreak();
            if (info.Commands.Length > 0)
            {
                infoWindow.AppendHighlight("Commands: ");
                infoWindow.AppendLineBreak();
                bool isLoaded = bot.Plugins.IsLoaded(info.InternalName);
                lock (info)
                {
                    foreach (Command command in info.Commands)
                    {
                        infoWindow.AppendNormalStart();
                        infoWindow.AppendString("  " + Format.UppercaseFirst(command.CommandName));
                        if (!isLoaded && bot.Commands.Exists(command.CommandName))
                        {
                            infoWindow.AppendString(" (");
                            infoWindow.AppendColorString(RichTextWindow.ColorRed, "Conflict with " + bot.Plugins.GetName(bot.Commands.GetInternalName(command.CommandName)));
                            infoWindow.AppendString(")");
                        }
                        infoWindow.AppendColorEnd();
                        infoWindow.AppendLineBreak();
                    }
                }
            }
            if (info.Dependencies.Length > 0)
            {
                infoWindow.AppendHighlight("Dependencies: ");
                infoWindow.AppendLineBreak();
                lock (info)
                {
                    foreach (string dependency in info.Dependencies)
                    {
                        infoWindow.AppendNormalStart();
                        infoWindow.AppendString("  ");
                        if (bot.Plugins.Exists(dependency))
                        {
                            infoWindow.AppendString(bot.Plugins.GetName(dependency));
                            infoWindow.AppendString(" (");
                            if (bot.Plugins.IsLoaded(dependency))
                                infoWindow.AppendColorString(RichTextWindow.ColorGreen, "Loaded");
                            else
                                infoWindow.AppendColorString(RichTextWindow.ColorOrange, "Not Loaded");
                            infoWindow.AppendString(") [");
                            infoWindow.AppendCommand("Info", "plugins info " + dependency);
                            infoWindow.AppendString("]");
                        }
                        else
                        {
                            infoWindow.AppendString(dependency + " (");
                            infoWindow.AppendColorString(RichTextWindow.ColorRed, "Not Found");
                            infoWindow.AppendString(")");
                        }
                        infoWindow.AppendColorEnd();
                        infoWindow.AppendLineBreak();
                    }
                }
            }
            bot.SendReply(e, "Plugin Information »» " + infoWindow.ToString());
        }

        public void OnPluginsOverviewCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            string[] plugins = bot.Plugins.GetPlugins();
            foreach (string plugin in plugins)
            {
                if (bot.Plugins.GetState(plugin) == PluginState.Core) continue;

                PluginLoader info = bot.Plugins.GetLoader(plugin);
                window.AppendHeader(info.Name + " v" + info.Version);

                if (!string.IsNullOrEmpty(info.Description))
                {
                    window.AppendHighlight("Description: ");
                    window.AppendNormal(info.Description);
                    window.AppendLineBreak();
                }

                window.AppendHighlight("Author: ");
                window.AppendNormal(info.Author);
                window.AppendLineBreak();

                window.AppendHighlight("Commands: ");
                List<string> commands = new List<string>();
                foreach (Command command in info.Commands)
                    if (!command.IsAlias)
                        commands.Add(Format.UppercaseFirst(command.CommandName));
                window.AppendNormal(string.Join(", ", commands.ToArray()));

                window.AppendLineBreak(2);
            }
            bot.SendReply(e, "Plugins Overview »» ", window);
        }

        public void OnPluginsCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            RichTextWindow windowCore = new RichTextWindow(bot);
            RichTextWindow windowLoaded = new RichTextWindow(bot);
            RichTextWindow windowInstalled = new RichTextWindow(bot);
            RichTextWindow windowDisabled = new RichTextWindow(bot);

            window.AppendTitle("Plugins");
            window.AppendNormal("Plugins provide functionality and features for your bot. You can load different plugins based on your needs.");
            window.AppendLineBreak();
            window.AppendNormal("After loading plugins it's wise to open the ");
            window.AppendBotCommand("configuration interface", "configuration");
            window.AppendNormal(" to futher configure the loaded plugins.");
            window.AppendLineBreak();
            window.AppendNormal("If you're unsure about which plugin you need, ");
            window.AppendBotCommand("click here", "plugins overview");
            window.AppendNormal(" to get an overview of all available plugins.");
            window.AppendLineBreak(2);

            string[] plugins = bot.Plugins.GetPlugins();
            foreach (string plugin in plugins)
            {
                RichTextWindow tmpWindow;
                PluginState state = bot.Plugins.GetState(plugin);
                switch (state)
                {
                    case PluginState.Core:
                        tmpWindow = windowCore;
                        tmpWindow.AppendNormalStart();
                        break;
                    case PluginState.Loaded:
                        tmpWindow = windowLoaded;
                        tmpWindow.AppendNormalStart();
                        tmpWindow.AppendString("[");
                        tmpWindow.AppendBotCommand("Unload", "plugins unload " + plugin);
                        tmpWindow.AppendString("] ");
                        break;
                    case PluginState.Installed:
                        tmpWindow = windowInstalled;
                        tmpWindow.AppendNormalStart();
                        tmpWindow.AppendString("[");
                        tmpWindow.AppendBotCommand("Load", "plugins load " + plugin);
                        tmpWindow.AppendString("] [");
                        tmpWindow.AppendBotCommand("Uninstall", "plugins uninstall " + plugin);
                        tmpWindow.AppendString("] ");
                        break;
                    default:
                        tmpWindow = windowDisabled;
                        tmpWindow.AppendNormalStart();
                        tmpWindow.AppendString("[");
                        tmpWindow.AppendBotCommand("Install", "plugins install " + plugin);
                        tmpWindow.AppendString("] ");
                        break;
                }
                tmpWindow.AppendString("[");
                tmpWindow.AppendBotCommand("Info", "plugins info " + plugin);
                tmpWindow.AppendString("] ");
                tmpWindow.AppendColorEnd();

                PluginLoader info = bot.Plugins.GetLoader(plugin);
                tmpWindow.AppendHighlight(info.Name);
                tmpWindow.AppendLineBreak();
            }

            window.AppendHeader("Core Plugins");
            if (windowCore.Text != string.Empty)
                window.AppendRawString(windowCore.Text);
            else
            {
                window.AppendHighlight("None");
                window.AppendLineBreak();
            }
            window.AppendLineBreak();

            window.AppendHeader("Loaded");
            if (windowLoaded.Text != string.Empty)
                window.AppendRawString(windowLoaded.Text);
            else
            {
                window.AppendHighlight("None");
                window.AppendLineBreak();
            }
            window.AppendLineBreak();

            window.AppendHeader("Installed");
            if (windowInstalled.Text != string.Empty)
                window.AppendRawString(windowInstalled.Text);
            else
            {
                window.AppendHighlight("None");
                window.AppendLineBreak();
            }
            window.AppendLineBreak();

            window.AppendHeader("Disabled");
            if (windowDisabled.Text != string.Empty)
                window.AppendRawString(windowDisabled.Text);
            else
            {
                window.AppendHighlight("None");
                window.AppendLineBreak();
            }

            bot.SendReply(e, "Plugins »» ", window);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "plugins":
                    return "Displays the plugins interface.\nFrom this interface you can install/uninstall/load/unload plugins.\nThe interface also provides access to all kinds of information about plugins (even if they're not loaded).\n" +
                        "Usage: /tell " + bot.Character + " plugins";
            }
            return null;
        }
    }
}
