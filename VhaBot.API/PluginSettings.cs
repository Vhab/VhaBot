using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class PluginSettings
    {
        /// <summary>
        /// The full name of the plugin
        /// </summary>
        public string Name;
        /// <summary>
        /// The internal name of the plugin.
        /// This name will be used by the core and other plugins to "talk" to it
        /// </summary>
        public string InternalName;
        /// <summary>
        /// The creator of the plugin
        /// </summary>
        public string Author;
        /// <summary>
        /// The current version of the plugin
        /// </summary>
        public int Version;
        /// <summary>
        /// A small description explaining what the plugin does
        /// </summary>
        public string Description;
        /// <summary>
        /// This will define if PluginBase.OnCommand() will be run on a seperate thread.
        /// Set this to true if your command may take more then 1 second to complete
        /// </summary>
        public bool Threaded = true;
        /// <summary>
        /// Commands that will trigger PluginBase.OnCommand()
        /// Don't add commands here directly, use RegisterCommand()
        /// </summary>
        public Dictionary<string, CommandRights> Commands = new Dictionary<string, CommandRights>();
        /// <summary>
        /// Defines in which state the plugin will start when it's detected for the first time.
        /// </summary>
        public PluginState DefaultState = PluginState.Disabled;
        /// <summary>
        /// A list of plugins that are required to be loaded for this plugin.
        /// Only use the internal name of the plugins
        /// </summary>
        public List<string> Dependencies = new List<string>();
        /// <summary>
        /// A list of plugins that depend on this plugin.
        /// You don't need to add anything here, it will be done automatically
        /// </summary>
        public string[] Dependees = new string[0];

        /// <summary>
        /// For registering commands that will trigger your plugin.
        /// (Only use in your plugin constructor!)
        /// </summary>
        /// <param name="command">The command to respond to</param>
        /// <param name="rights">The default userlevel required to access this command</param>
        public void RegisterCommand(string command, UserLevel rights) { this.RegisterCommand(command, rights, rights, rights); }
        [Obsolete("This function is now deprecated. Please use RegisterCommand(string command, UserLevel privateMessage, UserLevel privateChannel, UserLevel organization)")]
        public void RegisterCommand(string command, CommandRights rights)
        {
            if (command == null || command == string.Empty)
            {
                return;
            }
            command = command.ToLower();
            if (!this.Commands.ContainsKey(command))
            {
                CommandRights realRights = new CommandRights();
                realRights.PrivateMessage = rights.PrivateMessage;
                realRights.PrivateChannel = rights.PrivateChannel;
                realRights.Organization = rights.Organization;
                this.Commands.Add(command, realRights);
            }
        }
        /// <summary>
        /// For registering commands that will trigger your plugin.
        /// (Only use in your plugin constructor!)
        /// </summary>
        /// <param name="command">The command to respond to</param>
        /// <param name="privateMessage">The default userlevel required to access this command from a private message</param>
        /// <param name="privateChannel">The default userlevel required to access this command from the private channel</param>
        /// <param name="organization">The default userlevel required to access this command from the organization channel</param>
        public void RegisterCommand(string command, UserLevel privateMessage, UserLevel privateChannel, UserLevel organization)
        {
            if (command == null || command == string.Empty)
                return;
            
            command = command.ToLower();
            if (!this.Commands.ContainsKey(command))
            {
                CommandRights rights = new CommandRights();
                rights.PrivateMessage = privateMessage;
                rights.PrivateChannel = privateChannel;
                rights.Organization = organization;
                this.Commands.Add(command, rights);
            }
        }

        /// <summary>
        /// Registers an alternative way to access a certain command
        /// </summary>
        /// <param name="alias">The alias to respond to</param>
        /// <param name="command">The real command to trigger</param>
        public void RegisterCommandAlias(string alias, string command)
        {
            if (alias == null || alias == string.Empty)
            {
                return;
            }
            if (command == null || command == string.Empty)
            {
                return;
            }
            alias = alias.ToLower();
            command = command.ToLower();
            if (this.Commands.ContainsKey(alias) || !this.Commands.ContainsKey(command))
            {
                return;
            }
            CommandRights rights = new CommandRights();
            rights.IsAlias = true;
            rights.MainCommand = command;
            this.Commands.Add(alias, rights);
        }
    }
}
