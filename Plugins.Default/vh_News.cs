using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class NewsPlugin : PluginBase
    {
        private Config _database;
        private bool _sendLogon = false;
        private CultureInfo _cultureInfo = new CultureInfo("en-GB");
        private DateTimeFormatInfo _dtfi = new CultureInfo("en-US", false).DateTimeFormat;
        /* Registers The Icons People can use */
        private string[] _iconList = new string[] {
            "GFX_GUI_WINDOW_ICON_I",
            "GFX_GUI_WINDOW_ICON_FACTIONS",
            "GFX_GUI_WINDOW_ICON_HELP",
            "GFX_GUI_WINDOW_ICON_MAP",
            "GFX_GUI_WINDOW_ICON_NCU",
            "GFX_GUI_WINDOW_ICON_PERKS",
            "GFX_GUI_WINDOW_ICON_POPUP",
            "GFX_GUI_MAP_GUARDTOWER_BLUE",
            "GFX_GUI_MAP_GUARDTOWER_GREY",
            "GFX_GUI_MAP_GUARDTOWER_RED",
            "GFX_GUI_MAP_BUFFTOWER_BLUE",
            "GFX_GUI_MAP_BUFFTOWER_GREY",
            "GFX_GUI_MAP_BUFFTOWER_RED",
            "GFX_GUI_MAP_CONTROLLER_BLUE",
            "GFX_GUI_MAP_CONTROLLER_GREY",
            "GFX_GUI_MAP_CONTROLLER_RED"
        };


        public NewsPlugin()
        {
            this.Name = "News";
            this.InternalName = "NewsPlugin";
            this.Version = 2;
            this.Author = "Iriche & Arys";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("news icon", false, UserLevel.Disabled, UserLevel.Leader, UserLevel.Disabled),
                new Command("news sticky", false, UserLevel.Disabled, UserLevel.Admin, UserLevel.Disabled),
                new Command("news add", true, UserLevel.Leader),
                new Command("news remove", true, UserLevel.Disabled, UserLevel.Leader, UserLevel.Disabled),
                new Command("news id", false, UserLevel.Leader),
                new Command("news", true, UserLevel.Member, UserLevel.Member, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Events.UserLogonEvent += new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);

            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS news (news_id integer PRIMARY KEY AUTOINCREMENT, news_name varchar (14), news_date integer, news_text varchar (500), news_icon integer DEFAULT 0, news_sticky integer DEFAULT 0)");

            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "send_logon", "Send news on Logon", this._sendLogon);
            this._sendLogon = bot.Configuration.GetBoolean(this.InternalName, "send_logon", this._sendLogon);
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
            {
                this._sendLogon = bot.Configuration.GetBoolean(this.InternalName, "send_logon", this._sendLogon);
            }
        }

        private void Events_UserLogonEvent(BotShell bot, UserLogonArgs e)
        {
            if (this._sendLogon && e.Sections.Contains("notify")) {
                CommandArgs args = new CommandArgs(CommandType.Tell, 0, e.SenderID, e.Sender, e.SenderWhois, "news", "", false, null);
                this.OnNews(bot, args);
            }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "news add":
                    this.OnNewsAdd(bot, e);
                    break;
                case "news remove":
                    this.OnNewsRemove(bot, e);
                    break;
                case "news id":
                    this.OnNewsID(bot, e);
                    break;
                case "news icon":
                    this.OnNewsIcon(bot, e);
                    break;
                case "news sticky":
                    this.OnNewsSticky(bot, e);
                    break;
                case "news":
                    this.OnNews(bot, e);
                    break;
            }
        }

        private void OnNewsIcon(BotShell bot, CommandArgs e) {
            double d;
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: news icon [id] [icon]");
                return;
            }
            if (double.TryParse(e.Args[0], System.Globalization.NumberStyles.Integer, _cultureInfo, out d) == true && double.TryParse(e.Args[1], System.Globalization.NumberStyles.Integer, _cultureInfo, out d) == true)
            {
                if (_iconList.Length > Convert.ToInt32(e.Args[1]) && Convert.ToInt32(e.Args[1]) >= 0)
                {
                    try
                    {
                        using (IDbCommand command = this._database.Connection.CreateCommand())
                        {
                            if (bot.Users.Authorized(e.Sender, UserLevel.Leader))
                            {
                                command.CommandText = "UPDATE [news] SET [news_icon] = " + e.Args[1] + " WHERE [news_id] = " + e.Args[0];
                            }
                            else
                            {
                                command.CommandText = "UPDATE [news] SET [news_icon] = " + e.Args[1] + " WHERE [news_id] = " + e.Args[0] + " AND [news_name] = '" + e.Sender + "'";
                            }
                            command.ExecuteNonQuery();
                            bot.SendReply(e, "News icon updated");
                            return;
                        }
                    }
                    catch
                    {
                        bot.SendReply(e, "Error during news updating. Please try again later");
                        return;
                    }
                }
                else
                {
                    bot.SendReply(e, "Invalid icon");
                    return;
                }
            }
            bot.SendReply(e, "Invalid input");
            return;
        }

        private void OnNewsID(BotShell bot, CommandArgs e) {
            double d;
            if (e.Args.Length == 0)
            {
                bot.SendReply(e, "Correct Usage: news id [id]");
                return;
            }
            if (double.TryParse(e.Args[0], System.Globalization.NumberStyles.Integer, _cultureInfo, out d) == true)
            {
                try
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT [news_name], [news_sticky] FROM [news] WHERE [news_id] = " + e.Args[0];
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            if ((reader.GetString(0) == e.Sender || bot.Users.Authorized(e.Sender, UserLevel.Admin)) && e.Type == CommandType.Tell)
                            {
                                RichTextWindow window = new RichTextWindow(bot);
                                window.AppendTitle("News Options");
                                window.AppendHighlight("Sticky: ");
                                if (reader.GetInt64(1) == 1)
                                {
                                    window.AppendColorString(RichTextWindow.ColorGreen, "Enabled");
                                    window.AppendNormal(" [");
                                    window.AppendBotCommand("Disable", "news sticky " + e.Args[0] + " 0");
                                    window.AppendNormal("]");
                                }
                                else
                                {
                                    window.AppendColorString(RichTextWindow.ColorOrange, "Disabled");
                                    window.AppendNormal(" [");
                                    window.AppendBotCommand("Enable", "news sticky " + e.Args[0] + " 1");
                                    window.AppendNormal("]");
                                }
                                window.AppendLineBreak(2);

                                window.AppendHeader("Icon");
                                if (reader.GetInt64(1) == 1)
                                {
                                    window.AppendNormal("There are no icons available for sticky posts");
                                }
                                else
                                {
                                    int counter = 0;
                                    foreach (string Icon in _iconList)
                                    {
                                        window.AppendBotCommandStart("news icon " + e.Args[0] + " " + counter.ToString());
                                        window.AppendImage(Icon);
                                        window.AppendLinkEnd();
                                        window.AppendNormal(" ");
                                        counter = counter + 1;
                                    }
                                }
                                bot.SendReply(e, "News Options »» ", window);
                                return;
                            }
                            else
                            {
                                bot.SendReply(e, "You don't own this news post");
                                return;
                            }
                        }
                        else
                        {
                            bot.SendReply(e, "No such news post");
                            return;
                        }
                    }
                }
                catch
                {
                    bot.SendReply(e, "Error during news fetching. Please try again later");
                    return;
                }

            }
            bot.SendReply(e, "Invalid ID");
        }

        private void OnNewsRemove(BotShell bot, CommandArgs e) {
            double d;
            if (e.Args.Length == 0)
            {
                bot.SendReply(e, "Correct Usage: news remove [id]");
                return;
            }
            if (double.TryParse(e.Args[0], System.Globalization.NumberStyles.Integer, _cultureInfo, out d) == true)
            {
                if (e.Args.Length == 2 && e.Args[1] == "confirm")
                {
                    try
                    {
                        using (IDbCommand command = this._database.Connection.CreateCommand())
                        {
                            if (bot.Users.Authorized(e.Sender, UserLevel.Admin))
                            {
                                command.CommandText = "DELETE FROM [news] WHERE [news_id] = " + e.Args[0];
                            }
                            else
                            {
                                command.CommandText = "DELETE FROM [news] WHERE [news_id] = " + e.Args[0] + " AND [news_name] = '" + e.Sender + "'";
                            }
                            if (command.ExecuteNonQuery() > 0)
                                bot.SendReply(e, "News post succesfully removed");
                            else
                                bot.SendReply(e, "No news post removed");
                            return;
                        }
                    }
                    catch
                    {
                        bot.SendReply(e, "Error during news removing. Please try again later");
                        return;
                    }
                }
                try
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "SELECT [news_name] FROM [news] WHERE [news_id] = " + e.Args[0];
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            if ((reader.GetString(0) == e.Sender || bot.Users.Authorized(e.Sender, UserLevel.Admin)) && e.Type == CommandType.Tell)
                            {
                                bot.SendReply(e, "This command will permanently remove this news post. If you wish to continue use: /tell " + bot.Character + " news remove " + e.Args[0] + " confirm");
                                return;
                            }
                            else
                            {
                                bot.SendReply(e, "You're not the owner of this post");
                                return;
                            }
                        }
                    }
                }
                catch { }
                bot.SendReply(e, "Unable to remove this news post");
                return;
            }
            bot.SendReply(e, "Invalid ID");
            return;
        }

        private void OnNewsAdd(BotShell bot, CommandArgs e) {
            if (e.Args.Length == 0)
            {
                bot.SendReply(e, "Correct Usage: news add [news]");
                return;
            }
            try
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [news] (news_name, news_date, news_text) VALUES ('" + e.Sender + "', " + TimeStamp.Now + ", '" + Config.EscapeString(e.Words[0].Replace("\\n", "\n")) + "')";
                    IDataReader reader = command.ExecuteReader();
                }
                bot.SendReply(e, "News posted");
            }
            catch
            {
                bot.SendReply(e, "Error during news posting. Please try again later");
            }
        }

        private void OnNewsSticky(BotShell bot, CommandArgs e) {
            double d;
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: news sticky [id] [sticky]");
                return;
            }
            if (double.TryParse(e.Args[0], System.Globalization.NumberStyles.Integer, _cultureInfo, out d) == true && double.TryParse(e.Args[1], System.Globalization.NumberStyles.Integer, _cultureInfo, out d) == true)
            {
                if (Convert.ToInt32(e.Args[1]) == 0 || Convert.ToInt32(e.Args[1]) == 1)
                {
                    try
                    {
                        using (IDbCommand command = this._database.Connection.CreateCommand())
                        {
                            command.CommandText = "UPDATE [news] SET [news_sticky] = " + e.Args[1] + " WHERE [news_id] = " + e.Args[0];
                            command.ExecuteNonQuery();
                            bot.SendReply(e, "News sticky updated");
                            return;
                        }
                    }
                    catch
                    {
                        bot.SendReply(e, "Error during news updating. Please try again later");
                        return;
                    }
                }
                bot.SendReply(e, "Invalid sticky ID");
                return;
            }
            bot.SendReply(e, "Invalid input");
            return;
        }

        private void OnNews(BotShell bot, CommandArgs e) {
            try
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    RichTextWindow window = new RichTextWindow(bot);
                    window.AppendTitle();
                    bool titleSticky = false;
                    bool titleNormal = false;
                    Int64 lastPost = 0;
                    command.CommandText = "SELECT [news_id], [news_name] , [news_date], [news_text], [news_icon], [news_sticky] FROM [news] ORDER BY [news_sticky] DESC, [news_date] DESC LIMIT 15";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader.GetInt64(2) > lastPost)
                            lastPost = reader.GetInt64(2);
                        if (reader.GetInt64(5) == 1)
                        {
                            if (!titleSticky)
                            {
                                window.AppendHeader("Sticky News Posts");
                                window.AppendLineBreak();
                                titleSticky = true;
                            }
                        }
                        else
                        {
                            if (!titleNormal)
                            {
                                window.AppendHeader("Current News Posts");
                                window.AppendLineBreak();
                                titleNormal = true;
                            }
                        }
                        window.AppendBotCommandStart("news id " + reader.GetInt64(0).ToString());
                        if (reader.GetInt64(5) == 1)
                            window.AppendImage("GFX_GUI_WINDOW_ICON_POPUP");
                        else
                            window.AppendImage(_iconList[reader.GetInt64(4)]);
                        window.AppendLinkEnd();
                        window.AppendHighlight(" " + Format.DateTime(reader.GetInt64(2), FormatStyle.Compact) + " GMT by " + reader.GetString(1));
                        window.AppendLineBreak(true);
                        window.AppendNormal(reader.GetString(3));
                        if ((reader.GetString(1) == e.Sender || bot.Users.Authorized(e.Sender, UserLevel.Admin)))
                        {
                            window.AppendLineBreak(true);
                            window.AppendNormal("[");
                            window.AppendBotCommand("Remove", "news remove " + reader.GetInt64(0));
                            window.AppendNormal("]");
                        }
                        window.AppendLineBreak(2);
                    }
                    if (lastPost == 0)
                    {
                        bot.SendReply(e, "There's currently no news");
                        return;
                    }
                    else
                    {
                        bot.SendReply(e, "News last edited on " + HTML.CreateColorString(bot.ColorHeaderHex, Format.DateTime(lastPost, FormatStyle.Compact)) + " »» ", window);
                        return;
                    }
                }
            }
            catch
            {
                bot.SendReply(e, "Error during news fetching. Please try again later");
                return;
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "news":
                    return "Displays the current news.\nYou can also manage the news posts from this page by clicking on the icon next to the news header.\n" +
                        "After clicking on the icon you will go to a new page that allows you to change the icon and sticky the news post.\n" +
                        "Usage: /tell " + bot.Character + " news";
                case "news add":
                    return "Allows you to post a new news article.\n" +
                        "Usage: /tell " + bot.Character + " news add [message]";
                case "news remove":
                    return "Allows you to remove a news article\n" +
                        "Usage: /tell " + bot.Character + " news remove [id]";
            }
            return null;
        }
    }
}
