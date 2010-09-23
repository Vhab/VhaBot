using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Timers;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class Quotes : PluginBase
    {
        private Config _database;
        private Timer _timer;
        private BotShell _bot;
        private bool _autosend = false;
        private int _senddelay = 60;
        private bool _sendgc = true;
        private bool _sendpg = true;

        public Quotes()
        {
            this.Name = "Quotes Repository";
            this.InternalName = "vhQuotes";
            this.Author = "Tsuyoi";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Description = "A repository for storing and retrieving quotes.";
            this.Commands = new Command[] {
                new Command("quote", true, UserLevel.Guest),
                new Command("quotes", "quote"),
                new Command("quote add", true, UserLevel.Member),
                new Command("quotes add", "quote add"),
                new Command("quote remove", true, UserLevel.Leader),
                new Command("quote rem", "quote remove"),
                new Command("quote del", "quote remove")
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS quotes (id integer PRIMARY KEY AUTOINCREMENT, quote VARCHAR(500), contributor VARCHAR(15))");
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(ConfigurationChangedEvent);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "autosend", "Automatically send quotes to chat periodically", this._autosend);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "senddelay", "Time Delay on sending quotes to chat (in minutes)", this._senddelay, 15, 30, 60, 120);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendgc", "Send notifications to the organization channel", this._sendgc);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendpg", "Send notifications to the private channel", this._sendpg);
            this._timer = new Timer(this._senddelay * 60000);
            this._timer.AutoReset = true;
            this._timer.Elapsed += new ElapsedEventHandler(this.OnTimerTick);
            this.LoadConfiguration(bot);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(ConfigurationChangedEvent);
            if (this._timer != null)
            {
                this._timer.Stop();
                this._timer.Dispose();
            }
            this._database.Close();
        }

        private void ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section != this.InternalName) return;
            this.LoadConfiguration(bot);
        }

        private void LoadConfiguration(BotShell bot)
        {
            this._autosend = bot.Configuration.GetBoolean(this.InternalName, "autosend", this._autosend);
            this._senddelay = bot.Configuration.GetInteger(this.InternalName, "senddelay", this._senddelay);
            this._sendgc = bot.Configuration.GetBoolean(this.InternalName, "sendgc", this._sendgc);
            this._sendpg = bot.Configuration.GetBoolean(this.InternalName, "sendpg", this._sendpg);
            if (!this._autosend)
            {
                this._timer.Stop();

            }
            else
            {
                this._timer = new Timer(this._senddelay * 60000);
                this._timer.AutoReset = true;
                this._timer.Elapsed += new ElapsedEventHandler(this.OnTimerTick);
                this._timer.Start();
            }
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {

                    command.CommandText = "SELECT [id] FROM [quotes] ORDER BY [id] DESC";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        bool found = false;
                        int Max = (int)reader.GetInt64(0);
                        reader.Close();
                        if (Max > 1)
                        {
                            while (found == false)
                            {
                                Random random = new Random();
                                int randID = random.Next(1, Max);
                                command.CommandText = "SELECT [quote], [contributor] FROM [quotes] WHERE id=" + randID;
                                reader = command.ExecuteReader();
                                if (reader.Read())
                                {
                                    string message = "#" + randID.ToString() + " - " + reader.GetString(0) + " [Contributed By: " + reader.GetString(1) + "]";
                                    if (this._sendpg) { this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + message); }
                                    if (this._sendgc) { this._bot.SendOrganizationMessage(this._bot.ColorHighlight + message); }
                                    reader.Close();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            command.CommandText = "SELECT [quote], [contributor] FROM [quotes] WHERE id=1";
                            reader = command.ExecuteReader();
                            if (reader.Read())
                            {
                                string message = "#1 - " + reader.GetString(0) + " [Contributed By: " + reader.GetString(1) + "]";
                                if (this._sendpg) { this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + message); }
                                if (this._sendgc) { this._bot.SendOrganizationMessage(this._bot.ColorHighlight + message); }
                                reader.Close();
                                return;
                            }
                        }
                    }
                    else
                    {
                        string message = "No quotes are stored in the repository, please disable automatic quote spamming.";
                        if (this._sendpg) { this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + message); }
                        if (this._sendgc) { this._bot.SendOrganizationMessage(this._bot.ColorHighlight + message); }
                        return;
                    }
                }
            }
            catch
            {
                string message = "No quotes are stored in the repository, please disable automatic quote spamming.";
                if (this._sendpg) { this._bot.SendPrivateChannelMessage(this._bot.ColorHighlight + message); }
                if (this._sendgc) { this._bot.SendOrganizationMessage(this._bot.ColorHighlight + message); }
                return;
            }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "quote":
                    this.OnQuoteCommand(bot, e);
                    break;
                case "quote add":
                    this.OnQuoteAddCommand(bot, e);
                    break;
                case "quote remove":
                    this.OnQuoteRemoveCommand(bot, e);
                    break;
            }
        }

        private void OnQuoteCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length == 0)
            {
                try
                {
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {

                        command.CommandText = "SELECT [id] FROM [quotes] ORDER BY [id] DESC";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            bool found = false;
                            int Max = (int)reader.GetInt64(0);
                            reader.Close();
                            if (Max > 1)
                            {
                                while (found == false)
                                {
                                    Random random = new Random();
                                    int randID = random.Next(1, Max);
                                    command.CommandText = "SELECT [quote], [contributor] FROM [quotes] WHERE id=" + randID;
                                    reader = command.ExecuteReader();
                                    if (reader.Read())
                                    {
                                        bot.SendReply(e, "#" + randID.ToString() + " - " + reader.GetString(0) + " [Contributed By: " + reader.GetString(1) + "]");
                                        reader.Close();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                command.CommandText = "SELECT [quote], [contributor] FROM [quotes] WHERE id=1";
                                reader = command.ExecuteReader();
                                if (reader.Read())
                                {
                                    bot.SendReply(e, "#1 - " + reader.GetString(0) + " [Contributed By: " + reader.GetString(1) + "]");
                                    reader.Close();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            bot.SendReply(e, "No quotes are stored in the repository.");
                            return;
                        }
                    }
                }
                catch
                {
                    bot.SendReply(e, "No quotes are stored in the repository.");
                    return;
                }
            }
            else
            {
                int id = 0;
                if (int.TryParse(e.Args[0], out id))
                {
                    try
                    {
                        using (IDbCommand command = this._database.Connection.CreateCommand())
                        {
                            command.CommandText = "SELECT [quote], [contributor] FROM [quotes] WHERE id=" + id;
                            IDataReader reader = command.ExecuteReader();
                            if (reader.Read())
                            {
                                bot.SendReply(e, "#" + id + " - " + reader.GetString(0) + " [Contributed By: " + reader.GetString(1) + "]");
                                reader.Close();
                                return;
                            }
                            else
                            {
                                bot.SendReply(e, "No quote is registered for #" + id);
                                return;
                            }
                        }
                    }
                    catch
                    {
                        bot.SendReply(e, "No quotes are stored in the repository.");
                        return;
                    }
                }
            }
        }

        private void OnQuoteAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length > 0)
            {
                try
                {
                    int id = 0;
                    using (IDbCommand command = this._database.Connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO [quotes] (quote, contributor) VALUES ('" + Config.EscapeString(e.Words[0].Replace("\\n", "\n")) + "', '" + e.Sender + "')";
                        command.ExecuteNonQuery();
                        command.CommandText = "SELECT [id] FROM [quotes] WHERE quote = '" + Config.EscapeString(e.Words[0].Replace("\\n", "\n")) + "'";
                        IDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            id = (int)reader.GetInt64(0);
                        }
                        reader.Close();
                    }
                    bot.SendReply(e, "Your quote has been added as #" + id);
                    return;
                }
                catch
                {
                    bot.SendReply(e, "Error adding quote.");
                    return;
                }
            }
            else
            {
                bot.SendReply(e, "Correct Usage: quote add [quote]");
                return;
            }
        }

        private void OnQuoteRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length > 0)
            {
                int id = 0;
                if (int.TryParse(e.Args[0], out id))
                {
                    try
                    {
                        this._database.ExecuteNonQuery("DELETE FROM [quotes] WHERE [id] = " + id);
                        bot.SendReply(e, "Quote deleted.");
                        return;
                    }
                    catch
                    {
                        bot.SendReply(e, "Invalid quote ID.");
                        return;
                    }
                }
                else
                {
                    bot.SendReply(e, "Correct Usage: quote remove [id]");
                    return;
                }
            }
            else
            {
                bot.SendReply(e, "Correct Usage: quote remove [id]");
                return;
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "quote":
                    return "Displays a random quote." +
                        "Usage: /tell " + bot.Character + " quote";
                case "quote add":
                    return "Allows you to add a quote to the repository." +
                        "Usage: /tell " + bot.Character + " quote add [quote]";
                case "quote remove":
                    return "Allows you to remove a particular quote given the id.\n" +
                        "Usage: /tell " + bot.Character + " quote remove [id]";
            }
            return null;
        }
    }
}
