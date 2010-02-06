using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class MembersManager : PluginBase
    {
        public MembersManager()
        {
            this.Name = "Members Manager";
            this.InternalName = "vhMembersManager";
            this.Author = "Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("members add", true, UserLevel.Admin),
                new Command("members remove", true, UserLevel.Admin),
                new Command("members promote", true, UserLevel.Admin),
                new Command("members demote", true, UserLevel.Admin),
                new Command("members clear", true, UserLevel.SuperAdmin),
                new Command("alts add", true, UserLevel.Member),
                new Command("alts remove", true, UserLevel.Member),
                new Command("alts admin add", true, UserLevel.Admin),
                new Command("alts admin remove", true, UserLevel.Admin)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "members clear":
                    if (e.Args.Length == 0 || e.Args[0] != "confirm")
                    {
                        bot.SendReply(e, "This command will remove ALL members. If you wish to continue use: /tell " + bot.Character + " members clear confirm");
                        break;
                    }
                    bot.Users.RemoveAll();
                    bot.SendReply(e, "All members have been removed");
                    break;
                case "members add":
                case "members remove":
                case "members promote":
                case "members demote":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: " + e.Command + " [username]");
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    string username = Format.UppercaseFirst(e.Args[0]);
                    switch (e.Command)
                    {
                        case "members add":
                            if (bot.Users.UserExists(username, bot.GetUserID(username)))
                            {
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " is already a member of this bot");
                                break;
                            }
                            if (bot.Users.IsAlt(username))
                            {
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " is register as alt on this bot");
                                break;
                            }
                            if (bot.Users.AddUser(username, UserLevel.Member, e.Sender))
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " has been added to this bot");
                            else
                                bot.SendReply(e, "An unknown error has occurred");
                            break;
                        case "members remove":
                            if (bot.Users.IsAlt(username))
                            {
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " is alt and can't be removed using this command");
                                break;
                            }
                            if (!bot.Users.UserExists(username))
                            {
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " is not a member of this bot");
                                break;
                            }
                            bot.Users.RemoveUser(username);
                            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " has been removed from this bot");
                            break;
                        case "members promote":
                            if (!bot.Users.UserExists(username))
                            {
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " is not a member of this bot");
                                break;
                            }
                            UserLevel promotelevel = bot.Users.GetUser(username);
                            if (promotelevel >= bot.Users.GetUser(e.Sender))
                            {
                                bot.SendReply(e, "You can't promote a user to a higher rank than your own rank!");
                                break;
                            }
                            try
                            {
                                promotelevel = (UserLevel)((int)promotelevel * 2);
                                if (promotelevel != UserLevel.Disabled)
                                {
                                    if (bot.Users.SetUser(username, promotelevel))
                                    {
                                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " has been promoted to " + HTML.CreateColorString(bot.ColorHeaderHex, promotelevel.ToString()));
                                        break;
                                    }
                                }
                            }
                            catch { }
                            bot.SendReply(e, "An unknown error has occurred");
                            break;
                        case "members demote":
                            if (!bot.Users.UserExists(username))
                            {
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " is not a member of this bot");
                                break;
                            }
                            UserLevel demotelevel = bot.Users.GetUser(username);
                            if (demotelevel > bot.Users.GetUser(e.Sender))
                            {
                                bot.SendReply(e, "You can't demote a user that outranks you!");
                                break;
                            }
                            if (bot.Admin.ToLower() == username.ToLower())
                            {
                                bot.SendReply(e, "You can't demote the main bot administrator!");
                                bot.SendPrivateMessage(bot.Admin, bot.ColorHeader + e.Sender + bot.ColorHighlight + " attempted to demote you!");
                                break;
                            }
                            try
                            {
                                demotelevel = (UserLevel)((int)demotelevel / 2);
                                if (demotelevel != UserLevel.Guest)
                                {
                                    if (bot.Users.SetUser(username, demotelevel))
                                    {
                                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(username)) + " has been demoted to " + HTML.CreateColorString(bot.ColorHeaderHex, demotelevel.ToString()));
                                        break;
                                    }
                                }
                                else
                                {
                                    bot.SendReply(e, "You can't demote a user to a rank below member!");
                                    break;
                                }
                            }
                            catch { }
                            bot.SendReply(e, "An unknown error has occurred");
                            break;
                    }
                    break;
                case "alts add":
                    this.OnAltsAddCommand(bot, e);
                    break;
                case "alts remove":
                    this.OnAltsRemoveCommand(bot, e);
                    break;
                case "alts admin add":
                    this.OnAltsAdminAddCommand(bot, e);
                    break;
                case "alts admin remove":
                    this.OnAltsAdminRemoveCommand(bot, e);
                    break;
            }
        }

        private void OnAltsAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: alts add [alt]");
                return;
            }
            string alt = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(alt) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, alt));
                return;
            }
            if (bot.Users.IsAlt(e.Sender))
            {
                bot.SendReply(e, "This command can only be used by your main: " + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(bot.Users.GetMain(e.Sender))));
                return;
            }
            if (bot.Users.IsAlt(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " already has been added as alt of " + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(bot.Users.GetMain(alt))));
                return;
            }
            if (bot.Users.GetUser(alt) > bot.Users.GetUser(e.Sender))
            {
                bot.SendReply(e, "You can't add alts that outrank you!");
                return;
            }
            if (bot.Users.GetAlts(alt).Length > 0)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is a main with registered alts and can't be added as your alt");
                return;
            }
            if (bot.Users.GetUser(alt) > UserLevel.Guest)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " was a member of this bot and has been removed prior to being added as alt");
                bot.Users.RemoveUser(alt);
            }
            bot.Users.AddAlt(e.Sender, alt);
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " has been added as your alt");
        }

        private void OnAltsRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: alts remove [alt]");
                return;
            }
            string alt = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(alt) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, alt));
                return;
            }
            if (!bot.Users.IsAlt(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is not an alt");
                return;
            }
            if (bot.Users.GetMain(alt) != bot.Users.GetMain(e.Sender))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is not your alt");
                return;
            }
            bot.Users.RemoveAlt(alt);
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " has been removed from this bot");
        }

        private void OnAltsAdminAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: alts admin add [main] [alt]");
                return;
            }
            string main = Format.UppercaseFirst(e.Args[0]);
            string alt = Format.UppercaseFirst(e.Args[1]);
            if (bot.GetUserID(main) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, main));
                return;
            }
            if (bot.GetUserID(alt) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, alt));
                return;
            }
            if (bot.Users.IsAlt(main))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, main) + " is an alt of " + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(bot.Users.GetMain(main))));
                return;
            }
            if (!bot.Users.UserExists(main))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, main) + " is not a member of this bot");
                return;
            }
            if (bot.Users.IsAlt(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " already has been added as alt of " + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(bot.Users.GetMain(alt))));
                return;
            }
            if (bot.Users.GetAlts(alt).Length > 0)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is a main and has alts. You can't add a user with alts as alt!");
                return;
            }
            if (bot.Users.GetUser(main) > bot.Users.GetUser(e.Sender))
            {
                bot.SendReply(e, "You can't add alts to a user that outranks you!");
                return;
            }
            if (bot.Users.GetUser(main) < bot.Users.GetUser(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " outranks " + HTML.CreateColorString(bot.ColorHeaderHex, main) + " and can't be added as alt");
                return;
            }
            if (bot.Users.GetUser(alt) > UserLevel.Guest)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " was a member of this bot and has been removed prior to being added as alt");
                bot.Users.RemoveUser(alt);
            }
            bot.Users.AddAlt(main, alt);
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " has been added as alt of " + HTML.CreateColorString(bot.ColorHeaderHex, main));
        }

        private void OnAltsAdminRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: alts admin remove [alt]");
                return;
            }
            string alt = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(alt) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, alt));
                return;
            }
            if (!bot.Users.IsAlt(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is not an alt");
            }
            if (bot.Users.GetUser(bot.Users.GetMain(alt)) > bot.Users.GetUser(e.Sender))
            {
                bot.SendReply(e, "You can't remove alts from a user that outranks you!");
                return;
            }
            bot.Users.RemoveAlt(alt);
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " has been removed from this bot");
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "members add":
                    return "Allows you to add a new member to the bot.\nThis command isn't intended to add an alt, use the 'alts add' function for that.\n" +
                        "Usage: /tell " + bot.Character + " members add [username]";
                case "members remove":
                    return "Allows you to permanently remove a member from the bot.\nThis command will also remove any alts the member had.\n" +
                        "Usage: /tell " + bot.Character + " members remove [username]";
                case "members promote":
                    return "Allows you to promote an existing member to a higher rank.\nYou can't promote anyone to a higher rank than your own.\n" +
                        "Usage: /tell " + bot.Character + " members promote [username]";
                case "members demote":
                    return "Allows you to demote an existing member to a lower rank.\nYou can't demote anyone with a higher rank than your own.\n" +
                        "Usage: /tell " + bot.Character + " members demote [username]";
                case "alts add":
                    return "Allows you to add [altname] as an alternative character to your account.\nAlts will inherit the user rights of your main.\n" +
                        "Usage: /tell " + bot.Character + " alts add [altname]";
                case "alts remove":
                    return "Allows you to remove [altname] from the bot.\n" +
                        "Usage: /tell " + bot.Character + " alts remove [altname]";
                case "alts admin add":
                    return "Allows you to add [altname] as an alternative character to [main]'s account.\nAlts will inherit the user rights of their main.\n" +
                        "Usage: /tell " + bot.Character + " alts admin add [main] [altname]";
                case "alts admin remove":
                    return "Allows you to remove [altname] from the bot.\n" +
                        "Usage: /tell " + bot.Character + " alts admin remove [altname]";
            }
            return null;
        }
    }
}
