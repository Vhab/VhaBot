using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class MembersViewer : PluginBase
    {
        public MembersViewer()
        {
            this.Name = "Members Viewer";
            this.InternalName = "vhMembersViewer";
            this.Author = "Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("members", true, UserLevel.Leader),
                new Command("admins", true, UserLevel.Member),
                new Command("alts", true, UserLevel.Member),
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "members":
                    this.OnMembersCommand(bot, e);
                    break;
                case "admins":
                    this.OnAdminsCommand(bot, e);
                    break;
                case "alts":
                    this.OnAltsCommand(bot, e);
                    break;
            }
        }

        private void OnMembersCommand(BotShell bot, CommandArgs e)
        {
            SortedDictionary<string, UserLevel> members = bot.Users.GetUsers();
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Members");
            foreach (KeyValuePair<string, UserLevel> member in members)
            {
                window.AppendHighlight(member.Key);
                window.AppendNormal(" (" + member.Value + ")");
                window.AppendLineBreak();
            }
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, members.Count.ToString()) + " Members »» ", window);
        }

        private void OnAdminsCommand(BotShell bot, CommandArgs e)
        {
            SortedDictionary<string, UserLevel> members = bot.Users.GetUsers();
            List<string> chars = new List<string>();

            RichTextWindow window = new RichTextWindow(bot);
            RichTextWindow superadmins = new RichTextWindow(bot);
            RichTextWindow admins = new RichTextWindow(bot);
            RichTextWindow leaders = new RichTextWindow(bot);

            int adminCount = 0;
            foreach (KeyValuePair<string, UserLevel> member in members)
            {
                RichTextWindow tmp = null;
                switch (member.Value)
                {
                    case UserLevel.SuperAdmin:
                        tmp = superadmins;
                        break;
                    case UserLevel.Admin:
                        tmp = admins;
                        break;
                    case UserLevel.Leader:
                        tmp = leaders;
                        break;
                    default:
                        continue;
                }
                chars.Add(member.Key.ToLower());
                tmp.AppendHighlightStart();
                tmp.AppendString(member.Key);
                tmp.AppendString(" is ");
                if (bot.FriendList.IsOnline(member.Key) == OnlineState.Online)
                    tmp.AppendColorString(RichTextWindow.ColorGreen, "Online");
                else
                    tmp.AppendColorString(RichTextWindow.ColorRed, "Offline");
                tmp.AppendColorEnd();
                tmp.AppendLineBreak();
                if (!bot.FriendList.IsFriend(member.Key))
                    bot.FriendList.Add(this.InternalName, member.Key);
                foreach (string alt in bot.Users.GetAlts(member.Key))
                {
                    chars.Add(alt.ToLower());
                    tmp.AppendNormalStart();
                    tmp.AppendString("- " + Format.UppercaseFirst(alt));
                    tmp.AppendString(" is ");
                    if (bot.FriendList.IsOnline(alt) == OnlineState.Online)
                        tmp.AppendColorString(RichTextWindow.ColorGreen, "Online");
                    else
                        tmp.AppendColorString(RichTextWindow.ColorRed, "Offline");
                    tmp.AppendColorEnd();
                    tmp.AppendLineBreak();
                    if (!bot.FriendList.IsFriend(alt))
                        bot.FriendList.Add(this.InternalName, alt);
                }
                adminCount++;
            }
            foreach (string friend in bot.FriendList.List(this.InternalName))
                if (!chars.Contains(friend.ToLower()))
                    bot.FriendList.Remove(this.InternalName, friend);

            window.AppendTitle("Super Admins");
            window.AppendRawString(superadmins.Text);
            window.AppendLineBreak();

            window.AppendHeader("Admins");
            window.AppendRawString(admins.Text);
            window.AppendLineBreak();

            window.AppendHeader("Leaders");
            window.AppendRawString(leaders.Text);
            window.AppendLineBreak();

            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, adminCount.ToString()) + " Admins »» ", window);
        }

        private void OnAltsCommand(BotShell bot, CommandArgs e)
        {
            string username = "";
            if (e.Args.Length < 1)
            //{
            //     bot.SendReply(e, "Correct Usage: " + e.Command + " [username]");
            //     return;
            //}
                 username = e.Sender;
            else
                 username = e.Args[0];
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            string main = bot.Users.GetMain(username);
            RichTextWindow window = this.GetAltsWindow(bot, main);
            if (window == null)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, main) + " doesn't have any alts");
                return;
            }
            bot.SendReply(e, main + "'s characters »» ", window);
        }

        public RichTextWindow GetAltsWindow(BotShell bot, string user)
        {
            string main = bot.Users.GetMain(user);
            string[] alts = bot.Users.GetAlts(main);
            if (alts.Length < 1)
                return null;

            RichTextWindow window = new RichTextWindow(bot);
            List<string> characters = new List<string>();
            characters.Add(main);
            foreach (string alt in alts)
                characters.Add(alt);

            window.AppendTitle();
            foreach (string character in characters)
            {
                window.AppendHeader(Format.UppercaseFirst(character));
                window.AppendHighlight("Status: ");
                if (bot.FriendList.IsOnline(character) == OnlineState.Online)
                    window.AppendColorString(RichTextWindow.ColorGreen, "Online");
                else
                {
                    window.AppendColorString(RichTextWindow.ColorRed, "Offline");
                    window.AppendLineBreak();
                    window.AppendHighlight("Last Seen: ");
                    Int64 seen = bot.FriendList.Seen(character);
                    if (seen > 1)
                        window.AppendNormal(Format.DateTime(seen, FormatStyle.Large) + " GMT");
                    else
                        window.AppendNormal("N/A");
                }
                window.AppendLineBreak();
                window.AppendHighlight("User Level: ");
                window.AppendNormal(bot.Users.GetUser(character).ToString());
                window.AppendLineBreak();

                WhoisResult whois = XML.GetWhois(character, bot.Dimension);
                if (whois != null && whois.Success)
                {
                    window.AppendHighlight("Profession: ");
                    window.AppendNormal(whois.Stats.Profession);
                    window.AppendLineBreak();
                    window.AppendHighlight("Level: ");
                    window.AppendNormal(whois.Stats.Level.ToString());
                    if (whois.Stats.DefenderLevel > 0)
                        window.AppendNormal(" / " + whois.Stats.DefenderLevel);
                    window.AppendLineBreak();
                    if (whois.InOrganization)
                    {
                        window.AppendHighlight("Organization: ");
                        window.AppendNormal(whois.Organization.Name);
                        window.AppendLineBreak();
                        window.AppendHighlight("Rank: ");
                        window.AppendNormal(whois.Organization.Rank);
                        window.AppendLineBreak();
                    }
                }
                window.AppendLineBreak();
            }
            return window;
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "admins":
                    return "Show the list of current leaders, administrators and their alts, as well as their online status.\n" +
                        "Usage: /tell " + bot.Character + " admins";
                case "members":
                    return "Shows the list of all the current members of the bot\n" +
                        "Usage: /tell " + bot.Character + " members";
                case "alts":
                    return "Shows all [username]'s alternative characters currently registred on the bot.\n" +
                        "Usage: /tell " + bot.Character + " alts [username]";
            }
            return null;
        }
    }
}
