using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;
using VhaBot;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class RaidRules : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;

        public RaidRules()
        {
            this.Name = "Raid :: Rules";
            this.InternalName = "RaidRules";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Shows the official rules for this bot";
            this.Commands = new Command[] {
                new Command("rules", true, UserLevel.Guest),
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }

            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS rules (id INT NOT NULL AUTO_INCREMENT, title VARCHAR(255), rule TEXT NOT NULL, visible ENUM('true','false') NOT NULL, PRIMARY KEY (id))");
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            lock (this._database.Connection)
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT title, rule FROM rules WHERE visible = 'true'";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        window.AppendHeader(reader.GetString(0));
                        string rule = reader.GetString(1);
                        rule = rule.Replace("[b]", bot.ColorNormal);
                        rule = rule.Replace("[/b]", HTML.CreateColorEnd());
                        window.AppendHighlightStart();
                        window.AppendRawString(rule);
                        window.AppendColorEnd();
                        window.AppendLineBreak(2);
                    }
                    reader.Close();
                }
            }
            bot.SendReply(e, "Rules »» ", window);
        }
        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "rules":
                    return "Displays the official rules of this bot.\n" +
                        "Usage: /tell " + bot.Character + " rules";
            }
            return null;
        }
    }
}
