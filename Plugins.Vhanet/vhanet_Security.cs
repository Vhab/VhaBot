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
    public class VhanetSecurity : PluginBase
    {
        private VhanetDatabase _database;
        private VhanetMembersHost _host;

        public VhanetSecurity()
        {
            this.Name = "Vhanet :: Security";
            this.InternalName = "VhanetSecurity";
            this.Version = 100;
            this.Author = "Vhab";
            this.Dependencies = new string[] { "VhanetDatabase", "VhanetMembersHost" };
            this.DefaultState = PluginState.Disabled;
            this.Description = "Generates reports on reroll characters and faction changes";
            this.Commands = new Command[] {
                new Command("security", false, UserLevel.Admin),
                new Command("security fixid", false, UserLevel.Admin)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._database = (VhanetDatabase)bot.Plugins.GetPlugin("VhanetDatabase"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Database' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            try { this._host = (VhanetMembersHost)bot.Plugins.GetPlugin("VhanetMembersHost"); }
            catch { throw new Exception("Unable to connect to 'Vhanet :: Members Host' Plugin!"); }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (!this._database.Connected)
            {
                bot.SendReply(e, "Unable to connect to the database. Please try again later");
                return;
            }
            switch (e.Command)
            {
                case "security":
                    this.OnSecurityCommand(bot, e);
                    break;
                case "security fixid":
                    this.OnSecurityFixidCommand(bot, e);
                    break;
            }
        }
        private void OnSecurityFixidCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: security fixid [username]");
                return;
            }
            string username = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            UInt32 userID = bot.GetUserID(username);
            if (userID < 1)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            if (this._host.IsAlt(username))
            {
                this._database.ExecuteNonQuery("UPDATE alts SET altID = " + userID + " WHERE altname = '" + username + "'");
                bot.SendReply(e, "Updated " + HTML.CreateColorString(bot.ColorHeaderHex, username + "'s") + " character id to " + HTML.CreateColorString(bot.ColorHeaderHex, userID.ToString()));
                return;
            }
            if (this._host.IsMember(username))
            {
                this._database.ExecuteNonQuery("UPDATE members SET userID = " + userID + " WHERE username = '" + username + "'");
                bot.SendReply(e, "Updated " + HTML.CreateColorString(bot.ColorHeaderHex, username + "'s") + " character id to " + HTML.CreateColorString(bot.ColorHeaderHex, userID.ToString()));
                return;
            }
            bot.SendReply(e, "Unable to fix " + HTML.CreateColorString(bot.ColorHeaderHex, username));
        }

        private void OnSecurityCommand(BotShell bot, CommandArgs e)
        {
            bot.SendReply(e, "Gathering Data. This can take several minutes. Please stand by...");
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Invalid ID's / Rerolled Characters");
            List<SecurityProcessItem> members = new List<SecurityProcessItem>();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT username as name, userID as id, 'Main' as type FROM members UNION SELECT altname, altID, 'Alt' FROM alts ORDER BY name";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        string username = reader.GetString(0);
                        UInt32 userID = (UInt32)reader.GetInt64(1);
                        string type = reader.GetString(2);
                        members.Add(new SecurityProcessItem(username, userID, type));
                    }
                    reader.Close();
                }
            }
            foreach (SecurityProcessItem member in members)
            {
                UInt32 realID = bot.GetUserID(member.Username);
                if (realID == 0)
                {
                    window.AppendHighlight(member.Username);
                    window.AppendNormalStart();
                    window.AppendString(" (" + member.Type + ") (");
                    window.AppendColorString(RichTextWindow.ColorRed, "Deleted");
                    window.AppendString(") [");
                    window.AppendBotCommand("Account", "account " + member.Username);
                    window.AppendString("]");
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                }
                else if (realID != member.UserID)
                {
                    window.AppendHighlight(member.Username);
                    window.AppendNormalStart();
                    window.AppendString(" (Current=" + member.UserID + " Real=" + realID + ") (" + member.Type + ") (");
                    window.AppendColorString(RichTextWindow.ColorOrange, "Invalid ID");
                    window.AppendString(") [");
                    window.AppendBotCommand("Account", "account " + member.Username);
                    window.AppendString("] [");
                    window.AppendBotCommand("Fix", "security fixid " + member.Username);
                    window.AppendString("]");
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                }
            }
            bot.SendReply(e, "Security Report »» ", window);
        }

        public class SecurityProcessItem
        {
            public readonly string Username;
            public readonly UInt32 UserID;
            public readonly string Type;

            public SecurityProcessItem(string username, UInt32 userID, string type)
            {
                this.Username = username;
                this.UserID = userID;
                this.Type = type;
            }
        }
    }
}
