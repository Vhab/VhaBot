using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Threading;
using Mono.Data.SqliteClient;

namespace VhaBot
{
    public class Config
    {
        private static string _configPath;
        public static string ConfigPath { get { return _configPath; } }

        public enum ConfigState { Connected, Disconnected, Error }
        private enum ConfigType { String, Int }

        private SqliteConnection _connection;
        private string _configFile;
        private ConfigState _state;

        public string ConfigFile { get { return this._configFile; } }
        public IDbConnection Connection
        {
            get
            {
                int i = 0;
                while (true)
                {
                    i++;
                    if (i == 30)
                        return null;
                    lock (this._connection)
                    {
                        if (this._connection.State == ConnectionState.Open)
                        {
                            try
                            {
                                return (IDbConnection)this._connection;
                            }
                            catch { }
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
        }
        public ConfigState State
        {
            get
            {
                return this._state;
            }
        }

        public Config(string configFile) { this.Constructor(null, configFile); }
        public Config(string section, string configFile) { this.Constructor(section, configFile); }
        private void Constructor(string section, string configFile)
        {
            this._state = ConfigState.Disconnected;
            this._connection = new SqliteConnection();
            if (configFile.Length >= 4)
            {
                if (configFile.Substring(configFile.Length - 4) != ".db3")
                {
                    configFile += ".db3";
                }
            }
            else
            {
                configFile += ".db3";
            }
            if (section != null && section != string.Empty)
            {
                section = ConfigPath + Path.DirectorySeparatorChar + section;
                if (!Directory.Exists(section))
                    Directory.CreateDirectory(section);
                configFile = section + Path.DirectorySeparatorChar + configFile;
            }
            else
                configFile = ConfigPath + Path.DirectorySeparatorChar + configFile;
            this._configFile = configFile;

            if (File.Exists(this._configFile) == false)
            {
                BotShell.Output("[Configuration] No " + this._configFile + " Detected, Creating it...", true);
            }
            if (!this.Open())
            {
                BotShell.Output("[Error] Unable to load configuration file: " + this.ConfigFile);
                this._state = ConfigState.Error;
                return;
            }
            BotShell.Output("[Configuration] Loaded " + this._configFile);
        }

        private bool Open() {
            try
            {
                this._connection.ConnectionString = "URI=file:" + this._configFile +",version=3";
                this._connection.Open();
                this._state = ConfigState.Connected;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            try
            {
                this._connection.Close();
                this._connection.Dispose();
            }
            catch { }
        }

        public int ExecuteNonQuery(string query)
        {
            try
            {
                lock (this.Connection)
                {
                    using (IDbCommand Command = this.Connection.CreateCommand())
                    {
                        Command.CommandText = query;
                        return Command.ExecuteNonQuery();
                    }
                }
            }
            catch { }
            return -1;
        }

        public static string EscapeString(string value)
        {
            value = value.Replace(@"'", @"''");
            return value;
        }

        public static void SetConfigPath(string path)
        {
            _configPath = path;
        }
    }
}
