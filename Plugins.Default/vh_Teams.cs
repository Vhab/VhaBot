using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;
using VhaBot.Communication;

namespace VhaBot.Plugins
{
    public class Teams : PluginBase
    {
        private Config _database;
        public Teams()
        {
            this.Name = "Teams Manager";
            this.InternalName = "vhTeams";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Description = "Provides a team management system";
            this.Commands = new Command[] {
                new Command("teams", true, UserLevel.Member),
                new Command("teams admin", true, UserLevel.Leader),
                new Command("teams start", false, UserLevel.Leader),
                new Command("teams set", false, UserLevel.Leader),
                new Command("teams remove", false, UserLevel.Leader),
                new Command("teams leader", false, UserLevel.Leader),
                new Command("teams clear", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot) {
            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS teams (username VARCHAR(14), team INTEGER, leader INTEGER)");
        }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "teams":
                    this.OnTeamsCommand(bot, e);
                    break;
                case "teams admin":
                    this.OnTeamsAdminCommand(bot, e);
                    break;
                case "teams start":
                    this.OnTeamsStartCommand(bot, e);
                    break;
                case "teams set":
                    this.OnTeamsSetCommand(bot, e);
                    break;
                case "teams remove":
                    this.OnTeamsRemoveCommand(bot, e);
                    break;
                case "teams leader":
                    this.OnTeamsLeaderCommand(bot, e);
                    break;
                case "teams clear":
                    this.OnTeamsClearCommand(bot, e);
                    break;
            }
        }

        private void OnTeamsCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendHighlight(":: ");
            window.AppendColorString(bot.ColorHeaderHex, "Teams");
            window.AppendHighlight(" ::");
            bool found = false;
            int currentTeam = -1;
            int currentMember = 0;
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT username, team, leader FROM teams ORDER BY team, username";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string username = Format.UppercaseFirst(reader.GetString(0));
                    int team = (int)reader.GetInt64(1);
                    bool leader = (reader.GetInt64(2) > 0);
                    if (team != currentTeam)
                    {
                        currentMember = 0;
                        currentTeam = team;
                        window.AppendLineBreak();
                        window.AppendHighlight("  Team " + team + ": ");
                    }
                    currentMember++;
                    window.AppendNormalStart();
                    window.AppendString("");
                    if (leader)
                        window.AppendColorString(RichTextWindow.ColorRed, username);
                    else
                        window.AppendString(username);
                    window.AppendString(" ");
                    window.AppendColorEnd();
                    found = true;
                }
                reader.Close();
            }
            if (found)
            {
                bot.SendReply(e, window.Text);
                return;
            }
            bot.SendReply(e, "There are currently no teams setup");
        }

        private void OnTeamsAdminCommand(BotShell bot, CommandArgs e)
        {
            List<string> seen = new List<string>();
            List<int> teams = new List<int>();
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT team FROM teams GROUP BY team";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    teams.Add((int)reader.GetInt64(0));
                reader.Close();
            }

            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Teams Manager");
            window.AppendCommand("Display Teams", "/g " + bot.Character + " " + bot.CommandSyntax + "teams");
            window.AppendLineBreak();
            window.AppendBotCommand("Refresh Manager", "teams admin");
            window.AppendLineBreak();
            window.AppendBotCommand("Clear Teams", "teams clear");
            window.AppendLineBreak();

            int currentTeam = -1;
            int currentMember = 0;
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT username, team, leader FROM teams ORDER BY team, username";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string username = Format.UppercaseFirst(reader.GetString(0));
                    seen.Add(username);
                    int team = (int)reader.GetInt64(1);
                    bool leader = (reader.GetInt64(2) > 0);
                    if (team != currentTeam)
                    {
                        currentMember = 0;
                        currentTeam = team;
                        window.AppendLineBreak();
                        window.AppendHeader("Team " + team);
                    }
                    currentMember++;
                    WhoisResult whois = XML.GetWhois(username, bot.Dimension);
                    if (whois != null && whois.Success)
                        window.AppendHighlight(Format.Whois(whois, FormatStyle.Compact));
                    else
                        window.AppendHighlight(username);

                    window.AppendNormalStart();
                    if (leader)
                    {
                        window.AppendString(" (");
                        window.AppendColorString(RichTextWindow.ColorRed, "Leader");
                        window.AppendString(")");
                    }
                    foreach (int tm in teams)
                    {
                        if (tm == team)
                            continue;
                        window.AppendString(" [");
                        window.AppendBotCommand(tm.ToString(), "teams set " + username + " " + tm);
                        window.AppendString("]");
                    }
                    if (!leader)
                    {
                        window.AppendString(" [");
                        window.AppendBotCommand("Leader", "teams leader " + username + " " + team);
                        window.AppendString("]");
                    }
                    window.AppendString(" [");
                    window.AppendBotCommand("Remove", "teams remove " + username);
                    window.AppendString("]");
                    window.AppendString(" [");
                    window.AppendBotCommand("New Team", "teams start " + username);
                    window.AppendString("]");
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                }
                reader.Close();
            }
            window.AppendLineBreak();

            window.AppendHeader("Looking for Team");
            int lft = 0;
            List<string> list = new List<string>();
            foreach (Friend user in bot.PrivateChannel.List().Values)
                list.Add(user.User);

            // If 'Raid :: Core' is loaded, fetch the lft list from there
            List<string> raiders = new List<string>();
            if (bot.Plugins.IsLoaded("raidcore"))
            {
                try
                {
                    ReplyMessage reply = bot.SendPluginMessageAndWait(this.InternalName, "raidcore", "GetActiveRaiders", 100);
                    if (reply != null && reply.Args.Length > 0)
                        raiders = new List<string>((string[])reply.Args);
                }
                catch { }
            }

            // Sort LFT list
            SortedDictionary<string, SortedDictionary<string, WhoisResult>> sorted = new SortedDictionary<string, SortedDictionary<string, WhoisResult>>();
            foreach (string member in list)
            {
                WhoisResult whois = XML.GetWhois(member, bot.Dimension);
                if (whois == null || !whois.Success)
                    whois = null;
                if (!sorted.ContainsKey(whois.Stats.Profession))
                    sorted.Add(whois.Stats.Profession, new SortedDictionary<string, WhoisResult>());
                sorted[whois.Stats.Profession].Add(member, whois);
            }

            // Display LFT list
            foreach (KeyValuePair<string, SortedDictionary<string, WhoisResult>> kvp in sorted)
            {
                window.AppendHighlight(kvp.Key);
                window.AppendLineBreak();
                foreach (KeyValuePair<string, WhoisResult> member in kvp.Value)
                {
                    if (seen.Contains(member.Key))
                        continue;
                    window.AppendNormalStart();
                    window.AppendString("- ");
                    if (member.Value != null && member.Value.Success)
                        window.AppendString(Format.Whois(member.Value, FormatStyle.Compact));
                    else
                        window.AppendString(member.Key);
                    if (raiders.Contains(member.Key))
                    {
                        window.AppendString(" [");
                        window.AppendColorString(RichTextWindow.ColorGreen, "R");
                        window.AppendString("]");
                    }
                    foreach (int tm in teams)
                    {
                        window.AppendString(" [");
                        window.AppendBotCommand(tm.ToString(), "teams set " + member.Key + " " + tm);
                        window.AppendString("]");
                    }
                    window.AppendString(" [");
                    window.AppendBotCommand("New Team", "teams start " + member.Key);
                    window.AppendString("]");
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                    lft++;
                }
                window.AppendLineBreak();
            }
            if (lft == 0)
            {
                window.AppendHighlight("None");
            }
            bot.SendReply(e, "Teams Manager »» ", window);
        }

        private void OnTeamsStartCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: teams start [username]");
                return;
            }
            string username = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(username) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            int team = 1;
            bool search = true;
            this._database.ExecuteNonQuery("DELETE FROM teams WHERE username = '" + username + "'");
            while (search)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM teams WHERE team = " + team;
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                        team++;
                    else
                        search = false;
                    reader.Close();
                }
            }
            this._database.ExecuteNonQuery(string.Format("INSERT INTO teams (username, team, leader) VALUES ('{0}', {1}, 1)", username, team));
            bot.SendReply(e, "You have created team " + HTML.CreateColorString(bot.ColorHeaderHex, team.ToString()) + " with " + HTML.CreateColorString(bot.ColorHeaderHex, username) + " as team leader");
            bot.SendPrivateChannelMessage(bot.ColorHighlight + "Team " + HTML.CreateColorString(bot.ColorHeaderHex, team.ToString()) + " has been started with " + HTML.CreateColorString(bot.ColorHeaderHex, username) + " as leader");
        }

        private void OnTeamsSetCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: teams set [username] [team]");
                return;
            }
            string username = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(username) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            int team;
            if (!Int32.TryParse(e.Args[1], out team) || (team < 1))
            {
                bot.SendReply(e, "Invalid Team: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[1]));
                return;
            }
            this._database.ExecuteNonQuery("DELETE FROM teams WHERE username = '" + username + "'");
            this._database.ExecuteNonQuery(string.Format("INSERT INTO teams (username, team, leader) VALUES ('{0}', {1}, 0)", username, team));
            bot.SendReply(e, "You have added " + HTML.CreateColorString(bot.ColorHeaderHex, username) + " to team " + HTML.CreateColorString(bot.ColorHeaderHex, team.ToString()));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been moved to team " + HTML.CreateColorString(bot.ColorHeaderHex, team.ToString()));
        }

        private void OnTeamsRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: teams remove [username]");
                return;
            }
            string username = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(username) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }

            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM teams WHERE username = '" + username + "'";
                IDataReader reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    reader.Close();
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is not on any team");
                    return;
                }
                reader.Close();
            }
            this._database.ExecuteNonQuery("DELETE FROM teams WHERE username = '" + username + "'");
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been removed from the team");
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, username) + " has been removed from the team");
        }

        private void OnTeamsLeaderCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: teams set [username] [team]");
                return;
            }
            string username = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(username) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            int team;
            if (!Int32.TryParse(e.Args[1], out team) || (team < 1))
            {
                bot.SendReply(e, "Invalid Team: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[1]));
                return;
            }
            this._database.ExecuteNonQuery("DELETE FROM teams WHERE username = '" + username + "'");
            this._database.ExecuteNonQuery("UPDATE teams SET leader = 0 WHERE team = " + team);
            this._database.ExecuteNonQuery(string.Format("INSERT INTO teams (username, team, leader) VALUES ('{0}', {1}, 1)", username, team));
            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " is now the leader of team " + HTML.CreateColorString(bot.ColorHeaderHex, team.ToString()));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, username) + " is now leader of team " + HTML.CreateColorString(bot.ColorHeaderHex, team.ToString()));
        }

        private void OnTeamsClearCommand(BotShell bot, CommandArgs e)
        {
            this._database.ExecuteNonQuery("DELETE FROM teams");
            bot.SendReply(e, "All teams have been cleared");
            bot.SendPrivateChannelMessage(bot.ColorHighlight + "All teams have been cleared");
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "teams":
                    return "Displays the configured teams.\n" +
                        "Usage: /tell " + bot.Character + " teams";
                case "teams admin":
                    return "Displays an interface that allows you to configure the teams.\n" +
                        "Usage: /tell " + bot.Character + " teams admin";
                case "teams clear":
                    return "Clears all configured teams.\n" +
                        "Usage: /tell " + bot.Character + " teams clear";
            }
            return null;
        }
    }
}
