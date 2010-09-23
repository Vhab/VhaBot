using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Security.Cryptography;
using AoLib.Utils;
using VhaBot;
using VhaBot.ShellModules;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class VhanetWebLink : PluginBase
    {
        private BotShell _bot;
        private VhanetDatabase _database;
        private VhanetMembersHost _host;

        public VhanetWebLink()
        {
            this.Name = "Vhanet :: Web Link";
            this.InternalName = "VhanetWebLink";
            this.Version = 100;
            this.Author = "Vhab";
            this.Dependencies = new string[] { "VhanetDatabase", "VhanetMembersHost" };
            this.DefaultState = PluginState.Disabled;
            this.Description = "Allows users to enable/create Gridnet accounts";
            this.Commands = new Command[] {
                new Command("forum", false, UserLevel.Admin),
                new Command("activate", false, UserLevel.Guest),
                new Command("register", false, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
            try { this._database = (VhanetDatabase)bot.Plugins.GetPlugin("VhanetDatabase"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Database' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            try { this._host = (VhanetMembersHost)bot.Plugins.GetPlugin("VhanetMembersHost"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Members Host' Plugin!"); }
        }


        public override void OnCommand(BotShell sender, CommandArgs e)
        {
            if (!this._database.CheckDatabase(sender, e))
                return;

            switch (e.Command)
            {
                case "activate":
                    this.OnActivateCommand(sender, e);
                    break;
                case "register":
                    this.OnRegisterCommand(sender, e);
                    break;
                case "forum":
                    this.OnForumCommand(sender, e);
                    break;
            }
        }

        public override void OnUnauthorizedCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length > 0)
                return;
            if (e.Command == "activate" || e.Command == "forum")
            {
                e.Authorized = true;
                this.OnCommand(bot, e);
            }
        }

        private void OnForumCommand(BotShell bot, CommandArgs e)
        {
            string username = e.Sender;
            if (e.Args.Length > 0)
                username = Format.UppercaseFirst(e.Args[0]);

            ForumAccount account = this.GetAccount(username);
            if (account == null)
            {
                bot.SendReply(e, "Unable to locate " + HTML.CreateColorString(bot.ColorHeaderHex, username + "'s") + " forum account");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle(username + "'s Forum Account");
            
            window.AppendHighlight("Name: ");
            window.AppendNormal(account.Realname);
            window.AppendLineBreak();

            window.AppendHighlight("Primary Group: ");
            if (account.PrimaryGroup != string.Empty)
                window.AppendNormal(account.PrimaryGroup);
            else
                window.AppendNormal("N/A");
            window.AppendLineBreak();

            window.AppendHighlight("E-mail Address: ");
            window.AppendNormal(account.Email);
            window.AppendLineBreak();

            window.AppendHighlight("Post Count: ");
            window.AppendNormal(account.Posts.ToString());
            if (account.Rank != string.Empty)
                window.AppendNormal(" (" + account.Rank + ")");
            window.AppendLineBreak();

            window.AppendHighlight("Epeen: ");
            window.AppendNormal(account.Karma.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Enabled: ");
            if (account.Enabled)
            {
                window.AppendColorString(RichTextWindow.ColorGreen, "Yes");
            }
            else
            {
                window.AppendColorString(RichTextWindow.ColorRed, "No");
                if (username == e.Sender)
                {
                    window.AppendNormal(" [");
                    window.AppendBotCommand("Activate", "forum activate");
                    window.AppendNormal("]");
                }
            }
            window.AppendLineBreak();

            window.AppendHighlight("Registered On: ");
            window.AppendNormal(Format.DateTime(account.Registered, FormatStyle.Medium));
            window.AppendLineBreak();

            if (account.Seen > 0)
            {
                window.AppendHighlight("Last Seen: ");
                TimeSpan span = TimeStamp.ToDateTime(TimeStamp.Now) - TimeStamp.ToDateTime(account.Seen);
                window.AppendNormal(Format.Time(span, FormatStyle.Large) + " ago");
                window.AppendLineBreak();
            }

            if (account.Groups.Count > 0)
            {
                window.AppendLineBreak();
                window.AppendHeader("Additional Groups");
                foreach (string group in account.Groups.Values)
                {
                    window.AppendHighlight(group);
                    window.AppendLineBreak();
                }
            }
            bot.SendReply(e, username + "'s Forum Account »» " + window.ToString());
        }

        private void OnActivateCommand(BotShell bot, CommandArgs e)
        {
            // Beta process only allows clan, tl7, vhanet member
            if (bot.Dimension != AoLib.Net.Server.Atlantean)
            {
                bot.SendReply(e, "The activation process is currently disabled on this dimension. For more information read the public beta information on: http://www.vhabot.net/static.php?id=register");
                return;
            }
            if (e.SenderWhois == null)
            {
                bot.SendReply(e, "Unable to download the required information about your character");
                return;
            }
            if (!e.SenderWhois.Stats.Faction.Equals("clan", StringComparison.CurrentCultureIgnoreCase))
            {
                bot.SendReply(e, "Gridnet beta accounts are currently only available for members of the clan faction");
                return;
            }
            if (e.SenderWhois.Stats.Level < 205)
            {
                bot.SendReply(e, "Your character is required to be at least level 205 in order to participate in this beta");
                return;
            }
            if (this._host.IsAlt(e.Sender))
            {
                bot.SendReply(e, "Gridnet beta accounts can only be created using your registered main character");
                return;
            }
            if (!this._host.IsMember(e.Sender))
            {
                bot.SendReply(e, "Gridnet beta accounts are currently only available for members of the vhanet raid system");
                return;
            }

            // Validate input
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: activate [forum account] [forum password]");
                return;
            }
            ForumAccount account = this.GetAccount(e.Args[0]);
            if (account == null)
            {
                bot.SendReply(e, "Unable to locate the specified forum account");
                return;
            }
            if (!account.Enabled)
            {
                bot.SendReply(e, "Your forum account hasn't been verified yet. Please follow the instructions the forum gave you in order to verify your forum account");
                return;
            }

            // Validate password
            if (this.Sha1(e.Args[0].ToLower() + e.Args[1]) != account.Password)
            {
                bot.SendReply(e, "The password you supplied doesn't match the password of the specified forum account");
                return;
            }

            // Prepare account
            string dimension = null;
            switch (bot.Dimension)
            {
                case AoLib.Net.Server.Atlantean:
                    dimension = "rk1";
                    break;
                case AoLib.Net.Server.Rimor:
                    dimension = "rk2";
                    break;
                case AoLib.Net.Server.DieNeueWelt:
                    dimension = "dnw";
                    break;
                default:
                    bot.SendReply(e, "Gridnet accounts are not available for characters on your dimension");
                    return;
            }
            string gridnetAccount = e.Sender + " [" + dimension + "]";
            if (account.Username != account.Realname)
            {
                bot.SendReply(e, "Your forum account has already been modified and can't be used for this activation process");
                return;
            }

            // Check for duplicate accounts
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT memberName FROM smf_members WHERE realName = '" + Config.EscapeString(gridnetAccount) + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        bot.SendReply(e, "Your character already has been linked to the following forum account: " + HTML.CreateColorString(bot.ColorHeaderHex, reader.GetString(0)));
                        reader.Close();
                        return;
                    }
                    reader.Close();
                }
            }

            // Activate gridnet account
            this._database.ExecuteNonQuery("UPDATE smf_members SET realName = '" + Config.EscapeString(gridnetAccount) + "' WHERE memberName = '" + Config.EscapeString(account.Username) + "'");
            bot.SendReply(e, "Your gridnet account " + HTML.CreateColorString(bot.ColorHeaderHex, gridnetAccount) + " has been activated");
        }

        private void OnRegisterCommand(BotShell bot, CommandArgs e)
        {
            bot.SendReply(e, "The registration process is currently disabled. For more information read the public beta information on: http://www.vhabot.net/static.php?id=register");
        }

        public ForumAccount GetAccount(string username)
        {
            ForumAccount account = null;
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT ID_MEMBER, memberName, dateRegistered, posts, ID_GROUP, lastLogin, instantMessages, unreadMessages, emailAddress, ";
                    command.CommandText += "showOnline, karmaBad, karmaGood, memberIP, memberIP2, is_activated, additionalGroups, ID_POST_GROUP, totalTimeLoggedIn, realName, passwd ";
                    command.CommandText += "FROM smf_members WHERE memberName = '" + Config.EscapeString(username) + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        account = new ForumAccount();
                        account.ID = reader.GetInt32(0);

                        account.Username = reader.GetString(1);
                        account.Registered = reader.GetInt32(2);
                        account.Posts = reader.GetInt32(3);
                        account.PrimaryGroupID = reader.GetInt32(4);
                        account.Seen = reader.GetInt32(5);
                        account.Messages = reader.GetInt32(6);
                        account.MessagesUnread = reader.GetInt32(7);
                        account.Email = reader.GetString(8);
                        account.ShowOnline = (reader.GetInt32(9) > 0);
                        account.KarmaBad = reader.GetInt32(10);
                        account.KarmaGood = reader.GetInt32(11);
                        account.IP = reader.GetString(12);
                        account.IP2 = reader.GetString(13);
                        account.Enabled = (reader.GetInt32(14) == 1);
                        account.RankID = reader.GetInt32(16);
                        account.TotalTime = reader.GetInt32(17);
                        account.Realname = reader.GetString(18);
                        account.Password = reader.GetString(19);
                        account.Groups = new Dictionary<string, string>();
                        foreach (string group in reader.GetString(15).Split(','))
                            account.Groups.Add(group.Trim().Trim(','), "");

                        reader.Close();
                        if (account.PrimaryGroupID > 0)
                        {
                            command.CommandText = "SELECT groupName FROM smf_membergroups WHERE ID_GROUP = " + account.PrimaryGroupID;
                            reader = command.ExecuteReader();
                            if (reader.Read())
                                account.PrimaryGroup = reader.GetString(0);
                            reader.Close();
                        }
                        if (account.RankID > 0)
                        {
                            command.CommandText = "SELECT groupName FROM smf_membergroups WHERE ID_GROUP = " + account.RankID;
                            reader = command.ExecuteReader();
                            if (reader.Read())
                                account.Rank = reader.GetString(0);
                            reader.Close();
                        }
                        Dictionary<string, string> groups = new Dictionary<string, string>();
                        foreach (string group in account.Groups.Keys)
                        {
                            command.CommandText = "SELECT groupName FROM smf_membergroups WHERE ID_GROUP = '" + Config.EscapeString(group) + "'";
                            reader = command.ExecuteReader();
                            if (reader.Read())
                                groups.Add(group, reader.GetString(0));
                            reader.Close();
                        }
                        account.Groups = groups;
                    }
                    else
                    {
                        reader.Close();
                    }
                }
            }
            return account;
        }

        public class ForumAccount
        {
            public int ID;
            public string Username;
            public string Realname;
            public string Password;
            public bool Enabled = false;
            public Int64 Registered;
            public Int64 Seen;
            public int PrimaryGroupID = 0;
            public string PrimaryGroup = string.Empty;
            public bool Admin { get { return (this.PrimaryGroupID == 1); } }
            public int KarmaGood = 0;
            public int KarmaBad = 0;
            public int Karma { get { return this.KarmaGood - this.KarmaBad; } }
            public int Posts = 0;
            public string Email;
            public int TotalTime = 0;
            public bool ShowOnline = false;
            public bool Online = false;
            public Dictionary<string, string> Groups;
            public int RankID = 0;
            public string Rank = string.Empty;
            public int Messages;
            public int MessagesUnread;
            public string IP;
            public string IP2;
        }

        public string Sha1(string input)
        {
            if (input == null) return string.Empty;
            SHA1Managed hasher = new SHA1Managed();
            byte[] buffer = Encoding.UTF8.GetBytes(input);
            byte[] hashed = hasher.ComputeHash(buffer);
            return this.BytesToHex(hashed);
        }

        public string BytesToHex(byte[] bytes)
        {
            string output = "";
            foreach (byte input in bytes)
                output += input.ToString("X2");
            return output.ToLower();
        }
    }
}
