using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.ShellModules
{
    public class Configuration
    {
        private BotShell Parent;
        private Config Config;
        private Dictionary<string, Dictionary<string, ConfigurationEntry>> ConfigurationEntries;

        public Configuration(BotShell parent)
        {
            this.Parent = parent;
            this.ConfigurationEntries = new Dictionary<string, Dictionary<string, ConfigurationEntry>>();
            this.Config = new Config(this.Parent.ToString(), "configuration");
            this.Config.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CORE_Settings (Section VARCHAR(255), Key VARCHAR(255), Type VARCHAR(255), Value VARCHAR(255))");
        }

        public bool Register(ConfigType type, string plugin, string key, string name, object defaultValue, params object[] values)
        {
            plugin = plugin.ToLower();
            if (!this.Parent.Plugins.Exists(plugin))
                return false;

            key = key.ToLower();
            lock (this.ConfigurationEntries)
            {
                if (!this.ConfigurationEntries.ContainsKey(plugin))
                    this.ConfigurationEntries.Add(plugin, new Dictionary<string, ConfigurationEntry>());
                if (this.ConfigurationEntries[plugin].ContainsKey(key))
                    return false;
                ConfigurationEntry entry = new ConfigurationEntry(type, plugin, key, name, defaultValue, values);
                if (entry.Values != null && entry.Values.Length > 0)
                {
                    List<object> valuesList = new List<object>(entry.Values);
                    if (!valuesList.Contains(entry.DefaultValue))
                        return false;
                }
                this.ConfigurationEntries[plugin].Add(key, entry);
                return true;
            }
        }

        public void Unregister(string plugin, string key)
        {
            plugin = plugin.ToLower();
            key = key.ToLower();
            lock (this.ConfigurationEntries)
            {
                if (!this.ConfigurationEntries.ContainsKey(plugin))
                    return;
                if (!this.ConfigurationEntries[plugin].ContainsKey(key))
                    return;
                this.ConfigurationEntries[plugin].Remove(key);
                if (this.ConfigurationEntries[plugin].Count == 0)
                    this.ConfigurationEntries.Remove(plugin);
            }
        }

        public void UnregisterAll(string plugin)
        {
            plugin = plugin.ToLower();
            lock (this.ConfigurationEntries)
            {
                if (!this.ConfigurationEntries.ContainsKey(plugin))
                    return;
                this.ConfigurationEntries.Remove(plugin);
            }
        }

        public bool IsRegistered(string plugin, string key)
        {
            plugin = plugin.ToLower();
            key = key.ToLower();
            lock (this.ConfigurationEntries)
            {
                if (!this.ConfigurationEntries.ContainsKey(plugin))
                    return false;
                if (!this.ConfigurationEntries[plugin].ContainsKey(key))
                    return false;
                return true;
            }
        }

        public ConfigurationEntry GetRegistered(string plugin, string key)
        {
            if (!this.IsRegistered(plugin, key))
                return null;
            lock (this.ConfigurationEntries)
                return this.ConfigurationEntries[plugin.ToLower()][key.ToLower()];
        }

        public ConfigType GetType(string plugin, string key)
        {
            plugin = plugin.ToLower();
            key = key.ToLower();
            if (this.IsRegistered(plugin, key))
                lock (this.ConfigurationEntries)
                    return this.ConfigurationEntries[plugin][key].Type;
            else
                throw new Exception("no such configuration entry!");
        }

        public string GetCustom(string plugin, string key)
        {
            if (!this.IsRegistered(plugin, key))
                return null;
            if (this.GetRegistered(plugin, key).Type != ConfigType.Custom)
                return null;
            PluginBase plug = this.Parent.Plugins.GetPlugin(plugin);
            if (plug == null)
                return null;
            try { return plug.OnCustomConfiguration(this.Parent, key.ToLower()); }
            catch { }
            return null;
        }

        public bool Exists(string plugin, string key)
        {
            plugin = Config.EscapeString(plugin.ToLower());
            key = Config.EscapeString(key.ToLower());
            bool result = false;
            try
            {
                using (IDbCommand command = this.Config.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT Type FROM CORE_Settings WHERE Section = '" + plugin + "' AND Key = '" + key + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                        result = true;
                    reader.Close();
                }
            }
            catch { }
            return result;
        }

        public bool Set(ConfigType type, string plugin, string key, object value)
        {
            if (!this.IsRegistered(plugin, key))
                return false;

            ConfigType dbType = this.GetType(plugin, key);
            if (dbType != type)
                throw new InvalidCastException("Can't replace type " + dbType.ToString() + " with type " + type.ToString() + " at " + key + "@" + plugin);
            if (type == ConfigType.Custom)
                throw new ArgumentException("Can not set type Custom!");

            plugin = plugin.ToLower();
            key = key.ToLower();
            string format;
            if (!this.Exists(plugin, key))
                format = "INSERT INTO CORE_Settings VALUES ('{0}', '{1}', '{2}', '{3}')";
            else
                format = "UPDATE CORE_Settings SET Value = '{3}', Type = '{2}' WHERE Section = '{0}' AND Key = '{1}'";
            
            try
            {
                string valueString;
                switch (type)
                {
                    case ConfigType.Color:
                        if (!Regex.Match((string)value, "^[#]?[0-9a-f]{6}$", RegexOptions.IgnoreCase).Success)
                            return false;
                        valueString = (string)value;
                        if (valueString.Length == 7)
                            valueString = valueString.Substring(1);
                        break;
                    case ConfigType.String:
                        valueString = (string)value;
                        ConfigurationEntry stringEntry = this.GetEntry(plugin, key);
                        if (stringEntry.Values != null && stringEntry.Values.Length > 0)
                        {
                            List<object> values = new List<object>(stringEntry.Values);
                            if (!values.Contains(value))
                                return false;
                        }
                        break;
                    case ConfigType.Password:
                        valueString = (string)value;
                        break;
                    case ConfigType.Integer:
                        valueString = ((Int32)value).ToString();
                        ConfigurationEntry intEntry = this.GetEntry(plugin, key);
                        bool result = (intEntry.Values.Length < 1);
                        foreach (object intEntryValue in intEntry.Values)
                        {
                            try
                            {
                                if ((Int32)intEntryValue == (Int32)value)
                                    result = true;
                            }
                            catch { }
                        }
                        if (!result)
                            return false;
                        break;
                    case ConfigType.Boolean:
                        valueString = "false";
                        if ((Boolean)value)
                            valueString = "true";
                        break;
                    case ConfigType.Date:
                        valueString = TimeStamp.FromDateTime((DateTime)value).ToString();
                        break;
                    case ConfigType.Time:
                        valueString = ((TimeSpan)value).Ticks.ToString();
                        break;
                    case ConfigType.Dimension:
                        valueString = ((Server)value).ToString();
                        break;
                    case ConfigType.Username:
                        if (this.Parent.GetUserID((string)value) < 1)
                            return false;
                        valueString = (string)value;
                        break;
                    default:
                        return false;
                }
                string query = string.Format(format, Config.EscapeString(plugin), Config.EscapeString(key), type.ToString(), Config.EscapeString(valueString));
                if (this.Config.ExecuteNonQuery(query) > 0)
                {
                    ConfigurationChangedArgs args = new ConfigurationChangedArgs(type, plugin, key, value);
                    this.Parent.Events.OnConfigurationChanged(this.Parent, args);
                    return true;
                }
            }
            catch { }
            return false;
        }
        #region Set*
        public bool SetString(string plugin, string key, string value)
        {
            return this.Set(ConfigType.String, plugin, key, value);
        }

        public bool SetPassword(string plugin, string key, string value)
        {
            return this.Set(ConfigType.Password, plugin, key, value);
        }

        public bool SetInteger(string plugin, string key, int value)
        {
            return this.Set(ConfigType.Integer, plugin, key, value);
        }

        public bool SetBoolean(string plugin, string key, bool value)
        {
            return this.Set(ConfigType.Boolean, plugin, key, value);
        }

        public bool SetDate(string plugin, string key, DateTime value)
        {
            return this.Set(ConfigType.Date, plugin, key, value);
        }

        public bool SetTime(string plugin, string key, TimeSpan value)
        {
            return this.Set(ConfigType.Time, plugin, key, value);
        }

        public bool SetUsername(string plugin, string key, string value)
        {
            return this.Set(ConfigType.Username, plugin, key, value);
        }

        public bool SetDimension(string plugin, string key, Server value)
        {
            return this.Set(ConfigType.Dimension, plugin, key, value);
        }

        public bool SetColor(string plugin, string key, string value)
        {
            return this.Set(ConfigType.Color, plugin, key, value);
        }
        #endregion

        public object Get(ConfigType type, string plugin, string key, object defaultValue)
        {
            if (!this.IsRegistered(plugin, key))
                return defaultValue;

            ConfigType dbType = this.GetType(plugin, key);
            bool exists = this.Exists(plugin, key);
            if (dbType != type)
                throw new InvalidCastException("Can't lookup type " + type.ToString() + " because it's registered as " + dbType.ToString() + " at " + key + "@" + plugin);
            if (type == ConfigType.Custom)
                throw new ArgumentException("Can not get type Custom!");

            if (!exists)
                    lock (this.ConfigurationEntries)
                        if (this.ConfigurationEntries[plugin.ToLower()][key.ToLower()].Type != type)
                            throw new InvalidCastException();
                        else
                            return this.ConfigurationEntries[plugin.ToLower()][key.ToLower()].DefaultValue;

            plugin = Config.EscapeString(plugin.ToLower());
            key = Config.EscapeString(key.ToLower());
            object result = null;
            try
            {
                using (IDbCommand command = this.Config.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT Value FROM CORE_Settings WHERE Section = '" + plugin + "' AND Key = '" + key + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        switch (type)
                        {
                            case ConfigType.String:
                            case ConfigType.Username:
                            case ConfigType.Color:
                            case ConfigType.Password:
                                result = reader.GetString(0);
                                break;
                            case ConfigType.Integer:
                                result = (Int32)reader.GetInt64(0);
                                break;
                            case ConfigType.Boolean:
                                result = false;
                                if (reader.GetString(0).ToLower() == "true")
                                    result = true;
                                break;
                            case ConfigType.Date:
                                result = TimeStamp.ToDateTime(reader.GetInt64(0));
                                break;
                            case ConfigType.Time:
                                result = new TimeSpan(reader.GetInt64(0));
                                break;
                            case ConfigType.Dimension:
                                result = (Server)Enum.Parse(typeof(Server), reader.GetString(0));
                                break;
                        }
                    }
                    reader.Close();
                }
            }
            catch { return defaultValue; }
            return result;
        }
        #region Get*
        public string GetString(string plugin, string key, string defaultValue)
        {
            return (string)this.Get(ConfigType.String, plugin, key, defaultValue);
        }

        public string GetPassword(string plugin, string key, string defaultValue)
        {
            return (string)this.Get(ConfigType.Password, plugin, key, defaultValue);
        }

        public int GetInteger(string plugin, string key, int defaultValue)
        {
            return (int)this.Get(ConfigType.Integer, plugin, key, defaultValue);
        }

        public bool GetBoolean(string plugin, string key, bool defaultValue)
        {
            return (bool)this.Get(ConfigType.Boolean, plugin, key, defaultValue);
        }

        public DateTime GetDate(string plugin, string key, DateTime defaultValue)
        {
            return (DateTime)this.Get(ConfigType.Date, plugin, key, defaultValue);
        }

        public TimeSpan GetTime(string plugin, string key, TimeSpan defaultValue)
        {
            return (TimeSpan)this.Get(ConfigType.Time, plugin, key, defaultValue);
        }

        public string GetUsername(string plugin, string key, string defaultValue)
        {
            return (string)this.Get(ConfigType.Username, plugin, key, defaultValue);
        }

        public Server GetDimension(string plugin, string key, Server defaultValue)
        {
            return (Server)this.Get(ConfigType.Boolean, plugin, key, defaultValue);
        }

        public string GetColor(string plugin, string key, string defaultValue)
        {
            return (string)this.Get(ConfigType.Color, plugin, key, defaultValue);
        }
        #endregion

        public void Delete(string plugin, string key)
        {
            plugin = Config.EscapeString(plugin.ToLower());
            key = Config.EscapeString(key.ToLower());
            this.Config.ExecuteNonQuery("DELETE FROM CORE_Settings WHERE Section = '" + plugin + "' AND Key = '" + key + "'");
        }

        public string[] ListRegisteredPlugins()
        {
            List<string> results = new List<string>();
            lock (this.ConfigurationEntries)
                foreach (string result in this.ConfigurationEntries.Keys)
                    results.Add(result);
            return results.ToArray();
        }

        public ConfigurationEntry[] List(string plugin)
        {
            plugin = plugin.ToLower();
            List<ConfigurationEntry> results = new List<ConfigurationEntry>();
            lock (this.ConfigurationEntries)
            {
                if (!this.ConfigurationEntries.ContainsKey(plugin))
                    return new ConfigurationEntry[0];
                foreach (ConfigurationEntry result in this.ConfigurationEntries[plugin].Values)
                    results.Add(result);
            }
            return results.ToArray();
        }

        public ConfigurationEntry GetEntry(string plugin, string key)
        {
            plugin = plugin.ToLower();
            key = key.ToLower();
            if (this.IsRegistered(plugin, key))
                lock (this.ConfigurationEntries)
                    return this.ConfigurationEntries[plugin][key];
            return null;
        }
    }
}
