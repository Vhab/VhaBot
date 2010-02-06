using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace VhaBot.ShellModules
{
    public class Commands
    {
        private BotShell Parent;
        private Config Config;
        private Dictionary<string, CommandRights> CommandsList;

        public Commands(BotShell parent)
        {
            this.Parent = parent;
            this.Config = new Config(this.Parent.ToString(), "rights");
            this.CommandsList = new Dictionary<string, CommandRights>();
            this.Config.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CORE_Rights (InternalName VARCHAR(255), Command VARCHAR(255), GC_Right VARCHAR(255), PG_Right VARCHAR(255), TELL_Right VARCHAR(255))");
        }

        public bool Exists(string command)
        {
            if (command == null || command == string.Empty)
                return false;

            command = command.ToLower();
            lock (this.CommandsList)
                return this.CommandsList.ContainsKey(command);
        }

        public bool Register(string plugin, Command command)
        {
            if (this.Exists(command.CommandName))
                return false;
            if (plugin == null || plugin == string.Empty)
                return false;
            if (command.IsAlias)
                return false;

            CommandRights tmp = new CommandRights();
            tmp.Plugin = plugin.ToLower();
            tmp.Organization = command.Organization;
            tmp.PrivateChannel = command.PrivateChannel;
            tmp.PrivateMessage = command.PrivateMessage;
            tmp.Help = command.Help;

            lock (this.CommandsList)
                this.CommandsList.Add(command.CommandName, tmp);
            return true;
        }

        public bool RegisterAlias(string alias, string command)
        {
            if (this.Exists(alias) || !this.Exists(command))
                return false;
            if (alias == null || alias == string.Empty)
                return false;

            alias = alias.ToLower();
            command = command.ToLower();
            
            lock (this.CommandsList)
                if (this.CommandsList[command].IsAlias)
                    return false;

            CommandRights rights = new CommandRights();
            rights.IsAlias = true;
            rights.MainCommand = command;
            rights.Plugin = string.Empty;
            lock (this.CommandsList)
                this.CommandsList.Add(alias, rights);
            return true;
        }

        public void Unregister(string command)
        {
            if (!this.Exists(command))
                return;
            command = command.ToLower();
            lock (this.CommandsList)
                this.CommandsList.Remove(command);
        }

        public void UnregisterAll(string plugin)
        {
            if (plugin == null || plugin == string.Empty)
                return;
            List<string> remove = new List<string>();
            plugin = plugin.ToLower();
            lock (this.CommandsList)
            {
                foreach (KeyValuePair<string, CommandRights> kvp in this.CommandsList)
                    if (kvp.Value.Plugin == plugin)
                        remove.Add(kvp.Key);
                foreach (KeyValuePair<string, CommandRights> kvp in this.CommandsList)
                    if (kvp.Value.IsAlias && remove.Contains(kvp.Value.MainCommand) && !remove.Contains(kvp.Key))
                        remove.Add(kvp.Key);
                foreach (string command in remove)
                    this.CommandsList.Remove(command);
            }
        }

        public string GetMainCommand(string command)
        {
            if (!this.Exists(command))
                return command;
            command = command.ToLower();
            lock (this.CommandsList)
                if (this.CommandsList[command].IsAlias)
                    return this.CommandsList[command].MainCommand;
                else
                    return command;
        }

        public string GetInternalName(string command)
        {
            command = this.GetMainCommand(command).ToLower();
            lock (this.CommandsList)
                if (this.CommandsList.ContainsKey(command))
                    return this.CommandsList[command].Plugin;
                else
                    return null;
        }

        public string[] GetCommands(string plugin)
        {
            plugin = plugin.ToLower();
            List<string> list = new List<string>();
            lock (this.CommandsList)
                foreach (KeyValuePair<string, CommandRights> kvp in this.CommandsList)
                    if (kvp.Value.Plugin.ToLower() == plugin && !kvp.Value.IsAlias)
                        list.Add(kvp.Key.ToLower());
            return list.ToArray();
        }

        public int GetCommandsCount()
        {
            lock (this.CommandsList)
                return this.CommandsList.Count;
        }

        public CommandRights GetDefaultRights(string command)
        {
            if (!this.Exists(command))
                return null;
            command = this.GetMainCommand(command);
            lock (this.CommandsList)
            {
                CommandRights rights = new CommandRights();
                rights.Plugin = this.CommandsList[command].Plugin;
                rights.Organization = this.CommandsList[command].Organization;
                rights.PrivateChannel = this.CommandsList[command].PrivateChannel;
                rights.PrivateMessage = this.CommandsList[command].PrivateMessage;
                rights.Help = this.CommandsList[command].Help;
                return rights;
            }
        }

        public UserLevel GetDefaultRight(string command, CommandType type)
        {
            CommandRights rights = this.GetDefaultRights(command);
            if (rights == null)
                return UserLevel.Disabled;
            switch (type)
            {
                case CommandType.Organization:
                    return rights.Organization;
                case CommandType.PrivateChannel:
                    return rights.PrivateChannel;
                case CommandType.Tell:
                    return rights.PrivateMessage;
                default:
                    return UserLevel.Disabled;
            }
        }

        public CommandRights GetRights(string command)
        {
            if (!this.Exists(command))
                return null;

            CommandRights rights = this.GetDefaultRights(command);
            try
            {
                lock (this.CommandsList)
                {
                    command = this.GetMainCommand(command);
                    string internalName = this.CommandsList[command].Plugin.ToLower();
                    using (IDbCommand cmd = this.Config.Connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT GC_Right, PG_Right, TELL_Right FROM CORE_Rights WHERE InternalName = '" + internalName + "' AND Command = '"+command+"'";
                        IDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            rights.Organization = (UserLevel)Enum.Parse(typeof(UserLevel), reader.GetString(0));
                            rights.PrivateChannel = (UserLevel)Enum.Parse(typeof(UserLevel), reader.GetString(1));
                            rights.PrivateMessage = (UserLevel)Enum.Parse(typeof(UserLevel), reader.GetString(2));
                        }
                        reader.Close();
                    }
                }
            }
            catch { }
            return rights;
        }

        public UserLevel GetRight(string command, CommandType type)
        {
            CommandRights rights = this.GetRights(command);
            if (rights == null)
                return UserLevel.Disabled;
            switch (type)
            {
                case CommandType.Organization:
                    return rights.Organization;
                case CommandType.PrivateChannel:
                    return rights.PrivateChannel;
                case CommandType.Tell:
                    return rights.PrivateMessage;
                default:
                    return UserLevel.Disabled;
            }
        }

        public string GetHelp(string command)
        {
            if (!this.HasHelp(command))
                return null;
            string internalName = this.GetInternalName(command);
            PluginBase plugin = this.Parent.Plugins.GetPlugin(internalName);
            if (plugin == null)
                return null;
            string result = null;
            try
            {
                result = plugin.OnHelp(this.Parent, command.ToLower());
            }
            catch { }
            if (result == null)
                return string.Empty;
            return result;
        }

        public bool HasHelp(string command)
        {
            if (!this.Exists(command))
                return false;
            lock (this.CommandsList)
                return this.CommandsList[command.ToLower()].Help;
        }

        public void ResetRights(string command)
        {
            if (!this.Exists(command))
                return;
            command = this.GetMainCommand(command);
            string internalName;
            lock (this.CommandsList)
                internalName = this.CommandsList[command].Plugin.ToLower();
            this.Config.ExecuteNonQuery("DELETE FROM CORE_Rights WHERE InternalName = '" + internalName + "' AND Command = '" + command + "'");
        }

        public void ResetAllRights(string plugin)
        {
            plugin = plugin.ToLower();
            this.Config.ExecuteNonQuery("DELETE FROM CORE_Rights WHERE InternalName = '" + plugin + "'");
        }

        public void ResetRight(string command, CommandType type)
        {
            if (!this.Exists(command))
                return;
            UserLevel right = this.GetRight(command, type);
            this.SetRight(command, type, right);
        }

        public bool SetRights(string command, CommandRights rights)
        {
            if (!this.Exists(command))
                return false;
            if (rights == null)
                return false;
            command = this.GetMainCommand(command);
            string plugin;
            lock (this.CommandsList)
                plugin = this.CommandsList[command].Plugin.ToLower();
            CommandRights defaultRights = this.GetRights(command);
            if (defaultRights == null)
                return false;
            this.ResetRights(command);
            if (defaultRights != rights)
                this.Config.ExecuteNonQuery("INSERT INTO CORE_Rights VALUES('" + plugin + "', '" + command + "', '" + rights.Organization.ToString() + "', '" + rights.PrivateChannel.ToString() + "', '" + rights.PrivateMessage.ToString() + "')");
            return true;
        }

        public bool SetRight(string command, CommandType type, UserLevel right)
        {
            if (!this.Exists(command))
                return false;
            CommandRights rights = this.GetRights(command);
            if (rights == null)
                return false;
            switch (type)
            {
                case CommandType.Organization:
                    rights.Organization = right;
                    break;
                case CommandType.PrivateChannel:
                    rights.PrivateChannel = right;
                    break;
                case CommandType.Tell:
                    rights.PrivateMessage = right;
                    break;
            }
            return this.SetRights(command, rights);
        }
    }
}
