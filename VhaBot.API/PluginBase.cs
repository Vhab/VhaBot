using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using VhaBot.Communication;
using AoLib.Net;

namespace VhaBot
{
    #region old crap
    /*public abstract class PluginBase : IDisposable
    {
        private BotShell _bot;
        private PluginSettings _settings = new PluginSettings();

        /// <summary>
        /// A reference to the main bot
        /// </summary>
        public BotShell Bot { get { return this._bot; } }
        /// <summary>
        /// The settings of the plugin.
        /// Set all these values in the constructor and never touch them again
        /// </summary>
        public PluginSettings Settings { get { return this._settings; } }

        /// <summary>
        /// Used by the Core to set a reference to the parent
        /// </summary>
        /// <param name="bot">Reference to the parent</param>
        public void SetParent(BotShell bot)
        {
            if (this._bot == null) { this._bot = bot; }
            else { throw new Exception("Parent can only be set once!"); }
        }

        public abstract void OnLoad();
        public abstract void OnUnload();

        public virtual void OnInstall() { }
        public virtual void OnUninstall() { }

        public virtual void OnUpgrade(Int32 oldVersion, Int32 newVersion) { }

        public virtual void OnCommand(BotShell sender, CommandArgs e) { }
        public virtual void OnUnauthorizedCommand(BotShell sender, CommandArgs e) { }

        public virtual string OnHelp(BotShell sender, string command) { return null; }

        public void FireOnCommand(object args)
        {
            try
            {
                CommandArgs a = (CommandArgs)args;
                if (a.Authorized)
                    this.OnCommand(this.Bot, a);
                else
                    this.OnUnauthorizedCommand(this.Bot, a);
            }
            catch (Exception ex)
            {
                CommandArgs e = (CommandArgs)args;
                RichTextWindow window = new RichTextWindow(this.Bot);
                window.AppendTitle("Error Report");

                window.AppendHighlight("Error: ");
                window.AppendNormal(ex.Message);
                window.AppendLinkEnd();
                window.AppendLineBreak();

                window.AppendHighlight("Source: ");
                window.AppendNormal(ex.Source);
                window.AppendLinkEnd();
                window.AppendLineBreak();

                window.AppendHighlight("Target Site: ");
                window.AppendNormal(ex.TargetSite.ToString());
                window.AppendLinkEnd();
                window.AppendLineBreak();

                window.AppendHighlight("Stack Trace:");
                window.AppendLineBreak();
                window.AppendNormal(ex.StackTrace);
                window.AppendLinkEnd();
                window.AppendLineBreak();

                this.Bot.SendReply(e, "There has been an error while executing this command »» " + window.ToString("More Information"));
                Log.WriteLine("[DEBUG] [Plugin Execution Error] " + ex.ToString(), true);
            }
        }

        public void Dispose()
        {
            this._bot = null;
        }

        public override string ToString()
        {
            return this.Settings.Name + " v" + this.Settings.Version;
        }
        /*
        /// <summary>
        /// Allows plugins to register settings which later can be changed through the configuration panel
        /// </summary>
        /// <param name="type">Setting Type</param>
        /// <param name="key">The Internal Key of the Setting</param>
        /// <param name="name">The Name of the Setting (Will be displayed in the configuration panel)</param>
        /// <param name="defaultValue">The Default Value</param>
        /// <param name="values">Selection of values for creating an enum setting (ONLY AVAILABLE FOR STRING AND INTEGER SETTING TYPES!)</param>
        /// <returns></returns>
        protected bool ConfigRegister(ConfigType type, string key, string name, object defaultValue, params object[] values)
        {
            if (this.Bot == null)
                throw new Exception("You can't register configuration entries in the constructor!");
            return this.Bot.Configuration.Register(type, this.Settings.InternalName, key, name, defaultValue, values);
        }

        protected void ConfigUnregister(string key)
        {
            if (this.Bot == null)
                throw new Exception("You can't unregister configuration entries in the constructor!");
            this.Bot.Configuration.Unregister(this.Settings.InternalName, key);
        }

        protected void ConfigDelete(string key)
        {
            this.Bot.Configuration.Delete(this.Settings.InternalName, key);
        }

        #region ConfigSet*
        protected bool ConfigSet(ConfigType type, string key, object value)
        {
            return this.Bot.Configuration.Set(type, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetString(string key, string value)
        {
            return this.Bot.Configuration.Set(ConfigType.String, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetPassword(string key, string value)
        {
            return this.Bot.Configuration.Set(ConfigType.Password, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetInteger(string key, int value)
        {
            return this.Bot.Configuration.Set(ConfigType.Integer, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetBoolean(string key, bool value)
        {
            return this.Bot.Configuration.Set(ConfigType.Boolean, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetDate(string key, DateTime value)
        {
            return this.Bot.Configuration.Set(ConfigType.Date, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetTime(string key, TimeSpan value)
        {
            return this.Bot.Configuration.Set(ConfigType.Time, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetUsername(string key, string value)
        {
            return this.Bot.Configuration.Set(ConfigType.Username, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetDimension(string key, Server value)
        {
            return this.Bot.Configuration.Set(ConfigType.Dimension, this.Settings.InternalName, key, value);
        }

        protected bool ConfigSetColor(string key, string value)
        {
            return this.Bot.Configuration.Set(ConfigType.Color, this.Settings.InternalName, key, value);
        }
        #endregion

        #region ConfigGet*
        protected object ConfigGet(ConfigType type, string key, object defaultValue)
        {
            return this.Bot.Configuration.Get(type, this.Settings.InternalName, key, defaultValue);
        }

        protected string ConfigGetString(string key, string defaultValue)
        {
            return (string)this.Bot.Configuration.Get(ConfigType.String, this.Settings.InternalName, key, defaultValue);
        }

        protected string ConfigGetPassword(string key, string defaultValue)
        {
            return (string)this.Bot.Configuration.Get(ConfigType.Password, this.Settings.InternalName, key, defaultValue);
        }

        protected int ConfigGetInteger(string key, int defaultValue)
        {
            return (int)this.Bot.Configuration.Get(ConfigType.Integer, this.Settings.InternalName, key, defaultValue);
        }

        protected bool ConfigGetBoolean(string key, bool defaultValue)
        {
            return (bool)this.Bot.Configuration.Get(ConfigType.Boolean, this.Settings.InternalName, key, defaultValue);
        }

        protected DateTime ConfigGetDate(string key, DateTime defaultValue)
        {
            return (DateTime)this.Bot.Configuration.Get(ConfigType.Date, this.Settings.InternalName, key, defaultValue);
        }

        protected TimeSpan ConfigGetTime(string key, TimeSpan defaultValue)
        {
            return (TimeSpan)this.Bot.Configuration.Get(ConfigType.Time, this.Settings.InternalName, key, defaultValue);
        }

        protected string ConfigGetUsername(string key, string defaultValue)
        {
            return (string)this.Bot.Configuration.Get(ConfigType.Username, this.Settings.InternalName, key, defaultValue);
        }

        protected Server ConfigGetDimension(string key, Server defaultValue)
        {
            return (Server)this.Bot.Configuration.Get(ConfigType.Boolean, this.Settings.InternalName, key, defaultValue);
        }

        protected string ConfigGetColor(string key, string defaultValue)
        {
            return (string)this.Bot.Configuration.Get(ConfigType.Color, this.Settings.InternalName, key, defaultValue);
        }
        #endregion
        
    }*/
    #endregion

    public abstract class PluginBase : MarshalByRefObject
    {
        private bool _locked;
        private string _name;
        private string _internalName;
        private int _version;
        private string _author;
        private string _description;
        private PluginState _defaultState;
        private string[] _dependencies;
        private Command[] _commands;

        public string Name {
            set { if (this._locked) { throw new Exception(); } this._name = value; }
            get { return this._name; }
        }
        public string InternalName {
            set { if (this._locked) { throw new Exception(); } this._internalName = value.ToLower(); }
            get { return this._internalName; }
        }
        public int Version {
            set { if (this._locked) { throw new Exception(); } this._version = value; }
            get { return this._version; }
        }
        public string Author {
            set { if (this._locked) { throw new Exception(); } this._author = value; }
            get { return this._author; }
        }
        public string Description {
            set { if (this._locked) { throw new Exception(); } this._description = value; }
            get { return this._description; }
        }
        public PluginState DefaultState {
            set { if (this._locked) { throw new Exception(); } this._defaultState = value; }
            get { return this._defaultState; }
        }
        public string[] Dependencies
        {
            set { if (this._locked) { throw new Exception(); } this._dependencies = value; }
            get { if (this._dependencies != null) { return this._dependencies; } else { return new string[0]; } }
        }
        public Command[] Commands
        {
            set { if (this._locked) { throw new Exception(); } this._commands = value; }
            get { if (this._commands != null) { return this._commands; } else { return new Command[0]; } }
        }

        internal void Init() { this._locked = true; }

        public virtual void OnLoad(BotShell bot) { }
        public virtual void OnUnload(BotShell bot) { }
        public virtual void OnInstall(BotShell bot) { }
        public virtual void OnUninstall(BotShell bot) { }
        public virtual void OnUpgrade(BotShell bot, Int32 version) { }

        public virtual void OnCommand(BotShell bot, CommandArgs e) { }
        public virtual void OnUnauthorizedCommand(BotShell bot, CommandArgs e) { }

        public virtual string OnHelp(BotShell bot, string command) { return null; }
        public virtual string OnCustomConfiguration(BotShell bot, string key) { return null; }

        public virtual void OnPluginMessage(BotShell bot, PluginMessage message) { }
        public virtual void OnBotMessage(BotShell bot, BotMessage message) { }

        public override string ToString() { return this.Name + " v" + this.Version; }

        public void FireOnCommand(BotShell bot, CommandArgs args)
        {
            try
            {
                if (args.Authorized)
                    this.OnCommand(bot, args);
                else
                    this.OnUnauthorizedCommand(bot, args);
            }
            catch (Exception ex)
            {
                CommandArgs e = (CommandArgs)args;
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Error Report");

                window.AppendHighlight("Error: ");
                window.AppendNormal(ex.Message);
                window.AppendLinkEnd();
                window.AppendLineBreak();

                window.AppendHighlight("Source: ");
                window.AppendNormal(ex.Source);
                window.AppendLinkEnd();
                window.AppendLineBreak();

                window.AppendHighlight("Target Site: ");
                window.AppendNormal(ex.TargetSite.ToString());
                window.AppendLinkEnd();
                window.AppendLineBreak();

                window.AppendHighlight("Stack Trace:");
                window.AppendLineBreak();
                window.AppendNormal(ex.StackTrace);
                window.AppendLinkEnd();
                window.AppendLineBreak();

                bot.SendReply(e, "There has been an error while executing this command »» " + window.ToString("More Information"));
                BotShell.Output("[Plugin Execution Error] " + ex.ToString());
            }
        }
    }
}