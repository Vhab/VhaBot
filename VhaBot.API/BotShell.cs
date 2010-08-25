// Define the version we're compiling
//#define ADVANCED

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;
using System.Data;
using System.Data.Common;
using System.IO;
using AoLib.Net;
using AoLib.Utils;
using VhaBot.Configuration;
using VhaBot.ShellModules;
using VhaBot.Communication;

namespace VhaBot
{
    public sealed class BotShell
    {
        public static readonly string VERSION = "0.7.8";
        public static readonly string BRANCH = "Beta";
        public static readonly int BUILD = 20100208;
#if ADVANCED
        public static readonly string EDITION = "Advanced";
        public static readonly bool Advanced = true;
#else
        public static readonly string EDITION = "Community";
        public static readonly bool Advanced = false;
#endif
        private static bool _debug;
        public static bool Debug { get { return BotShell._debug; } }
        public static void SetDebug(bool debug) { BotShell._debug = debug; }

        public static readonly string[] IgnoredMessages = new string[] {
            "I am away from my keyboard right now,",
            "{SENDER} is AFK",
            "/tell {SENDER} !help",
            "/tell {SENDER} help",
            "Unknown command input. /tell {SENDER} help"
        };

        private string _character;
        private Server _dimension = Server.Atlantean;
        private BotState _state = BotState.Disconnected;
        private string _admin;
        private string _id;
        private bool _master = false;
        private Chat _chat = null;
        private Dictionary<int, Chat> _slaves;
        private Dictionary<string, int> _slaveIDs;
        private bool _hasSlaves = false;
        private Dictionary<String, UInt32> _userIDs;
        private string _organization = null;
        private int _organizationID = 0;
        private List<ReplyMessage> _replies = new List<ReplyMessage>();

        public string Admin { get { return Format.UppercaseFirst(this._admin); } }
        public string Character { get { return _character; } }
        public Server Dimension { get { return this._dimension; } }
        public string ID { get { return this._id; } }
        public bool Master { get { return this._master; } }
        public bool HasSlaves { get { return this._hasSlaves; } }
        public Dictionary<int, Chat> Slaves
        {
            get
            {
                if (this._slaves != null)
                    lock (this._slaves)
                        return this._slaves;
                else
                    return null;
            }
        }
        public BotState State { get { return this._state; } }
        public string Organization { get { return this._organization; } }
        public int OrganizationID { get { return this._organizationID; } }
        public bool InOrganization { get { return !(this.Organization == null || this.OrganizationID == 0); } }

        // Public settings
        private string _commandSyntax = "!";
        public string CommandSyntax { get { return this._commandSyntax; } set { this._commandSyntax = value; } }

        private string _colorHeaderHex = "FFFFFF";
        public string ColorHeader { get { return HTML.CreateColorStart(this._colorHeaderHex); } }
        public string ColorHeaderHex { get { return this._colorHeaderHex; } set { this._colorHeaderHex = value; } }

        private string _colorHeaderDetailHex = "3C8799";
        public string ColorHeaderDetail { get { return HTML.CreateColorStart(this._colorHeaderDetailHex); } }
        public string ColorHeaderDetailHex { get { return this._colorHeaderDetailHex; } set { this._colorHeaderDetailHex = value; } }

        private string _colorHighlightHex = "79CBE6";
        public string ColorHighlight { get { return HTML.CreateColorStart(this._colorHighlightHex); } }
        public string ColorHighlightHex { get { return this._colorHighlightHex; } set { this._colorHighlightHex = value; } }

        private string _colorNormalHex = "CCCCCC";
        public string ColorNormal { get { return HTML.CreateColorStart(this._colorNormalHex); } }
        public string ColorNormalHex { get { return this._colorNormalHex; } set { this._colorNormalHex = value; } }

        private int _maxWindowSizePrivateMessage = 18000;
        public int MaxWindowSizePrivateMessage { get { return this._maxWindowSizePrivateMessage; } set { this._maxWindowSizePrivateMessage = value; } }

        private int _maxWindowSizePrivateChannel = 12000;
        public int MaxWindowSizePrivateChannel { get { return this._maxWindowSizePrivateChannel; } set { this._maxWindowSizePrivateChannel = value; } }

        private int _maxWindowSizeOrganization = 12000;
        public int MaxWindowSizeOrganization { get { return this._maxWindowSizeOrganization; } set { this._maxWindowSizeOrganization = value; } }

        // Shell Modules
        private IUsers _users;
        private IUsers _usersOriginal;
        public IUsers Users { get { return this._users; } }

        private Events _events;
        public Events Events { get { return this._events; } }

        private SlaveEvents _slaveEvents;
        public SlaveEvents SlaveEvents { get { return this._slaveEvents; } }

        private Commands _commands;
        public Commands Commands { get { return this._commands; } }

        private ShellModules.Plugins _plugins;
        public ShellModules.Plugins Plugins { get { return this._plugins; } }

        private PrivateChannel _privateChannel;
        public PrivateChannel PrivateChannel { get { return this._privateChannel; } }

        private FriendList _friendList;
        public FriendList FriendList { get { return this._friendList; } }

        private Timers _timers;
        public Timers Timers { get { return this._timers; } }

        private Stats _stats;
        public Stats Stats { get { return this._stats; } }

        private ShellModules.Configuration _configuration;
        public ShellModules.Configuration Configuration { get { return this._configuration; } }

        private Bans _bans;
        public Bans Bans { get { return this._bans; } }

        private SendMessageHandler _sendMessageHandler;
        public bool CoreConnected { get { return !(this._sendMessageHandler == null); } }

        public BotShell(ConfigurationBot bot, ConfigurationCore core, SendMessageHandler messageHandler)
        {
            // Bot configuration
            this._admin = bot.Admin;
            this._character = Format.UppercaseFirst(bot.Character);
            this._dimension = (Server)Enum.Parse(typeof(Server), bot.Dimension);
            this._id = bot.GetID();
            this._master = bot.Master;
            this._sendMessageHandler = messageHandler;

            // Core configuration
            XML.SetXmlCachePath(core.CachePath);
            Config.SetConfigPath(core.ConfigPath);
            BotShell.SetDebug(core.Debug);

            // Define shell modules
            this._events = new Events();
            this._slaveEvents = new SlaveEvents();
            this._users = (IUsers)new Users(this);
            this._privateChannel = new PrivateChannel(this);
            this._friendList = new FriendList(this);
            this._timers = new Timers();
            this._stats = new Stats();
            this._plugins = new ShellModules.Plugins(this, core.PluginsPath + (!string.IsNullOrEmpty(bot.PluginsPath) ? ";" + bot.PluginsPath : ""));
            this._configuration = new ShellModules.Configuration(this);
            this._commands = new Commands(this);
            this._bans = new Bans(this);

            // Init plugins
            this._plugins.LoadPlugins();

            // Connect bot
            this.Connect(bot);
        }

        private void Connect(ConfigurationBot bot)
        {
            lock (this)
            {
                this.DisconnectMain();
                this.DisconnectSlaves();

                this._userIDs = new Dictionary<string, UInt32>();

                this._chat = new Chat();
                this._chat.ChannelJoinEvent += new AoLib.Net.ChannelJoinEventHandler(OnChannelJoin);
                this._chat.ChannelMessageEvent += new ChannelMessageEventHandler(OnChannelMessage);
                this._chat.PrivateGroupMessageEvent += new PrivChannelMessageEventHandler(OnPrivateChannelMessage);
                this._chat.TellEvent += new TellEventHandler(OnPrivateMessage);
                this._chat.StatusChangeEvent += new StatusChangeEventHandler(OnStateChanged);
                this._chat.ClientJoinEvent += new ClientJoinEventHandler(OnPrivateChannelAction);
                this._chat.BuddyStatusEvent += new BuddyStatusEventHandler(FriendList.OnNotifyAction);
                this._chat.NameLookupEvent += new NameLookupEventHandler(OnNameLookup);
                this._chat.Connect(bot.Account, bot.Password, bot.Character, this.Dimension);

                this.DisconnectSlaves();
                if (this._slaves != null)
                {
                    this._slaves.Clear();
                    this._slaves = null;
                }
                this._slaves = new Dictionary<int, Chat>();
                this._slaveIDs = new Dictionary<string, int>();
                this._hasSlaves = false;
                if (bot.Slaves != null && bot.Slaves.Length > 0)
                {
#if ADVANCED
                    int i = 1;
                    foreach (ConfigurationSlave botSlave in bot.Slaves)
                    {
                        try
                        {
                            Chat slave = new Chat();
                            slave.StatusChangeEvent += new StatusChangeEventHandler(OnStateChanged);
                            slave.PrivateGroupMessageEvent += new PrivChannelMessageEventHandler(OnPrivateChannelMessage);
                            slave.ChannelMessageEvent += new ChannelMessageEventHandler(OnChannelMessage);
                            slave.TellEvent += new TellEventHandler(OnPrivateMessage);
                            slave.ClientJoinEvent += new ClientJoinEventHandler(OnPrivateChannelAction);
                            slave.BuddyStatusEvent += new BuddyStatusEventHandler(FriendList.OnNotifyAction);
                            slave.NameLookupEvent += new NameLookupEventHandler(OnNameLookup);

                            slave.Connect(botSlave.Account, botSlave.Password, botSlave.Character, this.Dimension);
                            lock (this._slaves)
                                this._slaves.Add(i, slave);
                            lock (this._slaveIDs)
                                this._slaveIDs.Add(botSlave.Character.ToLower(), i);
                            this._hasSlaves = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        i++;
                    }
#else
                    BotShell.Output("[Error] Slaves are not supported in the " + BotShell.EDITION + " Edition");
#endif
                }
            }
        }

        public void Shutdown()
        {
            this.Cleanup();
            Environment.Exit(ExitCodes.SHUTDOWN);
        }

        public void Restart()
        {
            this.Cleanup();
            Environment.Exit(ExitCodes.RESTART);
        }

        private void Cleanup()
        {
            this._timers.Dispose();
            this.DisconnectMain();
            this.DisconnectSlaves();
            List<string> plugins = new List<string>(this.Plugins.GetLoadedPlugins());
            plugins.Reverse();
            foreach (string plugin in plugins)
            {
                this.Plugins.Unload(plugin, true);
            }
            this._commands = null;
            this._plugins = null;
            this._timers = null;
        }
        private void DisconnectMain()
        {
            try
            {
                if (this._chat != null)
                {
                    this._chat.Disconnect();
                    this._chat = null;
                }
            }
            catch { }
        }
        private void DisconnectSlaves()
        {
            if (this._hasSlaves)
            {
                if (this._slaves != null)
                {
                    foreach (KeyValuePair<int, Chat> kvp in this._slaves)
                    {
                        try
                        {
                            kvp.Value.Disconnect();
                        }
                        catch { }
                    }
                    this._slaves.Clear();
                    this._slaveIDs.Clear();
                }
            }
        }

        #region Commands
        private Chat GetOptimalBot() { return this.GetOptimalBot(false); }
        private Chat GetOptimalBot(Boolean prefereSlaves)
        {
            int count = this.GetMainBot().SlowQueueCount;
            if (prefereSlaves)
                count = int.MaxValue;
            Chat bot = this.GetMainBot();
            if (this._hasSlaves)
            {
                if (this._slaves != null)
                {
                    lock (this._slaves)
                    {
                        foreach (KeyValuePair<int, Chat> kvp in this._slaves)
                        {
                            try
                            {
                                if (kvp.Value.SlowQueueCount < count && kvp.Value.State == ChatState.Connected)
                                {
                                    bot = kvp.Value;
                                    count = kvp.Value.SlowQueueCount;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            lock (bot)
            {
                return bot;
            }
        }

        public Chat GetMainBot()
        {
            if (this._chat == null)
            {
                return null;
            }
            lock (this._chat)
            {
                return this._chat;
            }
        }

        public Chat GetSlaveBot(Int32 slaveID)
        {
            lock (this._slaves)
                if (this._slaves.ContainsKey(slaveID))
                    return this._slaves[slaveID];
            return null;
        }

        public Int32 GetSlaveID(string slave)
        {
            lock (this._slaveIDs)
            {
                if (this._slaveIDs.ContainsKey(slave.ToLower()))
                {
                    return this._slaveIDs[slave.ToLower()];
                }
                return 0;
            }
        }

        public string GetSlaveName(Int32 slaveID)
        {
            lock (this._slaveIDs)
                if (this._slaveIDs.ContainsValue(slaveID))
                    foreach (KeyValuePair<string, Int32> slave in this._slaveIDs)
                        if (slave.Value == slaveID)
                            return slave.Key;
            return null;
        }

        public Int32 GetSlavesCount()
        {
            lock (this._slaveIDs)
            {
                return this._slaves.Count;
            }
        }

        public UInt32 GetUserID(string user)
        {
            user = Format.UppercaseFirst(user);
            bool lookup = true;
            for (int i = 0; i < 15; i++)
            {
                if (this._userIDs == null)
                    return 0;

                lock (this._userIDs)
                    if (this._userIDs.ContainsKey(user))
                        if (this._userIDs[user] > 0)
                            return this._userIDs[user];
                        else
                        {
                            this._userIDs.Remove(user);
                            return 0;
                        }


                if (lookup)
                {
                    if (this.GetMainBot() == null)
                        return 0;

                    this.GetMainBot().SendNameLookup(user);
                    lookup = false;
                }
                Thread.Sleep(1000);
            }
            return 0;
        }

        public string GetUserName(UInt32 userID)
        {
            if (userID == 0 || userID == UInt32.MaxValue)
                return null;
            lock (this._userIDs)
            {
                if (this._userIDs.ContainsValue(userID))
                {
                    foreach (KeyValuePair<string, UInt32> kvp in this._userIDs)
                    {
                        if (kvp.Value == userID)
                            return kvp.Key;
                    }
                }
            }
            return null;
        }

        public void SendPrivateMessage(string user, string message) { this.SendPrivateMessage(user, message, true); }
        public void SendPrivateMessage(string user, string message, bool optimize) { this.SendPrivateMessage(0, user, message, PacketQueue.Priority.Standard, optimize); }
        public void SendPrivateMessage(string user, string message, PacketQueue.Priority priority, bool optimize) { this.SendPrivateMessage(0, user, message, priority, optimize); }
        public void SendPrivateMessage(UInt32 userid, string message) { this.SendPrivateMessage(userid, message, true); }
        public void SendPrivateMessage(UInt32 userid, string message, bool optimize) { this.SendPrivateMessage(userid, string.Empty, message, PacketQueue.Priority.Standard, optimize); }
        public void SendPrivateMessage(UInt32 userid, string message, PacketQueue.Priority priority, bool optimize) { this.SendPrivateMessage(userid, string.Empty, message, priority, optimize); }
        private void SendPrivateMessage(UInt32 userid, string user, string message, PacketQueue.Priority priority, bool optimize)
        {
            if (this.GetMainBot() == null)
                return;

            this.Stats.Counter_PrivateMessages_Sent++;

            Chat bot;
            if (optimize)
                bot = this.GetOptimalBot();
            else
                bot = this.GetMainBot();

            if (bot == null)
                return;

            if (user != string.Empty && user != null)
                bot.SendPrivateMessage(user, message);
            else
                bot.SendPrivateMessage(userid, message);
        }
        public void SendPrivateMessage(string user, string message, Int32 slaveID) { this.SendPrivateMessage(this.GetUserID(user), message, slaveID); }
        public void SendPrivateMessage(UInt32 userid, string message, Int32 slaveID)
        {
            lock (this.Slaves)
                if (this.Slaves.ContainsKey(slaveID))
                    this.Slaves[slaveID].SendPrivateMessage(userid, message);
        }


        public void SendPrivateChannelMessage(string message)
        {
            if (this.GetMainBot() == null)
                return;

            this.Stats.Counter_PrivateChannelMessages_Sent++;
            this.GetMainBot().SendPrivateChannelMessage(message);
        }
        public void SendPrivateChannelMessage(UInt32 channelid, string message)
        {
            if (this.GetMainBot() == null)
                return;

            this.Stats.Counter_PrivateChannelMessages_Sent++;
            this.GetMainBot().SendPrivateChannelMessage(channelid, message);
        }
        public void SendPrivateChannelMessage(string message, Int32 slaveID)
        {
            lock (this.Slaves)
                if (this.Slaves.ContainsKey(slaveID))
                    this.Slaves[slaveID].SendPrivateChannelMessage(message);
        }

        public void SendChannelMessage(string channel, string message) { this.GetMainBot().SendChannelMessage(channel, message); }
        public void SendChannelMessage(Int32 channelid, string message) { this.SendChannelMessage(new BigInteger((long)channelid), message); }
        public void SendChannelMessage(BigInteger channelid, string message)
        {
            if (this.GetMainBot() == null)
                return;

            this.Stats.Counter_ChannelMessages_Sent++;
            this.GetMainBot().SendChannelMessage(channelid, message);
        }

        public void SendOrganizationMessage(string message)
        {
            if (this.GetMainBot() == null)
                return;

            BigInteger id = this.GetMainBot().OrganizationID;
            if (id > 0)
                this.SendChannelMessage(id, message);
        }

        public void SendReply(CommandArgs args, string message) { this.SendReply(args, message, null, true); }
        public void SendReply(CommandArgs args, string message, TextWindow window) { this.SendReply(args, message, window, null); }
        public void SendReply(CommandArgs args, string message, TextWindow window, string title) { this.SendReply(args, message, window, title, true); }
        public void SendReply(CommandArgs args, string message, TextWindow window, string title, bool optimize)
        {
            string[] windows;
            int windowSize;
            switch (args.Type)
            {
                case CommandType.Tell:
                    windowSize = this.MaxWindowSizePrivateMessage;
                    break;
                case CommandType.PrivateChannel:
                    windowSize = this.MaxWindowSizePrivateChannel;
                    break;
                case CommandType.Organization:
                default:
                    windowSize = this.MaxWindowSizeOrganization;
                    break;
            }
            if (title != null && title != string.Empty)
                windows = window.ToStrings(title, windowSize);
            else
                windows = window.ToStrings(windowSize);
            this.SendReply(args, message, windows, optimize);
        }
        private void SendReply(CommandArgs args, string message, string[] windows, bool optimize)
        {
            List<string> pages = new List<string>();
            if (windows != null)
            {
                Int32 i = 0;
                foreach (string page in windows)
                {
                    i++;
                    string count = string.Empty;
                    if (windows.Length > 1)
                        count = " (Page " + i + " of " + windows.Length + ")";
                    pages.Add(this.ColorHighlight + message + page + count);
                }
            }
            else
                pages.Add(this.ColorHighlight + message);

            Chat bot = this.GetMainBot();
            if (optimize)
                bot = this.GetOptimalBot();
            foreach (string page in pages)
            {
                switch (args.Type)
                {
                    case CommandType.Organization:
                        this.SendOrganizationMessage(page);
                        break;
                    case CommandType.PrivateChannel:
                        this.SendPrivateChannelMessage(page);
                        break;
                    case CommandType.Tell:
                        bot.SendPrivateMessage(args.SenderID, page);
                        break;
                    default:
                        return;
                }
            }
        }

        public bool IsBot(string user)
        {
            if (user == null || user == string.Empty)
                return false;
            user = user.ToLower().Trim();
            if (this.Character.ToLower() == user)
                return true;
            if (this.HasSlaves && this.GetSlaveID(user) > 0)
                return true;
            return false;
        }
        #endregion

        #region Event Handlers
        private void OnNameLookup(object sender, NameLookupEventArgs e)
        {
            lock (this._userIDs)
                this._userIDs[Format.UppercaseFirst(e.Name)] = e.BuddyID;
        }

        private void OnChannelMessage(object sender, ChannelMessageEventArgs e)
        {
            Chat bot = (Chat)sender;
            ChannelType type = ChannelType.Unknown;
            try { type = (ChannelType)Enum.Parse(typeof(ChannelType), e.Type.ToString()); }
            catch { }
            ChannelMessageArgs args = new ChannelMessageArgs(this, e.SenderID, bot.GetUserName(e.SenderID), e.ChannelID.IntValue(), bot.GetChannelName(e.ChannelID), e.Message, type, false, (e.SenderID == bot.ID));
            if (type == ChannelType.Organization)
            {
                if (e.SenderID != bot.ID)
                {
                    // Auto-ban only available on organization channel to prevent lag due to public channels
                    if (e.SenderID != bot.ID) if (this.Bans.IsAutoBanned(args.SenderWhois)) return;
                    if (this.Bans.IsBanned(e.SenderID)) return;
                    if (this.Bans.IsBanned(this.GetUserName(e.SenderID))) return;

                    this.Stats.Counter_ChannelMessages_Received++;
                    if (bot.Character == this.Character)
                    {
                        if (e.Message.Length > this.CommandSyntax.Length)
                        {
                            if (e.Message.Substring(0, this.CommandSyntax.Length) == this.CommandSyntax)
                            {
                                CommandArgs commandArgs = new CommandArgs(CommandType.Organization, (UInt32)e.ChannelID.IntValue(), e.SenderID, args.Sender, args.SenderWhois, "", e.Message.Substring(this.CommandSyntax.Length), false, null);
                                args.Command = this.OnCommand(commandArgs);
                            }
                        }
                    }
                }
            }
            if (bot.Character == this.Character)
                this.Events.OnChannelMessage(this, args);
            else
                this.SlaveEvents.OnChannelMessage(this, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)), args);
        }

        private void OnPrivateChannelMessage(object sender, PrivateChannelMessageEventArgs e)
        {
            Chat bot = (Chat)sender;
            PrivateChannelMessageArgs args = new PrivateChannelMessageArgs(this, e.SenderID, bot.GetUserName(e.SenderID), e.PrivateGroupID, bot.GetUserName(e.PrivateGroupID), e.Local, e.Message, false, (e.SenderID == bot.ID));
            if (e.SenderID != bot.ID)
            {
                if (this.Bans.IsAutoBanned(args.SenderWhois)) return;
                if (this.Bans.IsBanned(e.SenderID)) return;
                if (this.Bans.IsBanned(this.GetUserName(e.SenderID))) return;

                this.Stats.Counter_PrivateChannelMessages_Received++;
                if (e.Message.Length > this.CommandSyntax.Length)
                {
                    if (e.Message.Substring(0, this.CommandSyntax.Length) == this.CommandSyntax)
                    {
                        CommandArgs commandArgs;
                        if (e.Local == true)
                        {
                            if (bot.Character == this.Character)
                                commandArgs = new CommandArgs(CommandType.PrivateChannel, e.PrivateGroupID, e.SenderID, args.Sender, args.SenderWhois, "", e.Message.Substring(this.CommandSyntax.Length), false, null);
                            else
                                commandArgs = new CommandArgs(CommandType.PrivateChannel, e.PrivateGroupID, e.SenderID, args.Sender, args.SenderWhois, "", e.Message.Substring(this.CommandSyntax.Length), true, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)));
                            args.Command = this.OnCommand(commandArgs);
                        }
                    }
                }
            }
            if (bot.Character == this.Character)
                this.Events.OnPrivateChannelMessage(this, args);
            else
                this.SlaveEvents.OnPrivateChannelMessage(this, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)), args);
        }

        private void OnPrivateMessage(object sender, TellEventArgs e)
        {
            if (!e.Outgoing)
            {
                // Auto-ignore certain messages
                foreach (string ignoredMessage in BotShell.IgnoredMessages)
                {
                    string ignored = ignoredMessage.Replace("{SENDER}", this.GetUserName(e.SenderID));
                    if (e.Message.StartsWith(ignored, StringComparison.CurrentCultureIgnoreCase)) return;
                }

                // Check for bans
                if (this.Bans.IsBanned(e.SenderID)) return;
                if (this.Bans.IsBanned(this.GetUserName(e.SenderID))) return;

                this.Stats.Counter_PrivateMessages_Received++;

                string message = e.Message;
                if (e.Message.Length > this.CommandSyntax.Length)
                {
                    if (e.Message.Substring(0, this.CommandSyntax.Length) == this.CommandSyntax)
                    {
                        message = e.Message.Substring(this.CommandSyntax.Length);
                    }
                }
                Chat bot = (Chat)sender;
                PrivateMessageArgs args = new PrivateMessageArgs(this, e.SenderID, this.GetUserName(e.SenderID), e.Message, false, false);
                if (this.Bans.IsAutoBanned(args.SenderWhois)) return;
                CommandArgs commandArgs;
                if (bot.Character == this.Character)
                    commandArgs = new CommandArgs(CommandType.Tell, e.SenderID, e.SenderID, args.Sender, args.SenderWhois, "", message, false, null);
                else
                    commandArgs = new CommandArgs(CommandType.Tell, e.SenderID, e.SenderID, args.Sender, args.SenderWhois, "", message, true, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)));
                args.Command = this.OnCommand(commandArgs);

                if (bot.Character == this.Character)
                    this.Events.OnPrivateMessage(this, args);
                else
                    this.SlaveEvents.OnPrivateMessage(this, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)), args);

                if (args.Command == false && args.DisableAutoMessage == false)
                    this.SendReply(commandArgs, "Unknown command input. " + HTML.CreateColorString(this.ColorHeaderHex, "/tell " + this.Character + " help"), null, false);
            }
            else
            {
                this.Stats.Counter_PrivateMessages_Sent++;
                Chat bot = (Chat)sender;
                PrivateMessageArgs eventArgs = new PrivateMessageArgs(this, e.SenderID, this.GetUserName(e.SenderID), e.Message, false, true);
                if (bot.Character == this.Character)
                    this.Events.OnPrivateMessage(this, eventArgs);
                else
                    this.SlaveEvents.OnPrivateMessage(this, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)), eventArgs);
            }
        }

        private void OnPrivateChannelAction(object sender, ClientJoinEventArgs e)
        {
            Chat bot = (Chat)sender;
            bool local;
            if (bot.ID == e.PrivateGroupID)
                local = true;
            else
                local = false;

            if (e.Join)
            {
                UserJoinChannelArgs args = new UserJoinChannelArgs(this, e.SenderID, bot.GetUserName(e.SenderID), e.PrivateGroupID, bot.GetUserName(e.PrivateGroupID), local);
                if (bot.Character == this.Character)
                    this.Events.OnUserJoinChannel(this, args);
                else
                    this.SlaveEvents.OnUserJoinChannel(this, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)), args);
            }
            else
            {
                UserLeaveChannelArgs args = new UserLeaveChannelArgs(this, e.SenderID, bot.GetUserName(e.SenderID), e.PrivateGroupID, bot.GetUserName(e.PrivateGroupID), local);
                if (bot.Character == this.Character)
                    this.Events.OnUserLeaveChannel(this, args);
                else
                    this.SlaveEvents.OnUserLeaveChannel(this, new SlaveArgs(bot.Character, this.GetSlaveID(bot.Character)), args);
            }
        }

        private void OnChannelJoin(object sender, AoLib.Net.ChannelJoinEventArgs e)
        {
            if (e.GroupType == AoLib.Net.ChannelType.Organization)
            {
                this._organizationID = e.GroupID.IntValue();
                this._organization = e.GroupName;
            }
            ChannelType chanType = (ChannelType)Enum.Parse(typeof(ChannelType),e.GroupType.ToString());
            ChannelJoinEventArgs args = new ChannelJoinEventArgs(e.GroupID, e.GroupName, e.Mute, e.Logging, chanType);
            this.Events.OnChannelJoin(this, args);
        }

        private void OnStateChanged(object sender, StatusChangeEventArgs e)
        {
            try
            {
                Chat bot = (Chat)sender;
                BotState state;
                switch (e.State)
                {
                    case ChatState.Connected:
                        state = BotState.Connected;
                        break;
                    case ChatState.Disconnected:
                        state = BotState.Disconnected;
                        break;
                    default:
                        state = BotState.Connecting;
                        break;
                }
                BotStateChangedArgs args;
                if (bot.Character == this._character && bot.Dimension == this._dimension)
                {
                    BotShell.Output("[" + bot.Character + "] " + e.State.ToString());
                    this._state = state;
                    args = new BotStateChangedArgs(state, false, 0, bot.Character);
                    this._organization = null;
                    this._organizationID = 0;
                }
                else
                {
                    BotShell.Output("[" + bot.Character + "] " + e.State.ToString());
                    args = new BotStateChangedArgs(state, true, this.GetSlaveID(bot.Character), bot.Character);
                }
                this.Events.OnBotStateChanged(this, args);
            }
            catch { }
        }
        #endregion

        #region Event Throwers
        private bool OnCommand(CommandArgs args)
        {
            int spaceCount = args.Args.Length;
            for (int i = spaceCount; i > 0; i--)
            {
                string command = String.Join(" ", args.Args, 0, i).ToLower();
                if (this.Commands.Exists(command))
                {
                    string message = args.Message.Substring(command.Length);
                    command = this.Commands.GetMainCommand(command);
                    UserLevel userlevel = this.Commands.GetRight(command, args.Type);
                    CommandArgs newArgs = new CommandArgs(args.Type, args.ChannelID, args.SenderID, args.Sender, args.SenderWhois, command, message, args.FromSlave, args.SlaveArgs);
                    if (this.Users.Authorized(args.Sender, userlevel))
                        newArgs.Authorized = true;
                    else
                        newArgs.Authorized = false;
                    try
                    {
                        PluginBase plugin = this.Plugins.GetPlugin(this.Commands.GetInternalName(command));
                        plugin.FireOnCommand(this, newArgs);
                        if (this.Stats != null)
                            this.Stats.Counter_Commands++;
                        if (newArgs != null && !newArgs.Authorized)
                        {
                            if (newArgs.Type == CommandType.Tell)
                            {
                                UserLevel senderlevel = this.Users.GetUser(args.Sender);
                                if (senderlevel < userlevel)
                                    this.SendReply(newArgs, "You're not authorized to use this command! Your user level is required to be at least " + HTML.CreateColorString(this.ColorHeaderHex, userlevel.ToString()));
                                else
                                    this.SendReply(newArgs, "You're not authorized to use this command!");
                                return true;
                            }
                            return false;
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        BotShell.Output("[Plugin Execution Error] " + ex.ToString());
                    }
                }
            }
            return false;
        }
        #endregion

        #region Override Shell Modules
        public bool UsersOverride(IUsers users)
        {
            if (users == null)
                return false;
            if (this._usersOriginal == null)
                this._usersOriginal = this._users;
            this._users = users;
            return true;
        }

        public bool UsersRestore()
        {
            if (this._usersOriginal == null)
                return false;
            this._users = this._usersOriginal;
            this._usersOriginal = null;
            return true;
        }
        #endregion

        #region Intra Bot Messages
        public ReplyMessage SendPluginMessageAndWait(string sourcePlugin, string targetPlugin, string command, int timeout, params object[] args)
        {
            int id;
            MessageResult result = this.SendPluginMessage(sourcePlugin, targetPlugin, command, out id, args);
            if (result != MessageResult.Success) return null;
            return this.GetReplyMessage(id, timeout);
        }

        public MessageResult SendPluginMessage(string sourcePlugin, string targetPlugin, string command, out int id, params object[] args)
        {
            id = -1;
            if (sourcePlugin == null || !this.Plugins.IsLoaded(sourcePlugin)) return MessageResult.InvalidSource;
            if (targetPlugin == null || !this.Plugins.IsLoaded(targetPlugin)) return MessageResult.InvalidTarget;
            if (command == null || command == string.Empty) return MessageResult.InvalidMessage;
            PluginMessage message = new PluginMessage(this.ID, this.ID, targetPlugin, sourcePlugin, command, args);
            id = message.ID;
            if (this.RelayMessage(message))
                return MessageResult.Success;
            else
                return MessageResult.Error;
        }

        public ReplyMessage SendRemotePluginMessageAndWait(string sourcePlugin, string target, string targetPlugin, string command, int timeout, params object[] args)
        {
            int id;
            MessageResult result = this.SendRemotePluginMessage(sourcePlugin, target, targetPlugin, command, out id, args);
            if (result != MessageResult.Success) return null;
            return this.GetReplyMessage(id, timeout);
        }

        public MessageResult SendRemotePluginMessage(string sourcePlugin, string target, string targetPlugin, string command, out int id, params object[] args)
        {
            id = -1;
            if (this._sendMessageHandler == null) return MessageResult.NotConnected;
            if (target == this.ID || target == null) return this.SendPluginMessage(sourcePlugin, targetPlugin, command, out id, args);
            if (sourcePlugin == null) return MessageResult.InvalidSource;
            if (targetPlugin == null) return MessageResult.InvalidTarget;
            if (command == null || command == string.Empty) return MessageResult.InvalidMessage;
            PluginMessage message = new PluginMessage(target, this.ID, targetPlugin, sourcePlugin, command, args);
            id = message.ID;
            lock (this._sendMessageHandler)
                try { return this._sendMessageHandler(message); }
                catch { return MessageResult.Error; }
        }

        public ReplyMessage[] SendBotMessageAndWait(string sourcePlugin, string command, int timeout, params object[] args)
        {
            int id;
            MessageResult result = this.SendBotMessage(sourcePlugin, command, out id, args);
            if (result != MessageResult.Success) return null;
            return this.GetReplyMessages(id, timeout);
        }

        public MessageResult SendBotMessage(string sourcePlugin, string command, out int id, params object[] args)
        {
            id = -1;
            if (sourcePlugin == null || !this.Plugins.IsLoaded(sourcePlugin)) return MessageResult.InvalidSource;
            if (command == null || command == string.Empty) return MessageResult.InvalidMessage;
            BotMessage message = new BotMessage(this.ID, this.ID, sourcePlugin, command, args);
            id = message.ID;
            if (this.RelayMessage(message))
                return MessageResult.Success;
            else
                return MessageResult.Error;
        }

        public ReplyMessage[] SendRemoteBotMessageAndWait(string sourcePlugin, string target, string command, int timeout, params object[] args)
        {
            int id;
            MessageResult result = this.SendRemoteBotMessage(sourcePlugin, target, command, out id, args);
            if (result != MessageResult.Success) return null;
            return this.GetReplyMessages(id, timeout);
        }

        public MessageResult SendRemoteBotMessage(string sourcePlugin, string target, string command, out int id, params object[] args)
        {
            id = -1;
            if (this._sendMessageHandler == null) return MessageResult.NotConnected;
            if (target == this.ID) return this.SendBotMessage(sourcePlugin, command, out id, args);
            if (sourcePlugin == null) return MessageResult.InvalidSource;
            if (command == null || command == string.Empty) return MessageResult.InvalidMessage;
            BotMessage message = new BotMessage(target, this.ID, sourcePlugin, command, args);
            id = message.ID;
            lock (this._sendMessageHandler)
                try { return this._sendMessageHandler(message); }
                catch { return MessageResult.Error; }
        }

        public MessageResult SendReplyMessage(string sourcePlugin, PluginMessage target, params object[] args) { return this.SendReplyMessage(sourcePlugin, (MessageBase)target, args); }
        public MessageResult SendReplyMessage(string sourcePlugin, BotMessage target, params object[] args) { return this.SendReplyMessage(sourcePlugin, (MessageBase)target, args); }
        private MessageResult SendReplyMessage(string sourcePlugin, MessageBase target, params object[] args)
        {
            if (target == null) return MessageResult.InvalidTarget;
            if (args.Length < 1) return MessageResult.InvalidMessage;
            if (sourcePlugin == null) return MessageResult.InvalidSource;
            ReplyMessage message = new ReplyMessage(target, sourcePlugin, args);
            if (message.Target == this.ID)
            {
                if (this.RelayMessage(message))
                    return MessageResult.Success;
                else
                    return MessageResult.Error;
            }
            else
            {
                if (this._sendMessageHandler == null) return MessageResult.NotConnected;
                lock (this._sendMessageHandler)
                    try { return this._sendMessageHandler(message); }
                    catch { return MessageResult.Error; }
            }
        }

        public MessageResult SendCoreMessage(string sourcePlugin, string target, CoreCommand command)
        {
            if (this._sendMessageHandler == null) return MessageResult.NotConnected;
            if (target == null) return MessageResult.InvalidTarget;
            CoreMessage message = new CoreMessage(target, this.ID, sourcePlugin, command);
            lock (this._sendMessageHandler)
                try { return this._sendMessageHandler(message); }
                catch { return MessageResult.Error; }
        }

        public ReplyMessage GetReplyMessage(int id) { return this.GetReplyMessage(id, 5000); }
        public ReplyMessage GetReplyMessage(int id, int timeout)
        {
            if (timeout > 600000) timeout = 600000;
            DateTime start = DateTime.Now;
            while (true)
            {
                TimeSpan span = DateTime.Now - start;
                if (span.TotalMilliseconds > timeout)
                    return null;
                lock (this._replies)
                    foreach (ReplyMessage reply in this._replies)
                        if (reply.ID == id)
                            return reply;
                Thread.Sleep(100);
            }
        }

        public ReplyMessage[] GetReplyMessages(int id) { return this.GetReplyMessages(id, 5000); }
        public ReplyMessage[] GetReplyMessages(int id, int timeout)
        {
            DateTime start = DateTime.Now;
            List<ReplyMessage> result = new List<ReplyMessage>();
            while (true)
            {
                TimeSpan span = DateTime.Now - start;
                if (span.TotalMilliseconds > timeout)
                    break;
                lock (this._replies)
                    foreach (ReplyMessage reply in this._replies)
                        if (reply.ID == id)
                            result.Add(reply);
                Thread.Sleep(100);
            }
            return result.ToArray();
        }

        public bool RelayMessage(MessageBase message)
        {
            if (message == null) return false;
            switch (message.Type)
            {
                case MessageType.Plugin:
                    PluginMessage pluginMessage = (PluginMessage)message;
                    if (!this.Plugins.IsLoaded(pluginMessage.TargetPlugin)) return false;
                    try
                    {
                        this.Plugins.GetPlugin(pluginMessage.TargetPlugin).OnPluginMessage(this, pluginMessage);
                        return true;
                    }
                    catch { return false; }
                case MessageType.Bot:
                    BotMessage botMessage = (BotMessage)message;
                    foreach (string plugin in this.Plugins.GetLoadedPlugins())
                    {
                        try
                        {
                            this.Plugins.GetPlugin(plugin).OnBotMessage(this, botMessage);
                        }
                        catch { }
                    }
                    return true;
                case MessageType.Reply:
                    lock (this._replies)
                        this._replies.Add((ReplyMessage)message);
                    return true;
                default:
                    throw new ArgumentException("Unsupported MessageType (" + message.Type + ") has been received by BotShell.RelayMessage()");
            }
        }
        #endregion

        public static void Output(string msg) { BotShell.Output(msg, false); }
        public static void Output(string msg, bool debug)
        {
            if (debug && !BotShell.Debug) return;
            Console.WriteLine(msg, debug);
        }

        public void Clean()
        {
            // Clean replies
            if (this._replies.Count > 0)
            {
                lock (this._replies)
                {
                    List<ReplyMessage> remove = new List<ReplyMessage>();
                    foreach (ReplyMessage reply in this._replies)
                    {
                        TimeSpan span = DateTime.Now - reply.Time;
                        if (span.TotalMinutes >= 1)
                            remove.Add(reply);
                    }
                    foreach (ReplyMessage reply in remove)
                    {
                        this._replies.Remove(reply);
                    }
                }
            }
            // Clean memory
            GC.Collect();
        }

        [Obsolete("Please use BotShell.ID instead of this function")]
        public override string ToString()
        {
            return this._character.ToLower() + "@" + this._dimension.ToString().ToLower();
        }
    }
}
