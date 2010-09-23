using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class RaidDatabase : PluginBase
    {
        private string _connectString = string.Empty;
        private MySqlConnection _connection;
        private BotShell _bot;
        private DateTime _lastActivity;
        public IDbConnection Connection
        {
            get
            {
                TimeSpan idle = DateTime.Now - this._lastActivity;
                if (idle.TotalMinutes > 5)
                    this.RebuildConnection(false);
                if (!this.Connected)
                    return null;
                this._lastActivity = DateTime.Now;
                return (IDbConnection)this._connection;
            }
        }
        public bool Connected
        {
            get
            {
                if (this._connection == null)
                    this.RebuildConnection(false);
                else
                    lock (this._connection)
                        if (this._connection.Ping() == false || this._connection.State != ConnectionState.Open)
                            this.RebuildConnection(false);
                if (this._connection == null)
                    return false;
                lock (this._connection)
                    return (this._connection.State == ConnectionState.Open);
            }
        }

        public RaidDatabase()
        {
            this.Name = "Raid :: Database";
            this.InternalName = "RaidDatabase";
            this.Author = "Vhab / Iriche";
            this.Description = "Handles the database connection to the database";
            this.DefaultState = PluginState.Disabled;
            this.Commands = new Command[] {
                new Command("raid reconnect", false, UserLevel.Leader)
            };
            this.Version = 100;
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
            bot.Configuration.Register(ConfigType.String, this.InternalName, "user", "MySql User", "root");
            bot.Configuration.Register(ConfigType.Password, this.InternalName, "password", "MySql Password", "");
            bot.Configuration.Register(ConfigType.String, this.InternalName, "host", "MySql Host", "localhost");
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "port", "MySql Port", 3306);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "database", "MySql Database", bot.Character.ToLower());
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            this.RebuildConnection(true);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }

        private void RebuildConnection(bool readConfig)
        {
            if (readConfig)
            {
                string user = this._bot.Configuration.GetString(this.InternalName, "user", "root");
                string password = this._bot.Configuration.GetPassword(this.InternalName, "password", "");
                string host = this._bot.Configuration.GetString(this.InternalName, "host", "127.0.0.1");
                int port = this._bot.Configuration.GetInteger(this.InternalName, "port", 3306);
                string database = this._bot.Configuration.GetString(this.InternalName, "database", "raid");
                string connectA = "server=" + host + ";port=" + port + ";connect timeout=20;pooling=false;protocol=socket";
                this._connectString = connectA + ";uid=" + user + ";pwd=" + password + ";database=" + database + ";";
            }
            if (this._connection != null)
                lock (this._connection)
                    try { this._connection.Close(); }
                    catch { }
            try
            {
                this._connection = new MySqlConnection(this._connectString);
                lock (this._connection)
                {
                    this._connection.Open();
                }
            }
            catch { this._connection = null; }
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
            {
                switch (e.Key)
                {
                    case "user":
                    case "password":
                    case "host":
                    case "port":
                    case "database":
                        this.RebuildConnection(true);
                        break;
                }
            }
        }

        public bool CheckDatabase(BotShell bot, CommandArgs e)
        {
            if (this.Connected)
                return true;
            bot.SendReply(e, "Unable to connect to the database. Please try again later");
            return false;
        }

        public int ExecuteNonQuery(string query) {
            if (!this.Connected)
                return -1;

            try
            {
                lock (this._connection)
                {
                    using (IDbCommand command = this.Connection.CreateCommand())
                    {
                        command.CommandText = query;
                        return command.ExecuteNonQuery();
                    }
                }
            }
            catch { }
            return -1;
        }

        private void OnRaidReconnectCommand(BotShell bot, CommandArgs e)
        {
            this.RebuildConnection(false);
            if (this._connection != null)
                bot.SendReply(e, "Restored the connection to the Raid Database");
            else
                bot.SendReply(e, "Could not restore the connection to the Raid Database");
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "raid reconnect":
                    this.OnRaidReconnectCommand(bot, e);
                    break;
            }
        }

    }
}
