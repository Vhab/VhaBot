using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;
using VhaBot;
using VhaBot.ShellModules;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class VhanetMembersHost : PluginBase
    {
        private BotShell _bot;
        private VhanetDatabase _database;

        public VhanetMembersHost()
        {
            this.Name = "Vhanet :: Central Members Host";
            this.InternalName = "VhanetMembersHost";
            this.Version = 100;
            this.Author = "Vhab";
            this.Dependencies = new string[] { "VhanetDatabase" };
            this.DefaultState = PluginState.Disabled;
            this.Description = "Provides a central members management system";
            this.Commands = new Command[] {
                new Command("members add", true, UserLevel.Admin),
                new Command("members remove", true, UserLevel.Admin),
                new Command("members promote", true, UserLevel.Guest),
                new Command("members demote", true, UserLevel.Guest),
                new Command("members enable", true, UserLevel.Guest),
                new Command("members disable", true, UserLevel.Guest),
                new Command("members last", true, UserLevel.Guest),
                new Command("account", true, UserLevel.Guest),
                new Command("alts add", true, UserLevel.Admin),
                new Command("alts remove", true, UserLevel.Admin),
                new Command("password", true, UserLevel.Guest),
                new Command("bots", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
            try { this._database = (VhanetDatabase)bot.Plugins.GetPlugin("VhanetDatabase"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Database' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");

            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS members (username VARCHAR(14) NOT NULL, userID BIGINT NOT NULL, addedBy VARCHAR(14) NOT NULL, addedOn INT NOT NULL, password VARCHAR(32) NULL, online INT(1) NOT NULL DEFAULT '0', UNIQUE (username))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS members_levels (username VARCHAR(14) NOT NULL, `group` VARCHAR(50) NOT NULL, userLevel INT NOT NULL, UNIQUE (username, `group`))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS members_access (username VARCHAR(14) NOT NULL, `group` VARCHAR(50) NOT NULL, UNIQUE (username, `group`))");
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS alts (altname VARCHAR(14) NOT NULL, altID BIGINT NOT NULL, username VARCHAR(14) NOT NULL, addedBy VARCHAR(14) NOT NULL, addedOn INT NOT NULL, online INT(1) NOT NULL DEFAULT '0', UNIQUE (altname))");
        }

        public bool CheckDatabase(BotShell bot, CommandArgs e)
        {
            if (this._database.Connected)
                return true;
            bot.SendReply(e, "Unable to connect to the database. Please try again later");
            return false;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (!this.CheckDatabase(bot, e))
                return;

            // Commands that use the default auth system
            switch (e.Command)
            {
                case "members add":
                    this.OnMembersAddCommand(bot, e);
                    return;
                case "members remove":
                    this.OnMembersRemoveCommand(bot, e);
                    return;
                case "alts add":
                    this.OnAltsAddCommand(bot, e);
                    return;
                case "alts remove":
                    this.OnAltsRemoveCommand(bot, e);
                    return;
                case "password":
                    this.OnPasswordCommand(bot, e);
                    return;
                case "bots":
                    this.OnBotsCommand(bot, e);
                    return;
            }

            // Commands using a custom system
            if (this.GetHighestUserLevel(e.Sender) < UserLevel.Admin && e.Sender != bot.Admin)
            {
                e.Authorized = false;
                return;
            }
            switch (e.Command)
            {
                case "members last":
                    this.OnMembersLastCommand(bot, e);
                    break;
                case "members promote":
                    this.OnMembersPromoteCommand(bot, e);
                    break;
                case "members demote":
                    this.OnMembersDemoteCommand(bot, e);
                    break;
                case "account":
                    this.OnAccountCommand(bot, e);
                    break;
                case "members enable":
                    this.OnMembersEnableCommand(bot, e);
                    break;
                case "members disable":
                    this.OnMembersDisableCommand(bot, e);
                    break;
            }
        }

        public bool IsAllowedMembersManagement(WhoisResult sender, WhoisResult target)
        {
            if (sender == null || !sender.Success || target == null || !target.Success)
                return false;
            if (!this._bot.Users.Authorized(sender.Name.Nickname, UserLevel.SuperAdmin))
                // Clan only adds clan
                if (sender.Stats.Faction == "Clan")
                    if (target.Stats.Faction == "Clan")
                        return true;
                    else
                        return false;
                // Omni/Neutral only adds Omni/Neutral
                else if (sender.Stats.Faction == "Neutral" || sender.Stats.Faction == "Omni")
                    if (target.Stats.Faction == "Neutral" || target.Stats.Faction == "Omni")
                        return true;
                    else
                        return false;
            return true;
        }

        private void OnMembersLastCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Lastest New Members");
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT username, addedOn, addedBy FROM members ORDER BY addedOn DESC LIMIT 0,50";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            string username = reader.GetString(0);
                            Int64 addedOn = reader.GetInt64(1);
                            string addedBy = reader.GetString(2);
                            window.AppendHighlight(username);
                            window.AppendNormal(" (By " + addedBy + " on " + Format.DateTime(addedOn, FormatStyle.Compact) + ")");
                            window.AppendLineBreak();
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            bot.SendReply(e, "Latest New Members »» ", window);

            window = new RichTextWindow(bot);
            window.AppendTitle("Lastest New Alts");
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT altname, username, addedOn, addedBy FROM alts ORDER BY addedOn DESC LIMIT 0,50";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            string altname = reader.GetString(0);
                            string username = reader.GetString(1);
                            Int64 addedOn = reader.GetInt64(2);
                            string addedBy = reader.GetString(3);
                            window.AppendHighlight(altname);
                            window.AppendNormal(" (Main: " + username + ") (By " + addedBy + " on " + Format.DateTime(addedOn, FormatStyle.Compact) + ")");
                            window.AppendLineBreak();
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            bot.SendReply(e, "Latest New Alts »» ", window);
        }

        private void OnMembersAddCommand(BotShell bot, CommandArgs e)
        {
            // Check arguments
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: members add [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            // Validate user
            if (this.IsMember(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is already a member of this system");
                return;
            }
            if (this.IsAlt(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is already an alt on this system");
                return;
            }
            if (bot.Bans.IsBanned(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is banned from this system");
                return;
            }
            WhoisResult targetWhois = XML.GetWhois(username, bot.Dimension);
            if (targetWhois == null || !targetWhois.Success || e.SenderWhois == null)
            {
                bot.SendReply(e, "Unable to get the required player data for this action");
                return;
            }
            if (!this.IsAllowedMembersManagement(e.SenderWhois, targetWhois))
            {
                bot.SendReply(e, "Faction restrictions prevent you from executing this command");
                return;
            }
            // Add user
            if (this._database.ExecuteNonQuery(String.Format("REPLACE INTO members (username, userID, addedBy, addedOn) VALUES ('{0}', {1}, '{2}', {3})", username, bot.GetUserID(username), e.Sender, TimeStamp.Now)) > 0)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been added to the " + bot.Character + " members system");
                this.AutoEnable(bot, e, targetWhois, false);
            }
            else
            {
                bot.SendReply(e, "Unable to add " + HTML.CreateColorString(bot.ColorHeaderHex, username) + " to the " + bot.Character + " members system");
            }
        }

        private void OnMembersRemoveCommand(BotShell bot, CommandArgs e)
        {
            // Check arguments
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: members remove [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            // Validate user
            if (!this.IsMember(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is not a member of this system");
                return;
            }
            if (this.IsAlt(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is an alt on this system. Please use the 'alts remove' command instead");
                return;
            }
            if (this.IsEnabledAnywhere(username))
            {
                bot.SendReply(e, "You need to remove " + HTML.CreateColorString(bot.ColorHeaderHex, username + "'s") + " access from all groups before you can remove this member");
                return;
            }
            WhoisResult targetWhois = XML.GetWhois(username, bot.Dimension);
            if (targetWhois == null || !targetWhois.Success || e.SenderWhois == null)
            {
                bot.SendReply(e, "Unable to get the required player data for this action");
                return;
            }
            if (!this.IsAllowedMembersManagement(e.SenderWhois, targetWhois))
            {
                bot.SendReply(e, "Faction restrictions prevent you from executing this command");
                return;
            }
            // Remove user
            this._database.ExecuteNonQuery("DELETE FROM members WHERE username = '" + username + "'");
            this._database.ExecuteNonQuery("DELETE FROM members_access USING members_access, alts WHERE members_access.username = alts.altname AND alts.username = '" + username + "'");
            this._database.ExecuteNonQuery("DELETE FROM alts WHERE username = '" + username + "'");
            this._database.ExecuteNonQuery("DELETE FROM members_levels WHERE username = '" + username + "'");
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been removed from the " + bot.Character + " members system");
        }

        private void OnMembersPromoteCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: members promote [group] [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[1]));
            string group = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            if (!this.IsMember(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is not a member of this system");
                return;
            }
            if (this.IsAlt(username))
            {
                bot.SendReply(e, "You can't promote alts");
                return;
            }
            if (!this.IsGroup(group))
            {
                bot.SendReply(e, "No such group: " + HTML.CreateColorString(bot.ColorHeaderHex, group));
                return;
            }

            UserLevel userLevel = this.GetUserLevel(username, group);
            UserLevel senderLevel = this.GetUserLevel(e.Sender, group);
            if (senderLevel < UserLevel.Admin && e.Sender != bot.Admin)
            {
                e.Authorized = false;
                return;
            }
            if (userLevel == UserLevel.SuperAdmin)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " already has the highest rank possible");
                return;
            }
            if (senderLevel <= userLevel && e.Sender != bot.Admin)
            {
                bot.SendReply(e, "You can't promote a user to a higher rank than your own rank!");
                return;
            }
            bool active = false;
            if (this.IsEnabled(group, username))
                active = true;
            foreach (string alt in this.GetAlts(username))
                if (this.IsEnabled(group, alt))
                    active = true;
            if (!active)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " doesn't have any characters enabled on " + HTML.CreateColorString(bot.ColorHeaderHex, group));
                return;
            }
            userLevel = (UserLevel)((int)userLevel * 2);
            this._database.ExecuteNonQuery(String.Format("REPLACE INTO members_levels (username, `group`, userLevel) VALUES ('{0}', '{1}', {2})", username, group, (int)userLevel));
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been promoted to " + HTML.CreateColorString(bot.ColorHeaderHex, userLevel.ToString()) + " on " + HTML.CreateColorString(bot.ColorHeaderHex, group));
        }

        private void OnMembersDemoteCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: members demote [group] [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[1]));
            string group = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            if (!this.IsMember(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is not a member of this system");
                return;
            }
            if (this.IsAlt(username))
            {
                bot.SendReply(e, "You can't demote alts");
                return;
            }
            if (!this.IsGroup(group))
            {
                bot.SendReply(e, "No such group: " + HTML.CreateColorString(bot.ColorHeaderHex, group));
                return;
            }
            if (username == bot.Admin && e.Sender != bot.Admin)
            {
                bot.SendReply(e, "You can't demote the bot owner");
                return;
            }

            UserLevel userLevel = this.GetUserLevel(username, group);
            UserLevel senderLevel = this.GetUserLevel(e.Sender, group);
            if (senderLevel < UserLevel.Admin && e.Sender != bot.Admin)
            {
                e.Authorized = false;
                return;
            }
            if (senderLevel < userLevel && e.Sender != bot.Admin)
            {
                bot.SendReply(e, "You can't demote a user with a higher rank than your own!");
                return;
            }
            if (userLevel <= UserLevel.Member)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " can't be demoted any futher");
                return;
            }
            userLevel = (UserLevel)((int)userLevel / 2);
            if (userLevel == UserLevel.Guest)
                this._database.ExecuteNonQuery("DELETE FROM members_levels WHERE username = '" + username + "' AND `group` = '" + group + "'");
            else
                this._database.ExecuteNonQuery(String.Format("REPLACE INTO members_levels (username, `group`, userLevel) VALUES ('{0}', '{1}', {2})", username, group, (int)userLevel));
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been demoted to " + HTML.CreateColorString(bot.ColorHeaderHex, userLevel.ToString()) + " on " + HTML.CreateColorString(bot.ColorHeaderHex, group));
        }

        private void OnAccountCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: account [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            username = this.GetMain(username);
            if (!this.IsMember(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is not a member of this system");
                return;
            }
            User user = this.GetUserInformation(username);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Account Information");

            window.AppendHighlight("Main: ");
            window.AppendNormal(user.Username);
            window.AppendLineBreak();

            window.AppendHighlight("Added By: ");
            window.AppendNormal(user.AddedBy);
            window.AppendLineBreak();

            window.AppendHighlight("Added On: ");
            if (user.AddedOn > 0)
                window.AppendNormal(Format.DateTime(user.AddedOn, FormatStyle.Compact));
            else
                window.AppendNormal("N/A");
            window.AppendLineBreak();

            List<string> characters = new List<string>();
            characters.Add(user.Username);
            foreach (string alt in user.Alts)
                characters.Add(alt);

            window.AppendHighlight("Characters: ");
            window.AppendLineBreak();
            foreach (string character in characters)
            {
                window.AppendNormalStart();
                window.AppendString("- ");
                WhoisResult whois = XML.GetWhois(character, bot.Dimension);
                if (whois != null && whois.Success)
                    window.AppendString(Format.Whois(whois, FormatStyle.Compact));
                else
                    window.AppendString(character);
                window.AppendString(" (");
                if (bot.FriendList.IsOnline(character) == OnlineState.Online)
                    window.AppendColorString(RichTextWindow.ColorGreen, "Online");
                else
                {
                    string lastseen = string.Empty;
                    Int64 seen = this._bot.FriendList.Seen(username);
                    if (seen > 1)
                    {
                        lastseen = " since " + Format.DateTime(seen, FormatStyle.Compact);
                    }
                    window.AppendColorString(RichTextWindow.ColorRed, "Offline" + lastseen);
                }
                window.AppendString(")");
                window.AppendColorEnd();
                window.AppendLineBreak();
            }
            window.AppendLineBreak();

            foreach (VhanetGroup group in this.GetGroups())
            {
                if (!group.Primary) continue;
                if (e.Sender != bot.Admin && this.GetUserLevel(e.Sender, group.Group) < UserLevel.Admin)
                    continue;

                window.AppendHeader(group.Group);
                window.AppendHighlight("User Level: ");
                window.AppendNormalStart();
                window.AppendString(this.GetUserLevel(user.Username, group.Group).ToString());
                window.AppendString(" [");
                window.AppendBotCommand("Promote", "members promote " + group.Group + " " + user.Username);
                window.AppendString("] [");
                window.AppendBotCommand("Demote", "members demote " + group.Group + " " + user.Username);
                window.AppendString("]");
                window.AppendColorEnd();
                window.AppendLineBreak();

                window.AppendHighlight("Characters:");
                window.AppendLineBreak();
                foreach (string character in characters)
                {
                    window.AppendNormalStart();
                    window.AppendString("- " + character + " (");
                    if (this.IsEnabled(group.Group, character))
                    {
                        window.AppendColorString(RichTextWindow.ColorGreen, "Enabled");
                        window.AppendString(") [");
                        window.AppendBotCommand("Disable", "members disable " + group.Group + " " + character);
                    }
                    else
                    {
                        window.AppendColorString(RichTextWindow.ColorRed, "Disabled");
                        window.AppendString(") [");
                        window.AppendBotCommand("Enable", "members enable " + group.Group + " " + character);
                    }
                    window.AppendString("]");
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                }
                window.AppendLineBreak();
            }

            bot.SendReply(e, user.Username + "'s Account »» ", window);
        }

        private void OnMembersEnableCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: members enable [group] [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[1]));
            string group = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            if (!this.IsMember(username) && !this.IsAlt(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is not known on this system");
                return;
            }
            if (!this.IsGroup(group))
            {
                bot.SendReply(e, "No such group: " + HTML.CreateColorString(bot.ColorHeaderHex, group));
                return;
            }
            if (this.IsEnabled(group, username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is already has been granted access for group " + HTML.CreateColorString(bot.ColorHeaderHex, group));
                return;
            }
            UserLevel userLevel = this.GetUserLevel(username, group);
            UserLevel senderLevel = this.GetUserLevel(e.Sender, group);
            if (senderLevel < UserLevel.Admin && e.Sender != bot.Admin)
            {
                e.Authorized = false;
                return;
            }
            if (senderLevel < userLevel && e.Sender != bot.Admin)
            {
                bot.SendReply(e, "You can't enable a user with a higher rank than your own!");
                return;
            }
            WhoisResult whois = XML.GetWhois(username, bot.Dimension);
            if (whois == null || !whois.Success)
            {
                bot.SendReply(e, "Unable to get the required player data for this action");
                return;
            }
            VhanetGroup groupDetails = this.GetGroup(group);
            if (groupDetails == null)
            {
                bot.SendReply(e, "Unable to get the required group data for this action");
                return;
            }
            if (!bot.Users.Authorized(e.Sender, UserLevel.SuperAdmin))
            {
                switch (whois.Stats.Faction)
                {
                    case "Clan":
                        if (groupDetails.AllowedClan)
                            break;
                        else
                            goto default;
                    case "Neutral":
                        if (groupDetails.AllowedNeutral)
                            break;
                        else
                            goto default;
                    case "Omni":
                        if (groupDetails.AllowedOmni)
                            break;
                        else
                            goto default;
                    default:
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, whois.Stats.Faction) + " members can't be enabled on this group");
                        return;
                }
                if (this.IsMember(username))
                {
                    if (whois.Stats.Level < groupDetails.RequirementMain)
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " doesn't meet the main level requirement of " + HTML.CreateColorString(bot.ColorHeaderHex, groupDetails.RequirementMain.ToString()));
                        return;
                    }
                }
                else
                {
                    if (whois.Stats.Level < groupDetails.RequirementAlt)
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " doesn't meet the alt level requirement of " + HTML.CreateColorString(bot.ColorHeaderHex, groupDetails.RequirementAlt.ToString()));
                        return;
                    }
                }
            }

            int promoted = this._database.ExecuteNonQuery(String.Format("INSERT IGNORE INTO members_levels (username, `group`, userLevel) VALUES ('{0}', '{1}', {2})", this.GetMain(username), group, (int)UserLevel.Member));
            this._database.ExecuteNonQuery("REPLACE INTO members_access (username, `group`) VALUES ('" + username + "', '" + group + "')");
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been granted access on " + HTML.CreateColorString(bot.ColorHeaderHex, group));
            if (promoted > 0)
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has automatically been promoted to " + HTML.CreateColorString(bot.ColorHeaderHex, "Member") + " on " + HTML.CreateColorString(bot.ColorHeaderHex, group));
        }

        private void OnMembersDisableCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: members disable [group] [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[1]));
            string group = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(username) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            if (!this.IsMember(username) && !this.IsAlt(username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is not known on this system");
                return;
            }
            if (!this.IsGroup(group))
            {
                bot.SendReply(e, "No such group: " + HTML.CreateColorString(bot.ColorHeaderHex, group));
                return;
            }
            if (!this.IsEnabled(group, username))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " hasn't been granted access for group " + HTML.CreateColorString(bot.ColorHeaderHex, group));
                return;
            }
            UserLevel userLevel = this.GetUserLevel(username, group);
            UserLevel senderLevel = this.GetUserLevel(e.Sender, group);
            if (senderLevel < UserLevel.Admin && e.Sender != bot.Admin)
            {
                e.Authorized = false;
                return;
            }
            if (senderLevel < userLevel && e.Sender != bot.Admin)
            {
                bot.SendReply(e, "You can't disable a user with a higher rank than your own!");
                return;
            }
            this._database.ExecuteNonQuery("DELETE FROM members_access WHERE username = '" + username + "' AND `group` = '" + group + "'");
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been denied access on " + HTML.CreateColorString(bot.ColorHeaderHex, group));
            username = this.GetMain(username);
            if (this.IsEnabled(group, username))
                return;
            foreach (string alt in this.GetAlts(username))
                if (this.IsEnabled(group, alt))
                    return;
            this._database.ExecuteNonQuery("DELETE FROM members_levels WHERE username = '" + username + "' AND `group` = '" + group + "'");
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been automatically demoted to " + HTML.CreateColorString(bot.ColorHeaderHex, UserLevel.Guest.ToString()) + " on " + HTML.CreateColorString(bot.ColorHeaderHex, group));
        }

        private void OnAltsAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: alts alt [main] [alt]");
                return;
            }
            string main = this.GetMain(e.Args[0]);
            string alt = Config.EscapeString(Format.UppercaseFirst(e.Args[1]));
            if (bot.GetUserID(alt) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, alt));
                return;
            }
            if (!this.IsMember(main))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, main) + " is not a registered main on this system");
                return;
            }
            if (this.IsMember(alt) || this.IsAlt(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is already registered on this system");
                return;
            }
            WhoisResult mainWhois = XML.GetWhois(main, bot.Dimension);
            WhoisResult altWhois = XML.GetWhois(alt, bot.Dimension);
            if (mainWhois == null || !mainWhois.Success || altWhois == null || !altWhois.Success || e.SenderWhois == null)
            {
                bot.SendReply(e, "Unable to get the required player data for this action");
                return;
            }
            if (!this.IsAllowedMembersManagement(e.SenderWhois, mainWhois) || !this.IsAllowedMembersManagement(e.SenderWhois, altWhois))
            {
                bot.SendReply(e, "Faction restrictions prevent you from executing this command");
                return;
            }
            this._database.ExecuteNonQuery(String.Format("INSERT INTO alts (username, altname, altid, addedBy, addedOn) VALUES ('{0}', '{1}', {2}, '{3}', {4})", main, alt, bot.GetUserID(alt), e.Sender, TimeStamp.Now));
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " has been added as alt to " + HTML.CreateColorString(bot.ColorHeaderHex, main + "'s") + " account");
            this.AutoEnable(bot, e, altWhois, true);
        }

        private void OnAltsRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: alts remove [alt]");
                return;
            }
            string alt = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            if (bot.GetUserID(alt) < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, alt));
                return;
            }
            if (this.IsMember(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is member of this bot. Use 'members remove' instead");
                return;
            }
            if (!this.IsAlt(alt))
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " is not a registered alt on this system");
                return;
            }
            if (this.IsEnabledAnywhere(alt))
            {
                bot.SendReply(e, "You need to remove " + HTML.CreateColorString(bot.ColorHeaderHex, alt + "'s") + " access from all groups before you can remove this alt");
                return;
            }
            WhoisResult altWhois = XML.GetWhois(alt, bot.Dimension);
            if (altWhois == null || !altWhois.Success || e.SenderWhois == null)
            {
                bot.SendReply(e, "Unable to get the required player data for this action");
                return;
            }
            if (!this.IsAllowedMembersManagement(e.SenderWhois, altWhois))
            {
                bot.SendReply(e, "Faction restrictions prevent you from executing this command");
                return;
            }
            this._database.ExecuteNonQuery("DELETE FROM members_access WHERE username = '" + alt + "'");
            this._database.ExecuteNonQuery("DELETE FROM alts WHERE altname = '" + alt + "'");
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, alt) + " has been removed");
        }

        private void OnPasswordCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: password [password] [verify password]");
                return;
            }
            if (e.Args[0] != e.Args[1])
            {
                bot.SendReply(e, "The passwords you supplied don't match!");
                return;
            }
            if (e.Args[0].Length < 5)
            {
                bot.SendReply(e, "Your password needs to be at least 5 characters");
                return;
            }
            if (e.Args[0].Contains(@"\") || e.Args[0].Contains(@"'"))
            {
                bot.SendReply(e, "Your password contains invalid characters");
                return;
            }
            if (this.IsAlt(e.Sender))
            {
                bot.SendReply(e, "This command is only available from your main");
                return;
            }
            string password = e.Args[0];
            string format = "UPDATE members SET password = MD5('${0}$-${1}$') WHERE username = '{0}'";
            if (this._database.ExecuteNonQuery(String.Format(format, e.Sender, Config.EscapeString(password))) > 0)
            {
                bot.SendReply(e, "Your password has been updated");
                return;
            }
            bot.SendReply(e, "Unable to update your password!");
        }

        private void OnBotsCommand(BotShell bot, CommandArgs e)
        {
            VhanetGroup[] groups = this.GetGroups();
            if (groups.Length == 0)
            {
                bot.SendReply(e, "Unable to display the bots list!");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            foreach (VhanetGroup group in groups)
            {
                window.AppendHeader(group.Name);
                window.AppendHighlight("Members Group: ");
                window.AppendNormal(group.Group);
                window.AppendLineBreak();

                window.AppendHighlight("Raid Bot: ");
                if (group.Raid)
                    window.AppendNormal("Yes");
                else
                    window.AppendNormal("No");
                window.AppendLineBreak();

                window.AppendHighlight("Primary Bot: ");
                if (group.Primary)
                    window.AppendNormal("Yes");
                else
                    window.AppendNormal("No");
                window.AppendLineBreak();

                if (group.Raid && group.Primary)
                {
                    window.AppendHighlight("Main Level Requirement: ");
                    window.AppendNormal(group.RequirementMain.ToString());
                    window.AppendLineBreak();

                    window.AppendHighlight("Alt Level Requirement: ");
                    window.AppendNormal(group.RequirementAlt.ToString());
                    window.AppendLineBreak();

                    window.AppendHighlight("Accepts Clan: ");
                    if (group.AllowedClan)
                        window.AppendNormal("Yes");
                    else
                        window.AppendNormal("No");
                    window.AppendLineBreak();

                    window.AppendHighlight("Accepts Neutral: ");
                    if (group.AllowedNeutral)
                        window.AppendNormal("Yes");
                    else
                        window.AppendNormal("No");
                    window.AppendLineBreak();

                    window.AppendHighlight("Accepts Omni: ");
                    if (group.AllowedOmni)
                        window.AppendNormal("Yes");
                    else
                        window.AppendNormal("No");
                    window.AppendLineBreak();

                    window.AppendHighlight("Automated Membership: ");
                    if (group.AutoEnable)
                        window.AppendNormal("Yes");
                    else
                        window.AppendNormal("No");
                    window.AppendLineBreak();
                }

                window.AppendHighlight("Description: ");
                window.AppendNormal(group.Description);
                window.AppendLineBreak(2);
            }
            bot.SendReply(e, "Bots Overview »» ", window);
        }

        public User GetUserInformation(string username)
        {
            User result = null;
            username = Format.UppercaseFirst(Config.EscapeString(username));
            /*if (username == this._bot.Admin)
            {
                return new User(username, this._bot.GetUserID(username), UserLevel.SuperAdmin, "Core", 0, this.GetAlts(username));
            }*/
            string[] alts = this.GetAlts(username);
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT username, userID, addedOn, addedBy FROM members WHERE username = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        try
                        {
                            result = new User(reader.GetString(0), (UInt32)reader.GetInt64(1), UserLevel.Guest, reader.GetString(3), reader.GetInt64(2), alts);
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public string[] GetAlts(string username)
        {
            username = Format.UppercaseFirst(Config.EscapeString(username));
            List<string> alts = new List<string>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT altname FROM alts WHERE username = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            alts.Add(Format.UppercaseFirst(reader.GetString(0)));
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return alts.ToArray();
        }

        public string GetMain(string username)
        {
            username = Format.UppercaseFirst(Config.EscapeString(username));
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT username FROM alts WHERE altname = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        try
                        {
                            username = Format.UppercaseFirst(reader.GetString(0));
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return username;
        }

        public UserLevel GetHighestUserLevel(string username)
        {
            username = this.GetMain(username);
            /*if (username == this._bot.Admin)
                return UserLevel.SuperAdmin;*/
            UserLevel result = UserLevel.Guest;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT userLevel FROM members_levels WHERE username = '" + username + "' ORDER BY userLevel DESC";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        try { result = (UserLevel)reader.GetInt32(0); }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public UserLevel GetUserLevel(string username, string group)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            /*if (username == this._bot.Admin)
                return UserLevel.SuperAdmin;
            if (this.GetMain(username) == this._bot.Admin)
                return UserLevel.SuperAdmin;*/
            group = Config.EscapeString(Format.UppercaseFirst(group));
            UserLevel result = UserLevel.Guest;
            if (!this.IsGroup(group))
            {
                return result;
            }
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    if (this.IsAlt(username))
                        command.CommandText = "SELECT members_levels.userLevel FROM members_levels, alts WHERE alts.username = members_levels.username AND alts.altname = '" + username + "' AND members_levels.`group` = '" + group + "'";
                    else
                        command.CommandText = "SELECT userLevel FROM members_levels WHERE username = '" + username + "' AND `group` = '" + group + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        try { result = (UserLevel)reader.GetInt32(0); }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public bool IsGroup(string group)
        {
            foreach (VhanetGroup vhanetGroup in this.GetGroups())
                if (vhanetGroup.Group.Equals(group, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            return false;
        }

        public bool IsMember(string username)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            /*if (username == this._bot.Admin)
                return true;*/
            bool result = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM members WHERE username = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        result = true;
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public bool IsAlt(string username)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            /*if (username == this._bot.Admin)
                return false;*/
            bool result = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM alts WHERE altname = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        result = true;
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public bool IsEnabled(string group, string username)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            group = Config.EscapeString(Format.UppercaseFirst(group));
            /*if (username == this._bot.Admin)
                return true;*/
            bool result = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM members_access WHERE username = '" + username + "' AND `group` = '" + group + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        result = true;
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public bool IsEnabledAnywhere(string username)
        {
            username = Config.EscapeString(Format.UppercaseFirst(username));
            bool result = false;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM members_access WHERE username = '" + username + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        result = true;
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public string[] GetMembers()
        {
            List<string> members = new List<string>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT username as name FROM members UNION SELECT altname FROM alts ORDER BY name";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            if (!members.Contains(reader.GetString(0).ToLower()))
                                members.Add(reader.GetString(0).ToLower());
                        }
                        catch { }
                    }
                    reader.Close();
                }
            }
            return members.ToArray();
        }

        public void AutoEnable(BotShell bot, CommandArgs e, WhoisResult target, bool alt)
        {
            if (target == null || !target.Success) return;
            WhoisResult main = null;
            if (alt) main = XML.GetWhois(this.GetMain(target.Name.Nickname), bot.Dimension);
            foreach (VhanetGroup group in this.GetGroups())
            {
                if (!group.Primary) continue;
                if (!group.AutoEnable) continue;
                if (target.Stats.Faction.ToLower() == "clan")
                { if (!group.AllowedClan) continue; }
                else if (target.Stats.Faction.ToLower() == "neutral")
                { if (!group.AllowedNeutral) continue; }
                else if (target.Stats.Faction.ToLower() == "omni")
                { if (!group.AllowedOmni) continue; }
                else continue;

                if (alt)
                {
                    if (target.Stats.Level < group.RequirementAlt)
                        continue;
                    if (main == null || !main.Success)
                        continue;
                    if (main.Stats.Level < group.RequirementMain)
                        continue;
                }
                else
                {
                    if (target.Stats.Level < group.RequirementMain)
                        continue;
                }
                this._database.ExecuteNonQuery("REPLACE INTO members_access (username, `group`) VALUES ('" + target.Name.Nickname + "', '" + group.Group + "')");
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, target.Name.Nickname) + " has been automatically granted access on " + HTML.CreateColorString(bot.ColorHeaderHex, group.Group));
                int promoted = this._database.ExecuteNonQuery(String.Format("INSERT IGNORE INTO members_levels (username, `group`, userLevel) VALUES ('{0}', '{1}', {2})", this.GetMain(target.Name.Nickname), group.Group, (int)UserLevel.Member));
                if (promoted > 0)
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, this.GetMain(target.Name.Nickname)) + " has automatically been promoted to " + HTML.CreateColorString(bot.ColorHeaderHex, "Member") + " on " + HTML.CreateColorString(bot.ColorHeaderHex, group.Group));

            }
        }

        #region Group Functions
        public VhanetGroup[] GetGroups()
        {
            List<VhanetGroup> groups = new List<VhanetGroup>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT bot, `group`, raid, tracker, requirementMain, requirementAlt, allowedClan, allowedNeutral, allowedOmni, autoEnable, description FROM bots ORDER BY bot ASC";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        groups.Add(new VhanetGroup(
                            reader.GetString(0),
                            reader.GetString(1),
                            (reader.GetInt32(2) == 1),
                            (reader.GetInt32(3) == 1),
                            reader.GetInt32(4),
                            reader.GetInt32(5),
                            (reader.GetInt32(6) == 1),
                            (reader.GetInt32(7) == 1),
                            (reader.GetInt32(8) == 1),
                            (reader.GetInt32(9) == 1),
                            reader.GetString(10)
                        ));
                    }
                    reader.Close();
                }
            }
            return groups.ToArray();
        }

        public VhanetGroup GetGroup(string group)
        {
            foreach (VhanetGroup grp in this.GetGroups())
                if (grp.Name.Equals(group, StringComparison.CurrentCultureIgnoreCase))
                    return grp;
            return null;
        }
        #endregion

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
                    return "Allows you to promote an existing member to a higher rank on the specified group.\nYou can't promote anyone to a higher rank than your own.\n" +
                        "Usage: /tell " + bot.Character + " members promote [group] [username]";
                case "members demote":
                    return "Allows you to demote an existing member to a lower rank on the specified group.\nYou can't demote anyone with a higher rank than your own.\n" +
                        "Usage: /tell " + bot.Character + " members demote [group] [username]";
                case "members enable":
                    return "Allows you to grant a character access to the specified group.\n" +
                        "Usage: /tell " + bot.Character + " members enable [group] [username]";
                case "members disable":
                    return "Allows you to deny a character access to the specified group.\n" +
                        "Usage: /tell " + bot.Character + " members disable [group] [username]";
                case "members last":
                    return "Displays a list of the latest members and alts.\n" +
                        "Usage: /tell " + bot.Character + " members last";
                case "alts add":
                    return "Allows you to add [altname] as an alternative character to [main]'s account.\nAlts will inherit the user rights of their main.\n" +
                        "Usage: /tell " + bot.Character + " alts add [main] [altname]";
                case "alts remove":
                    return "Allows you to remove [altname] from the bot.\n" +
                        "Usage: /tell " + bot.Character + " alts remove [altname]";
                case "account":
                    return "Shows you an overview of the user's account and his/her access to various groups\n" +
                        "Usage: /tell " + bot.Character + " account [username]";
                case "password":
                    return "Allows you to change your password.\nYou need to specify your new password twice in order to properly update it.\n" +
                        "Usage: /tell " + bot.Character + " password [password] [verify password]";
                case "bots":
                    return "Displays a list of all bots connected to this system\n" +
                        "Usage: /tell " + bot.Character + " bots";
            }
            return null;
        }
    }

    public class VhanetGroup
    {
        public readonly string Name;
        public readonly string Group;
        public bool Primary { get { return (this.Name.ToLower() == this.Group.ToLower()); } }
        public readonly bool Raid;
        public readonly bool Tracker;
        public readonly int RequirementMain;
        public readonly int RequirementAlt;
        public readonly bool AllowedClan;
        public readonly bool AllowedNeutral;
        public readonly bool AllowedOmni;
        public readonly bool AutoEnable;
        public readonly string Description;

        public VhanetGroup(string name, string group, bool raid, bool tracker, int requirementMain, int requirementAlt, bool allowedClan, bool allowedNeutral, bool allowedOmni, bool autoEnable, string description)
        {
            this.Name = name;
            this.Group = group;
            this.Raid = raid;
            this.Tracker = tracker;
            this.RequirementMain = requirementMain;
            this.RequirementAlt = requirementAlt;
            this.AllowedClan = allowedClan;
            this.AllowedNeutral = allowedNeutral;
            this.AllowedOmni = allowedOmni;
            this.AutoEnable = autoEnable;
            this.Description = description;
        }
    }
}
