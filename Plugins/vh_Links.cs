using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class Links : PluginBase
    {
        private Config _database;
        public Links()
        {
            this.Name = "Links and Bookmarks";
            this.InternalName = "vhLinks";
            this.Author = "Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("links", true, UserLevel.Member),
                new Command("links add", true, UserLevel.Admin),
                new Command("links remove", true, UserLevel.Admin),
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS links (id INTEGER PRIMARY KEY AUTOINCREMENT, category VARCHAR(255), link VARCHAR(255) UNIQUE, description TEXT)");
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "links":
                    this.OnLinksCommand(bot, e);
                    break;
                case "links add":
                    this.OnLinksAddCommand(bot, e);
                    break;
                case "links remove":
                    this.OnLinksRemoveCommand(bot, e);
                    break;
            }
        }

        public void OnLinksCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            int links = 0;
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT id, category, link, description FROM links ORDER BY category, description";
                IDataReader reader = command.ExecuteReader();
                string lastCategory = "";
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string category = reader.GetString(1);
                    string link = reader.GetString(2);
                    string description = reader.GetString(3);
                    if (category != lastCategory)
                    {
                        if (links > 0) window.AppendLineBreak();
                        window.AppendHeader(category);
                        lastCategory = category;
                    }
                    window.AppendHighlight(description);
                    window.AppendNormalStart();
                    window.AppendString(" [");
                    window.AppendCommand("Visit", "/start " + link);
                    window.AppendString("] [");
                    window.AppendBotCommand("Remove", "links remove " + id);
                    window.AppendString("]");
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                    links++;
                }
            }
            if (links > 0)
                bot.SendReply(e, "Links and Bookmarks »» ", window);
            else
                 bot.SendReply(e, "There are currently no links in the database");
        }

        public void OnLinksAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 3 || (!e.Args[1].StartsWith("http://") && !e.Args[1].StartsWith("https://")))
            {
                bot.SendReply(e, "Correct Usage: links add [category] [link] [description]");
                return;
            }
            string category = Config.EscapeString(e.Args[0].Replace('_', ' '));
            string url = Config.EscapeString(e.Args[1].Replace("'", "").Replace("\"", ""));
            string description = Config.EscapeString(HTML.EscapeString(e.Words[2]));
            int result = this._database.ExecuteNonQuery("INSERT INTO links (category, link, description) VALUES ('" + category + "', '" + url + "', '" + description + "')");
            if (result > 0)
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, e.Args[1]) + " has been added to the links database");
            else
                bot.SendReply(e, "An error has occurred while adding the link to the database");
        }

        public void OnLinksRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: links remove [id]");
                return;
            }
            string id = Config.EscapeString(e.Args[0]);
            int result = this._database.ExecuteNonQuery("DELETE FROM links WHERE id = '" + id + "'");
            if (result > 0)
                bot.SendReply(e, "Link " + HTML.CreateColorString(bot.ColorHeaderHex, "#" + e.Args[0]) + " has been removed from the links database");
            else
                bot.SendReply(e, "Unable to remove that link from the database");
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "links":
                    return "Displays all links and bookmarks currently in the bot's database.\n" +
                        "Usage: /tell " + bot.Character + " links";
                case "links add":
                    return "Allows you to add a new link or bookmark to the database.\n" +
                        "Usage: /tell " + bot.Character + " links add [category] [link] [description]\n" +
                        "Example: /tell " + bot.Character + " links add Bots http://www.vhabot.net VhaBot - Multi-Bot System";
                case "links remove":
                    return "Allows you to remove a link from the database.\n" +
                        "Usage: /tell " + bot.Character + " links remove [id]";
            }
            return null;
        }
    }
}
