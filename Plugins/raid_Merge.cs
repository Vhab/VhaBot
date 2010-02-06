using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidMerge : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private RaidMembers _members;
        private BotShell _bot;

        public RaidMerge()
        {
            this.Name = "Raid :: Account Merger";
            this.InternalName = "RaidMerge";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore", "RaidMembers" };
            this.Description = "Allows you to merge/fix accounts";
            this.Commands = new Command[] {
                new Command("merge", true, UserLevel.Admin)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
            try { this._members = (RaidMembers)bot.Plugins.GetPlugin("RaidMembers"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Members' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: merge [username]");
                return;
            }
            string[] alts = bot.Users.GetAlts(e.Args[0]);
            if (alts.Length < 1)
            {
                bot.SendReply(e, "You're required to specify an account with alts in order to start the merge process");
                return;
            }
            string main = Config.EscapeString(Format.UppercaseFirst(e.Args[0]));
            lock (this)
            {
                bot.SendReply(e, "Now merging/fixing " + HTML.CreateColorString(bot.ColorHeaderHex, main + "'s") + " account...");
                this.Log(main, e.Sender + " has started an account merge");

                // Points
                int points = (int)(this._core.GetPoints(main) * 10);
                if (this._core.GetPoints(main) < this._core.MinimumPoints)
                    points = 0;
                int activity = this._core.GetActivity(main);
                if (activity < 0)
                    activity = 0;
                this.Log(main, main + "'s Old Raw Points: " + points);
                this.Log(main, main + "'s Old Activity: " + activity);
                foreach (string alt in alts)
                {
                    lock (this._database.Connection)
                    {
                        using (IDbCommand command = this._database.Connection.CreateCommand())
                        {
                            command.CommandText = "SELECT points, activity FROM points WHERE main = '" + alt + "'";
                            IDataReader reader = command.ExecuteReader();
                            if (reader.Read())
                            {
                                int tmpPoints = reader.GetInt32(0);
                                int tmpActivity = reader.GetInt32(1);
                                this.Log(main, alt + "'s Old Raw Points: " + tmpPoints);
                                this.Log(main, alt + "'s Old Activity: " + tmpActivity);
                                points += tmpPoints;
                                activity += tmpActivity;
                            }
                            reader.Close();
                        }
                    }
                }
                this._database.ExecuteNonQuery("REPLACE INTO points SET main = '" + main + "', points = " + points + ", activity = " + activity);
                this.Log(main, "New Raw Points: " + points);
                this.Log(main, "New Activity: " + activity);
                foreach (string alt in alts)
                {
                    this.Log(main, "Removing points account " + alt);
                    this._database.ExecuteNonQuery("DELETE FROM points WHERE main = '" + alt + "'");
                }

                /*// Credits
                int credits = this._members.GetCredits(main);
                if (credits < 0)
                    this.Log(main, "No credits history");
                else
                    this.Log(main, "Old Credits: " + credits);
                foreach (string alt in alts)
                {
                    lock (this._database.Connection)
                    {
                        using (IDbCommand command = this._database.Connection.CreateCommand())
                        {
                            command.CommandText = "SELECT credits FROM credits WHERE main = '" + alt + "'";
                            IDataReader reader = command.ExecuteReader();
                            if (reader.Read())
                            {
                                int tmpCredits = reader.GetInt32(0);
                                this.Log(main, alt + ": Old Credits: " + tmpCredits);
                                if (tmpCredits < credits)
                                    credits = tmpCredits;
                                else if (credits < 0)
                                    credits = tmpCredits;
                            }
                            reader.Close();
                        }
                    }
                }
                if (credits >= 0)
                {
                    this.Log(main, "New Credits: " + credits);
                    this._database.ExecuteNonQuery("INSERT INTO credits (`main`, `credits`, `lastRaidID`) VALUES ('" + main + "', " + credits + ", 0) ON DUPLICATE KEY UPDATE credits = " + credits);
                    foreach (string alt in alts)
                    {
                        this.Log(main, "Removing credits account " + alt);
                        this._database.ExecuteNonQuery("DELETE FROM credits WHERE main = '" + alt + "'");
                    }
                }
                else
                {
                    this.Log(main, "No credits change");
                }*/

                // Update raid history
                foreach (string alt in alts)
                {
                    this.Log(main, "Converting raid history from " + alt);
                    int tmpAffected = this._database.ExecuteNonQuery("UPDATE raiders SET main = '" + main + "' WHERE main = '" + alt + "'");
                    this.Log(main, "Converted: " + tmpAffected + " entries");
                }

                // Update loot history
                foreach (string alt in alts)
                {
                    this.Log(main, "Converting loot history from " + alt);
                    int tmpAffected = this._database.ExecuteNonQuery("UPDATE loot_history SET main = '" + main + "' WHERE main = '" + alt + "'");
                    this.Log(main, "Converted: " + tmpAffected + " entries");
                }
                this.Log(main, "Merge completed");
                this.Log(main, "-----------------------------------------");
                bot.SendReply(e, "Merging/fixing " + HTML.CreateColorString(bot.ColorHeaderHex, main + "'s") + " account done");
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "merge":
                    return "Allows you to merge accounts.\n" +
                        "WARNING: This process is not reversible!\n" +
                        "Before merging an account, update the account so it reflects the correct structure of main and alts.\n" +
                        "After you finished adding/remove members/alts use the merge command.\n" +
                        "Usage: /tell " + bot.Character + " merge [username]";
            }
            return null;
        }

        private void Log(string account, string message)
        {
            string log = string.Format("merge log.txt");
            string path = "logs" + Path.DirectorySeparatorChar + this._bot.ToString();
            message = message.Trim();

            StreamWriter writer = null;
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                string file = path + Path.DirectorySeparatorChar + log;
                writer = new StreamWriter(file, true);
                writer.WriteLine("{0:yyyy}-{0:MM}-{0:dd}\t{1:00}:{2:00}:{3:00}\t{4}\t{5}", DateTime.Now, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, account, message);
            }
            catch { }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
    }
}
