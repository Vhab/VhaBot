using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class XmlService : PluginBase
    {
        private string OrgWebsite = "http://people.anarchy-online.com/org/stats/d/{0}/name/{1}";
        private string CharWebsite = "http://people.anarchy-online.com/character/bio/d/{0}/name/{1}";
        private string CharAunoWebsite = "http://auno.org/ao/char.php?dimension={0}&name={1}";
        private string OrgAunoWebsite = "http://auno.org/ao/char.php?dimension={0}&guild={1}";
        private string TimeoutError = "(This may be because the server was too slow to respond or is currently unavailable)";

        public XmlService()
        {
            this.Name = "XML Lookup Services";
            this.InternalName = "vhXmlService";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("whois", true, UserLevel.Guest),
                new Command("whoisall", true, UserLevel.Guest),
                new Command("history", true, UserLevel.Guest),
                new Command("organization", true, UserLevel.Guest),
                new Command("org", "organization"),
                new Command("server", true, UserLevel.Guest),
                new Command("is", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot) { }

        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "whois":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: whois [username]");
                        return;
                    }
                    OnWhoisCommand(bot, e, bot.Dimension, false);
                    break;
                case "whoisall":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: whoisall [username]");
                        return;
                    }
                    OnWhoisCommand(bot, e, AoLib.Net.Server.Atlantean, true);
                    OnWhoisCommand(bot, e, AoLib.Net.Server.Rimor, true);
                    OnWhoisCommand(bot, e, AoLib.Net.Server.DieNeueWelt, true);
                    break;
                case "history":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: history [username]");
                        return;
                    }
                    OnHistoryCommand(bot, e);
                    break;
                case "organization":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: organization [username]");
                        return;
                    }
                    OnOrganizationCommand(bot, e);
                    break;
                case "server":
                    this.OnServerCommand(bot, e);
                    break;
                case "is":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: is [username]");
                        return;
                    }
                    this.OnIsCommand(bot, e);
                    break;
            }
        }

        private void OnWhoisCommand(BotShell bot, CommandArgs e, AoLib.Net.Server dimension, Boolean showDimension)
        {
            if (!showDimension && dimension == AoLib.Net.Server.Test)
            {
                bot.SendReply(e, "The whois command is not available on the test server. Please use 'whoisall' instead");
                return;
            }
            if (dimension == bot.Dimension)
            {
                if (bot.GetUserID(e.Args[0]) < 100)
                {
                    bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                    return;
                }
            }
            WhoisResult whois = XML.GetWhois(e.Args[0].ToLower(), dimension);
            if (whois == null || !whois.Success)
            {
                string error = string.Empty;
                if (showDimension)
                    error += HTML.CreateColorString(bot.ColorHeaderHex, dimension.ToString() + ": ");
                error += "Unable to gather information on that user " + this.TimeoutError;
                bot.SendReply(e, error);
                return;
            }

            RichTextWindow window = new RichTextWindow(bot);
            StringBuilder builder = new StringBuilder();

            if (showDimension)
                builder.Append(HTML.CreateColorString(bot.ColorHeaderHex, dimension.ToString() + ": "));

            builder.Append(String.Format("{0} (Level {1}", whois.Name.Nickname, whois.Stats.Level));
            window.AppendTitle(whois.Name.ToString());

            window.AppendHighlight("Breed: ");
            window.AppendNormal(whois.Stats.Breed);
            window.AppendLineBreak();

            window.AppendHighlight("Gender: ");
            window.AppendNormal(whois.Stats.Gender);
            window.AppendLineBreak();

            window.AppendHighlight("Profession: ");
            window.AppendNormal(whois.Stats.Profession);
            window.AppendLineBreak();

            window.AppendHighlight("Level: ");
            window.AppendNormal(whois.Stats.Level.ToString());
            window.AppendLineBreak();

            if (whois.Stats.DefenderLevel != 0)
            {
                window.AppendHighlight("Defender Rank: ");
                window.AppendNormal(String.Format("{0} ({1})", whois.Stats.DefenderRank, whois.Stats.DefenderLevel));
                window.AppendLineBreak();

                builder.Append(" / Defender Rank " + whois.Stats.DefenderLevel);
            }

            if (dimension == bot.Dimension)
            {
                window.AppendHighlight("Status: ");
                UInt32 userid = bot.GetUserID(whois.Name.Nickname);
                OnlineState state = bot.FriendList.IsOnline(userid);
                switch (state)
                {
                    case OnlineState.Online:
                        window.AppendColorString(RichTextWindow.ColorGreen, "Online");
                        break;
                    case OnlineState.Offline:
                        window.AppendColorString(RichTextWindow.ColorRed, "Offline");
                        Int64 seen = bot.FriendList.Seen(whois.Name.Nickname);
                        if (seen > 0)
                        {
                            window.AppendLineBreak();
                            window.AppendHighlight("Last Seen: ");
                            window.AppendNormal(Format.DateTime(seen, FormatStyle.Compact));
                        }
                        break;
                    default:
                        window.AppendColorString(RichTextWindow.ColorOrange, "Unknown");
                        break;
                }
                window.AppendLineBreak();
            }

            builder.Append(")");
            builder.Append(String.Format(" is a {0} {1}", whois.Stats.Faction, whois.Stats.Profession));

            window.AppendHighlight("Alignment: ");
            window.AppendNormal(whois.Stats.Faction);
            window.AppendLineBreak();

            if (whois.InOrganization)
            {
                window.AppendHighlight("Organization: ");
                window.AppendNormal(whois.Organization.Name);
                window.AppendLineBreak();

                window.AppendHighlight("Organization Rank: ");
                window.AppendNormal(whois.Organization.Rank);
                window.AppendLineBreak();

                builder.AppendFormat(", {0} of {1}", whois.Organization.Rank, whois.Organization.Name);
            }

            window.AppendHighlight("Last Updated: ");
            window.AppendNormal(whois.LastUpdated);
            window.AppendLineBreak(2);

            if (dimension == bot.Dimension)
            {
                window.AppendHeader("Options");
                window.AppendCommand("Add to Friendlist", "/cc addbuddy " + whois.Name.Nickname);
                window.AppendLineBreak();
                window.AppendCommand("Remove from Friendlist", "/cc rembuddy " + whois.Name.Nickname);
                window.AppendLineBreak();
                window.AppendBotCommand("Character History", "history " + whois.Name.Nickname);
                window.AppendLineBreak();
                if (whois.Organization != null && whois.Organization.Name != null)
                {
                    window.AppendBotCommand("Organization Information", "organization " + whois.Name.Nickname);
                    window.AppendLineBreak();
                }
                window.AppendLineBreak();
            }

            window.AppendHeader("Links");
            window.AppendCommand("Official Character Website", "/start " + string.Format(this.CharWebsite, (int)dimension, whois.Name.Nickname));
            window.AppendLineBreak();
            if (whois.Organization != null && whois.Organization.Name != null)
            {
                window.AppendCommand("Official Organization Website", "/start " + string.Format(this.OrgWebsite, (int)dimension, whois.Organization.ID));
                window.AppendLineBreak();
            }
            window.AppendCommand("Auno's Character Website", "/start " + string.Format(this.CharAunoWebsite, (int)dimension, whois.Name.Nickname));
            window.AppendLineBreak();
            if (whois.Organization != null && whois.Organization.Name != null)
            {
                window.AppendCommand("Auno's Organization Website", "/start " + string.Format(this.OrgAunoWebsite, (int)dimension, whois.Organization.Name.Replace(' ', '+')));
                window.AppendLineBreak();
            }

            builder.Append(" »» " + window.ToString("More Information"));
            bot.SendReply(e, builder.ToString());
        }

        private void OnHistoryCommand(BotShell bot, CommandArgs e)
        {
            if (bot.GetUserID(e.Args[0]) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                return;
            }

            bot.SendReply(e, Format.UppercaseFirst(e.Args[0]) + "'s History »» Gathering Data...");

            HistoryResult history = XML.GetHistory(e.Args[0].ToLower(), bot.Dimension);
            if (history == null || history.Items == null)
            {
                bot.SendReply(e, "Unable to gather information on that user " + this.TimeoutError);
                return;
            }

            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Auno.org Player History");
            window.AppendHighlight("Character: ");
            window.AppendNormal(Format.UppercaseFirst(e.Args[0]));
            window.AppendLineBreak();
            window.AppendHighlight("Entries: ");
            window.AppendNormal(history.Items.Length.ToString());
            window.AppendLineBreak();
            window.AppendHighlight("URL: ");
            window.AppendNormal(string.Format(this.CharAunoWebsite, (int)bot.Dimension, e.Args[0].ToLower()));
            window.AppendLineBreak(2);

            window.AppendHighlightStart();
            window.AppendString("Date             ");
            window.AppendString("LVL   ");
            window.AppendString("DR   ");
            window.AppendColorString("000000", "'");
            window.AppendString("Faction   ");
            window.AppendString("Organization");
            window.AppendColorEnd();
            window.AppendLineBreak();

            foreach (HistoryResult_Entry entry in history.Items)
            {
                window.AppendNormalStart();
                window.AppendString(entry.Date);
                window.AppendHighlight(" | ");
                window.AppendString(entry.Level.ToString("000"));
                window.AppendHighlight(" | ");
                window.AppendString(entry.DefenderLevel.ToString("00"));
                window.AppendHighlight(" | ");
                switch (entry.Faction.ToLower())
                {
                    case "clan":
                        window.AppendString("Clan    ");
                        break;
                    case "omni":
                        window.AppendString("Omni   ");
                        break;
                    case "neutral":
                        window.AppendString("Neutral");
                        break;
                    default:
                        window.AppendString("Unknown");
                        break;
                }
                window.AppendHighlight(" | ");
                if (entry.Organization != null && entry.Organization != String.Empty)
                {
                    window.AppendString(entry.Rank);
                    window.AppendString(" of ");
                    window.AppendString(entry.Organization);
                }
                window.AppendColorEnd();
                window.AppendLineBreak();
            }
            bot.SendReply(e, Format.UppercaseFirst(e.Args[0]) + "'s History »» ", window);
        }

        private void OnOrganizationCommand(BotShell bot, CommandArgs e)
        {
            if (bot.GetUserID(e.Args[0]) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                return;
            }

            bot.SendReply(e, "Organization »» Gathering Data...");

            WhoisResult whoisResult = XML.GetWhois(e.Args[0], bot.Dimension);
            if (whoisResult != null && whoisResult.Organization != null)
            {
                OrganizationResult organization = XML.GetOrganization(whoisResult.Organization.ID, bot.Dimension);
                if (organization != null && organization.Members != null)
                {
                    RichTextWindow window = new RichTextWindow(bot);
                    RichTextWindow membersWindow = new RichTextWindow(bot);

                    window.AppendTitle(organization.Name);

                    window.AppendHighlight("Leader: ");
                    window.AppendNormal(organization.Leader.Nickname);
                    window.AppendLineBreak();

                    window.AppendHighlight("Alignment: ");
                    window.AppendNormal(organization.Faction);
                    window.AppendLineBreak();

                    window.AppendHighlight("Members: ");
                    window.AppendNormal(organization.Members.Items.Length.ToString());
                    window.AppendLineBreak();

                    SortedDictionary<string, int> profs = new SortedDictionary<string, int>();
                    SortedDictionary<string, int> breeds = new SortedDictionary<string, int>();
                    SortedDictionary<string, int> genders = new SortedDictionary<string, int>();

                    membersWindow.AppendHeader("Members");

                    foreach (OrganizationMember member in organization.Members.Items)
                    {
                        if (!profs.ContainsKey(member.Profession))
                            profs.Add(member.Profession, 0);
                        profs[member.Profession]++;

                        if (!breeds.ContainsKey(member.Breed))
                            breeds.Add(member.Breed, 0);
                        breeds[member.Breed]++;

                        if (!genders.ContainsKey(member.Gender))
                            genders.Add(member.Gender, 0);
                        genders[member.Gender]++;

                        membersWindow.AppendHighlight(member.Nickname);
                        membersWindow.AppendNormal(string.Format(" {0} (L {1} / DR {2}) {3} {4}", member.Rank, member.Level, member.DefenderLevel, member.Breed, member.Profession));
                        membersWindow.AppendLineBreak();
                    }

                    string stats;
                    char[] trimchars = new char[] { ' ', ',' };

                    window.AppendHighlight("Genders: ");
                    stats = string.Empty;
                    foreach (KeyValuePair<string, int> kvp in genders)
                    {
                        stats += kvp.Value + " " + kvp.Key + ", ";
                    }
                    window.AppendNormal(stats.Trim(trimchars));
                    window.AppendLineBreak();

                    window.AppendHighlight("Breeds: ");
                    stats = string.Empty;
                    foreach (KeyValuePair<string, int> kvp in breeds)
                    {
                        stats += kvp.Value + " " + kvp.Key + ", ";
                    }
                    window.AppendNormal(stats.Trim(trimchars));
                    window.AppendLineBreak();

                    window.AppendHighlight("Professions: ");
                    stats = string.Empty;
                    foreach (KeyValuePair<string, int> kvp in profs)
                    {
                        stats += kvp.Value + " " + kvp.Key + ", ";
                    }
                    window.AppendNormal(stats.Trim(trimchars));
                    window.AppendLineBreak();

                    window.AppendHighlight("ID: ");
                    window.AppendNormal(organization.ID.ToString());
                    window.AppendLineBreak();

                    window.AppendHighlight("Last Updated: ");
                    window.AppendNormal(organization.LastUpdated);
                    window.AppendLineBreak(2);

                    window.AppendRawString(membersWindow.Text);

                    bot.SendReply(e, organization.Name + " »» ", window);
                    return;
                }
            }
            bot.SendReply(e, "Unable to gather information on that organization " + this.TimeoutError);
        }

        private void OnServerCommand(BotShell bot, CommandArgs e)
        {
            bot.SendReply(e, "Server Status »» Gathering Data...");

            ServerStatusResult server = XML.GetServerStatus();
            if (server == null || server.Dimensions == null)
            {
                bot.SendReply(e, "Unable to gather server information " + this.TimeoutError);
                return;
            }
            ServerStatusResult_Dimension dimension = server.GetDimension(bot.Dimension);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Server Information");

            window.AppendHighlight("Server Manager: ");
            if (dimension.ServerManager.Online)
                window.AppendColorString(RichTextWindow.ColorGreen, "Online");
            else
                window.AppendColorString(RichTextWindow.ColorRed, "Offline");
            window.AppendLineBreak();

            window.AppendHighlight("Client Manager: ");
            if (dimension.ClientManager.Online)
                window.AppendColorString(RichTextWindow.ColorGreen, "Online");
            else
                window.AppendColorString(RichTextWindow.ColorRed, "Offline");
            window.AppendLineBreak();

            window.AppendHighlight("Chat Server: ");
            if (dimension.ChatServer.Online)
                window.AppendColorString(RichTextWindow.ColorGreen, "Online");
            else
                window.AppendColorString(RichTextWindow.ColorRed, "Offline");
            window.AppendLineBreak(2);

            window.AppendHeader("Alignment");
            window.AppendHighlight("Clan: ");
            window.AppendNormal(dimension.Distribution.Clan.Percent + "%");
            window.AppendLineBreak();
            window.AppendHighlight("Neutral: ");
            window.AppendNormal(dimension.Distribution.Neutral.Percent + "%");
            window.AppendLineBreak();
            window.AppendHighlight("Omni: ");
            window.AppendNormal(dimension.Distribution.Omni.Percent + "%");
            window.AppendLineBreak(2);

            foreach (ServerStatusResult_Playfield pf in dimension.Playfields)
            {
                bool skip = false;
                foreach (string arg in e.Args)
                    if (!pf.Name.ToLower().Contains(arg.ToLower()))
                        skip = true;

                if (skip)
                    continue;

                switch (pf.Status)
                {
                    case PlayfieldStatus.Online:
                        window.AppendImage("GFX_GUI_FRIENDLIST_STATUS_GREEN");
                        break;
                    default:
                        window.AppendImage("GFX_GUI_FRIENDLIST_STATUS_RED");
                        break;
                }
                window.AppendNormalStart();
                window.AppendString(" ");
                window.AppendColorStart(RichTextWindow.ColorGreen);
                double players = 0;
                while (players <= pf.Players && players <= 8 && pf.Players != 0)
                {
                    players += 0.5;
                    window.AppendString("l");
                }
                window.AppendColorEnd();
                while (players <= 8)
                {
                    players += 0.5;
                    window.AppendString("l");
                }
                window.AppendString(" ");
                window.AppendColorEnd();

                window.AppendHighlight(pf.Name);
                window.AppendNormal(string.Format(" (ID: {0} Players: {1}%)", pf.ID, pf.Players));
                window.AppendLineBreak();
            }
            bot.SendReply(e, "Server Status »» ", window);
        }

        private void OnIsCommand(BotShell bot, CommandArgs e)
        {
            foreach (string username in e.Args)
            {
                UInt32 userid = bot.GetUserID(username);
                OnlineState state = bot.FriendList.IsOnline(userid);
                if (state == OnlineState.Timeout)
                {
                    bot.SendReply(e, "Request timed out. Please try again later");
                    return;
                }
                if (state == OnlineState.Unknown)
                {
                    bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                    return;
                }
                string append = HTML.CreateColorString(RichTextWindow.ColorGreen, "Online");
                if (state == OnlineState.Offline)
                {
                    append = HTML.CreateColorString(RichTextWindow.ColorRed, "Offline");
                    Int64 seen = bot.FriendList.Seen(username);
                    if (seen > 1)
                        append += " and was last seen online at " + HTML.CreateColorString(bot.ColorHeaderHex, Format.DateTime(seen, FormatStyle.Large) + " GMT");
                }
                bot.SendReply(e, String.Format("{0} is currently {1}", Format.UppercaseFirst(username), append));
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "whois":
                    return "Gathers and displays information about a user. Information like Level, Breed and Profession\n" +
                        "Usage: /tell " + bot.Character + " whois [username]";
                case "whoisall":
                    return "Gathers and displays information about users matching the specified username on all dimensions.\n" +
                        "Usage: /tell " + bot.Character + " whois [username]";
                case "history":
                    return "Gathers and displays the history of a user.\n" +
                        "Usage: /tell " + bot.Character + " history [username]";
                case "organization":
                    return "Displays information about the organization of the specified user.\n" +
                        "Usage: /tell " + bot.Character + " organization [username]";
                case "server":
                    return "Displays the current status of this dimension's server.\n" +
                        "Usage: /tell " + bot.Character + " server";
                case "is":
                    return "Displays the current online status of the specified user.\n" +
                        "Usage: /tell " + bot.Character + " is [username]";
            }
            return null;
        }
    }
}