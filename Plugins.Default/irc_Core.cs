using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using AoLib.Utils;
using Sharkbite.Irc;
using VhaBot.Communication;

namespace VhaBot.Plugins
{
    public class VhSimpleIRC : PluginBase
    {
        public readonly string BOLD = Convert.ToChar(2).ToString();
        public readonly string COLOR = Convert.ToChar(3).ToString();
        public readonly string REVERSE = Convert.ToChar(22).ToString();
        public readonly string UNDERLINE = Convert.ToChar(31).ToString();

        private Plugins.IRCQueue.PrioQueue<Plugins.IRCQueue.IRCQueueItem> IRCQueue;

        private string ItemFormat;
        private string ItemPattern;
        private string Server = "irc.funcom.com";
        private string ServerPassword;
        private int Port = 6667;
        private string Nickname;
        private string Channel = string.Empty, LogChannel = string.Empty;
        private string Password = string.Empty, LogPassword = string.Empty;
        private Connection _IRC;
        private Connection IRC
        {
            get
            {
                if (this._IRC != null)
                    lock (this._IRC)
                        return this._IRC;
                else
                    return null;
            }
        }
        private int MessageCap = 400;
        private string GuildColor = string.Empty;
        private string RelayMode = "guild";
        private string ConnectSyntaxIrc = "off";
        private string ConnectSyntaxAo = "off";
        private int ConnectDelay = 45;

        private bool Connected = false;
        private bool Closing = true;
        private System.Timers.Timer ReconnectTimer = new System.Timers.Timer();
        private System.Timers.Timer QueueTimer = new System.Timers.Timer();
        private BotShell Bot;

        public VhSimpleIRC()
        {
            this.Name = "IRC Relay";
            this.InternalName = "ircCore";
            this.Author = "Vhab / Iriche";
            this.Version = 124;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("irc", false, UserLevel.Admin),
                new Command("irc set", false, UserLevel.Admin),
                new Command("irc restart", false, UserLevel.Admin),
                new Command("irc start", false, UserLevel.Admin),
                new Command("irc stop", false, UserLevel.Admin),
                new Command("irc raw", false, UserLevel.Admin),
                new Command("irc connect", "irc start"),
                new Command("irc disconnect", "irc stop"),
                new Command("say", false, UserLevel.Admin)
            };

            this.ItemFormat = this.COLOR + this.COLOR + "{0}" + this.COLOR + " " + this.COLOR + "(http://auno.org/ao/db.php?id={1}&id2={2}&ql={3})" + this.COLOR + this.COLOR;
            this.ItemPattern = this.COLOR + this.COLOR + "(.+?)" + this.COLOR + " " + this.COLOR + "[(](.+?)id=([0-9]+)&amp;id2=([0-9]+)&amp;ql=([0-9]+)[)]" + this.COLOR + this.COLOR;
        }

        public override void OnLoad(BotShell bot)
        {
            this.IRCQueue = new Plugins.IRCQueue.PrioQueue<Plugins.IRCQueue.IRCQueueItem>();
            bot.Events.PrivateChannelMessageEvent += new PrivateChannelMessageHandler(Events_PrivateChannelMessageEvent);
            bot.Events.ChannelMessageEvent += new ChannelMessageHandler(Events_ChannelMessageEvent);
            bot.Events.UserJoinChannelEvent += new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.UserLeaveChannelEvent += new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
            bot.Timers.Minute += new EventHandler(Events_Ping);

            bot.Configuration.Register(ConfigType.String, this.InternalName, "server", "IRC Server Address", this.Server);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "port", "IRC Server Port", this.Port);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "server_password", "IRC Server Password", this.ServerPassword);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "nickname", "Nickname", bot.Character);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "channel", "Channel", this.Channel);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "password", "Channel Password", this.Password);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "logchannel", "Log Channel", this.LogChannel);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "logpassword", "Log Password", this.LogPassword);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "cap", "IRC Message Cap", this.MessageCap);
            bot.Configuration.Register(ConfigType.Color, this.InternalName, "color", "Message Color", this.GuildColor);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "mode", "Relay Mode", "both", "both", "guild", "guest", "none");
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "autostart", "Auto-Connect", false);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "aoconnect", "AO Connect Syntax", this.ConnectSyntaxAo);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "ircconnect", "IRC Connect Syntax", this.ConnectSyntaxIrc);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "delay", "IRC Reconnect Delay", this.ConnectDelay);

            this.Bot = bot;
            this.ReconnectTimer.Elapsed += new ElapsedEventHandler(Reconnect);
            this.ReconnectTimer.Interval = 30000;
            this.QueueTimer.Elapsed += new ElapsedEventHandler(QueueTimer_Elapsed);
            this.QueueTimer.Interval = 350;
            this.LoadSettings();

            if (this.Bot.Configuration.GetBoolean(this.InternalName, "autostart", false))
                this.StartLink();
        }

        void QueueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.Connected)
            {
                lock (this.IRCQueue)
                {
                    if (this.IRCQueue.Count > 0)
                    {
                        Plugins.IRCQueue.IRCQueueItem message = this.IRCQueue.Dequeue();
                        if (message.Target.StartsWith("#"))
                            this.IRC.Sender.PublicMessage(message.Target, message.Message);
                        else
                            this.IRC.Sender.PrivateNotice(message.Target, message.Message);
                    }
                }
            }
        }



        public override void OnUnload(BotShell bot)
        {
            this.Bot.Events.PrivateChannelMessageEvent -= new PrivateChannelMessageHandler(Events_PrivateChannelMessageEvent);
            this.Bot.Events.ChannelMessageEvent -= new ChannelMessageHandler(Events_ChannelMessageEvent);
            this.Bot.Events.UserJoinChannelEvent -= new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            this.Bot.Events.UserLeaveChannelEvent -= new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
            this.Bot.Timers.Minute -= new EventHandler(Events_Ping);
            this.StopLink();
        }

        private void Events_ChannelMessageEvent(BotShell bot, ChannelMessageArgs e)
        {
            if (e.Command) { return; }
            if (e.Self) { return; }
            if (e.Type == ChannelType.Organization)
            {
                string message = e.Message;
                if (this.ConnectSyntaxAo != "off")
                    if (message.StartsWith(this.ConnectSyntaxAo, StringComparison.CurrentCultureIgnoreCase))
                        message = message.Substring(this.ConnectSyntaxAo.Length);
                    else
                        return;

                string nick = string.Empty;
                if (e.SenderID != 0)
                    nick = e.Sender + ": ";

                string formattedMessage = String.Format(this.BOLD + this.BOLD + this.BOLD + "[{0}]" + this.BOLD + " {1}{2}", e.Channel, nick, message);
                if (this.RelayMode == "both" || this.RelayMode == "guild")
                {
                    this.SendMessage(formattedMessage, "guest");
                    if (e.Items != null && e.Items.Length > 0)
                        foreach (AoItem item in e.Items)
                            formattedMessage = formattedMessage.Replace(item.Raw, string.Format(this.ItemFormat, item.Name, item.LowID, item.HighID, item.QL));
                    this.SendMessage(formattedMessage, "irc");
                }
            }
        }

        private void Events_PrivateChannelMessageEvent(BotShell bot, PrivateChannelMessageArgs e)
        {
            if (e.Command) { return; }
            if (e.Self) { return; }
            if (e.Local)
            {
                if (this.RelayMode == "both" || this.RelayMode == "guest")
                {
                    string message = e.Message;
                    if (this.ConnectSyntaxAo != "off")
                        if (message.StartsWith(this.ConnectSyntaxAo, StringComparison.CurrentCultureIgnoreCase))
                            message = message.Substring(this.ConnectSyntaxAo.Length);
                        else
                            return;

                    string formattedMessage = String.Format(this.BOLD + this.BOLD + this.BOLD + "[{0}'s Guest]" + this.BOLD + " {1}: {2}", bot.Character, e.Sender, message);
                    this.SendMessage(formattedMessage, "guild");
                    if (e.Items != null && e.Items.Length > 0)
                        foreach (AoItem item in e.Items)
                            formattedMessage = formattedMessage.Replace(item.Raw, string.Format(this.ItemFormat, item.Name, item.LowID, item.HighID, item.QL));
                    this.SendMessage(formattedMessage, "irc");
                }
            }
        }

        private void Events_UserLeaveChannelEvent(BotShell bot, UserLeaveChannelArgs e)
        {
            if (e.Local)
                if (this.RelayMode == "both" || this.RelayMode == "guest")
                    this.SendMessage(this.BOLD + this.BOLD + this.BOLD + "[" + bot.Character + "'s Guest]" + this.BOLD + " " + e.Sender + " has left the private channel", "irc");
        }

        private void Events_UserJoinChannelEvent(BotShell bot, UserJoinChannelArgs e)
        {
            if (e.Local)
                if (this.RelayMode == "both" || this.RelayMode == "guest")
                    this.SendMessage(this.BOLD + this.BOLD + this.BOLD + "[" + bot.Character + "'s Guest]" + this.BOLD + " " + Format.Whois(e.Sender, bot.Dimension, FormatStyle.Medium) + " has joined the private channel", "irc");
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "irc":
                    RichTextWindow window = new RichTextWindow(bot);

                    string password = "";
                    for (int i = 0; i < this.Password.Length; i++)
                    {
                        password += "*";
                    }

                    window.AppendTitle("SimpleIRC Configuration");
                    window.AppendHighlight("Server: ");
                    window.AppendNormal(this.Server);
                    window.AppendLineBreak();

                    window.AppendHighlight("Port: ");
                    window.AppendNormal(this.Port.ToString());
                    window.AppendLineBreak();

                    window.AppendHighlight("Nickname: ");
                    window.AppendNormal(this.Nickname);
                    window.AppendLineBreak();

                    window.AppendHighlight("Channel: ");
                    window.AppendNormal(this.Channel);
                    window.AppendLineBreak();

                    window.AppendHighlight("Password: ");
                    window.AppendNormal(password);
                    window.AppendLineBreak();

                    window.AppendHighlight("Auto Start: ");
                    if (this.Bot.Configuration.GetBoolean(this.InternalName, "autostart", false))
                    {
                        window.AppendNormal("On");
                    }
                    else
                    {
                        window.AppendNormal("Off");
                    }
                    window.AppendLineBreak();

                    window.AppendHighlight("Color: ");
                    window.AppendNormal(this.GuildColor);
                    window.AppendLineBreak();

                    window.AppendHighlight("Relay Mode: ");
                    window.AppendNormal(this.RelayMode);
                    window.AppendLineBreak();

                    window.AppendHighlight("AO->IRC Connect Syntax: ");
                    window.AppendNormal(this.ConnectSyntaxAo);
                    window.AppendLineBreak();

                    window.AppendHighlight("IRC->AO Connect Syntax: ");
                    window.AppendNormal(this.ConnectSyntaxIrc);
                    window.AppendLineBreak();

                    window.AppendHighlight("Reconnect Delay: ");
                    window.AppendNormal(this.ConnectDelay.ToString());
                    window.AppendLineBreak();

                    window.AppendHighlight("Message Cap: ");
                    window.AppendNormal(this.MessageCap.ToString());

                    window.AppendLineBreak(2);

                    window.AppendHighlight("Control: ");
                    window.AppendCommand("Start", "/tell " + this.Bot.Character + " irc start");
                    window.AppendNormal(" / ");
                    window.AppendCommand("Stop", "/tell " + this.Bot.Character + " irc stop");

                    this.Bot.SendReply(e, "IRC »» " + window.ToString());
                    break;
                case "irc start":
                    this.StartLink();
                    this.Bot.SendReply(e, "Starting SimpleIRC");
                    break;
                case "irc stop":
                    this.Bot.SendReply(e, "Stopping SimpleIRC");
                    this.StopLink();
                    break;
                case "irc restart":
                    this.Bot.SendReply(e, "Stopping SimpleIRC");
                    this.StopLink();
                    this.StartLink();
                    this.Bot.SendReply(e, "Starting SimpleIRC");
                    break;
                case "irc raw":
                    if (e.Args.Length > 0)
                    {
                        string raw = string.Join(" ", e.Args);
                        this.IRC.Sender.Raw(raw);
                        this.Bot.SendReply(e, "Executed Raw Command");
                    }
                    else
                    {
                        this.Bot.SendReply(e, "Invalid Command");
                    }
                    break;
                case "say":
                    if (e.Args.Length > 1)
                    {
                        string message = string.Join(" ", e.Args, 1, e.Args.Length - 1);
                        switch (e.Args[0].ToLower())
                        {
                            case "both":
                            case "all":
                                this.IRC.Sender.PublicMessage(this.Channel, message);
                                if (this.RelayMode == "guild" || this.RelayMode == "both")
                                {
                                    this.Bot.SendOrganizationMessage(message);
                                }
                                if (this.RelayMode == "guest" || this.RelayMode == "both")
                                {
                                    this.Bot.SendPrivateChannelMessage(message);
                                }
                                return;
                            case "guild":
                                this.Bot.SendOrganizationMessage(message);
                                return;
                            case "private":
                            case "guest":
                                this.Bot.SendPrivateChannelMessage(message);
                                return;
                            case "irc":
                                this.IRC.Sender.PublicMessage(this.Channel, message);
                                return;
                            default:
                                break;
                        }
                    }
                    this.Bot.SendReply(e, "Invalid target! Valid targets are: all, guild, guest, irc");
                    break;
            }
        }

        private void LoadSettings()
        {
            this.Server = this.Bot.Configuration.GetString(this.InternalName, "server", this.Server);
            this.ServerPassword = this.Bot.Configuration.GetString(this.InternalName, "server_password", this.Server);
            this.Port = this.Bot.Configuration.GetInteger(this.InternalName, "port", this.Port);
            this.Nickname = this.Bot.Configuration.GetString(this.InternalName, "nickname", this.Bot.Character);
            this.Channel = this.Bot.Configuration.GetString(this.InternalName, "channel", string.Empty);
            this.Password = this.Bot.Configuration.GetString(this.InternalName, "password", string.Empty);
            this.LogChannel = this.Bot.Configuration.GetString(this.InternalName, "logchannel", string.Empty);
            this.LogPassword = this.Bot.Configuration.GetString(this.InternalName, "logpassword", string.Empty);
            this.MessageCap = this.Bot.Configuration.GetInteger(this.InternalName, "cap", this.MessageCap);
            this.GuildColor = this.Bot.Configuration.GetColor(this.InternalName, "color", this.GuildColor);
            this.RelayMode = this.Bot.Configuration.GetString(this.InternalName, "mode", this.RelayMode);
            this.ConnectSyntaxAo = this.Bot.Configuration.GetString(this.InternalName, "aoconnect", this.ConnectSyntaxAo).ToLower();
            this.ConnectSyntaxIrc = this.Bot.Configuration.GetString(this.InternalName, "ircconnect", this.ConnectSyntaxIrc).ToLower();
            this.ConnectDelay = this.Bot.Configuration.GetInteger(this.InternalName, "delay", this.ConnectDelay);
            this.ReconnectTimer.Interval = this.ConnectDelay * 1000;

            if (this.Channel == string.Empty || this.Channel == null)
            {
                this.Channel = this.Bot.Character;
            }

            if (!this.Channel.StartsWith("#"))
                this.Channel = "#" + this.Channel;
            if (this.LogChannel != string.Empty && this.LogChannel != null)
            {
                if (!this.LogChannel.StartsWith("#"))
                    this.LogChannel = "#" + this.LogChannel;
            }
        }

        private void StartLink()
        {
            this.LoadSettings();
            try
            {
                if (this.IRC != null)
                {
                    this.IRC.Disconnect("Restarting...");
                    this._IRC = null;
                }
            }
            catch { }

            ConnectionArgs cargs = new ConnectionArgs(this.Nickname, this.Server);
            cargs.Port = this.Port;
            cargs.RealName = "VhaBot/" + BotShell.VERSION;
            
            if (this.ServerPassword != null && this.ServerPassword != string.Empty)
            {
                cargs.ServerPassword = this.ServerPassword;
            }
            this._IRC = new Connection(cargs, false, false);
            this.IRC.Listener.OnRegistered += new RegisteredEventHandler(Listener_OnRegistered);
            this.IRC.Listener.OnPublic += new PublicMessageEventHandler(Listener_OnPublic);
            this.IRC.Listener.OnAdmin += new AdminEventHandler(Listener_OnAdmin);
            this.IRC.Listener.OnError += new ErrorMessageEventHandler(Listener_OnError);
            this.IRC.Listener.OnJoin += new JoinEventHandler(Listener_OnJoin);
            this.IRC.Listener.OnPart += new PartEventHandler(Listener_OnPart);
            this.IRC.Listener.OnKick += new KickEventHandler(Listener_OnKick);
            this.IRC.Listener.OnQuit += new QuitEventHandler(Listener_OnQuit);
            this.IRC.Listener.OnAction += new ActionEventHandler(Listener_OnAction);
            this.IRC.Listener.OnDisconnected += new DisconnectedEventHandler(Listener_OnDisconnected);
            this.IRC.Listener.OnTopicChanged += new TopicEventHandler(Listener_OnTopicChanged);
            this.IRC.Listener.OnNick += new NickEventHandler(Listener_OnNick);
            this.IRC.Listener.OnPrivate += new PrivateMessageEventHandler(Listener_OnPrivate);
            this.IRC.HandleNickTaken = true;
            this.IRC.EnableCtcp = false;
            this.IRC.EnableDcc = false;

            try
            {
                this.Closing = false;
                this.Connected = false;
                this.ReconnectTimer.Enabled = false;
                this.QueueTimer.Enabled = true;
                this.Output("Connecting...");
                this.IRC.Connect();
            }
            catch
            {
                this.ReconnectTimer.Enabled = true;
                this.Output("Unable to connect");
                this.Output("Retrying in " + this.ConnectDelay + " seconds");
            }
        }

        void Listener_OnAdmin(string message)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private void Events_Ping(object sender, EventArgs e)
        {
            if (this.IRC != null)
            {
                if (this.IRC.Connected)
                    try
                    {
                        this.IRC.Sender.Raw("PING " + TimeStamp.Now);
                    }
                    catch { }
            }
        }

        private void Listener_OnRegistered()
        {
            this.Connected = true;
            this.IRCQueue.Clear();
            if (this.Password == null || this.Password == String.Empty)
                this.IRC.Sender.Join(this.Channel);
            else
                this.IRC.Sender.Join(this.Channel, this.Password);
            this.Output("Joined " + this.Channel);


            if (this.LogChannel != null && this.LogChannel != String.Empty)
            {
                this.Output("There is a log channel set, joining " + this.LogChannel + ", password: "+ this.LogPassword);
                if (this.LogPassword == "" || this.LogPassword == null || this.LogPassword == String.Empty)
                    this.IRC.Sender.Join(this.LogChannel);
                else
                    this.IRC.Sender.Join(this.LogChannel, this.LogPassword);
                this.Output("Joined " + this.LogChannel);
            }
            this.Output("Connected");
        }

        private void Listener_OnError(ReplyCode code, string message)
        {
            if (message.Contains("PONG"))
                return; // Silly lib thinks PONG is an error!

            try
            {
                this.Output("ERROR: " + message);
                if (this.Connected)
                {
                    this.Connected = false;
                    this.Output("Disconnected");
                }
                if (this.Closing == false && this.ReconnectTimer.Enabled == false)
                {
                    this.Output("Reconnecting in " + this.ConnectDelay + " seconds");
                    this.ReconnectTimer.Enabled = true;
                }
                try
                {
                    this.IRC.Disconnect("Error");
                    this._IRC = null;
                }
                catch { }
            }
            catch { }
        }

        private void Listener_OnDisconnected()
        {
            if (this.Connected)
            {
                this.Connected = false;
                this.Output("Disconnected");
            }
        }

        private void Listener_OnJoin(UserInfo user, string channel)
        {
            if (channel.ToLower() == this.Channel.ToLower() && user.Nick.ToLower() != this.IRC.ConnectionData.Nick.ToLower())
            {
                string formattedMessage = string.Format("{0} has joined {1}", user.Nick, channel);
                this.SendMessage("[IRC] " + formattedMessage, "both");
                this.Output(formattedMessage);
            }
        }

        private void Listener_OnPart(UserInfo user, string channel, string reason)
        {
            if (channel.ToLower() == this.Channel.ToLower() && user.Nick.ToLower() != this.IRC.ConnectionData.Nick.ToLower())
            {
                string formattedMessage = string.Format("{0} has left {1}", user.Nick, channel);
                if (reason != null && reason != string.Empty)
                    formattedMessage += " (" + this.StripControlChars(reason) + ")";
                this.SendMessage("[IRC] " + HTML.EscapeString(formattedMessage), "both");
                this.Output(formattedMessage);
            }
        }

        private void Listener_OnKick(UserInfo user, string channel, string kickee, string reason)
        {
            if (channel.ToLower() == this.Channel.ToLower() && user.Nick.ToLower() != this.IRC.ConnectionData.Nick.ToLower())
            {
                string formattedMessage = string.Format("{0} has kicked {1} from {2}", user.Nick, kickee, channel);
                if (reason != null && reason != string.Empty)
                    formattedMessage += " (" + this.StripControlChars(reason) + ")";
                this.SendMessage("[IRC] " + HTML.EscapeString(formattedMessage), "both");
                this.Output(formattedMessage);
            }
            if (kickee.ToLower() == this.IRC.ConnectionData.Nick.ToLower())
            {
                if (this.Password == null || this.Password == String.Empty)
                    this.IRC.Sender.Join(this.Channel);
                else
                    this.IRC.Sender.Join(this.Channel, this.Password);
            }
        }

        private void Listener_OnQuit(UserInfo user, string reason)
        {
            string formattedMessage = string.Format("{0} has quit IRC", user.Nick);
            if (reason != null && reason != string.Empty)
                formattedMessage += " (" + this.StripControlChars(reason) + ")";
            this.SendMessage("[IRC] " + HTML.EscapeString(formattedMessage), "both");
            this.Output(formattedMessage);
        }

        private void Listener_OnNick(UserInfo user, string newNick)
        {
            if (user.Nick.ToLower() != this.IRC.ConnectionData.Nick.ToLower())
            {
                string formattedMessage = string.Format("{0} is now known as {1}", user.Nick, newNick);
                this.SendMessage("[IRC] " + HTML.EscapeString(formattedMessage), "both");
                this.Output(formattedMessage);
            }
        }

        private bool Listener_OnCommand(UserInfo user, string message)
        {
            // !online
            if (message.ToLower() == "!online")
            {
                string reply = string.Empty;
                string[] online = this.Bot.FriendList.Online("notify");
                if (online.Length == 0)
                    reply += "No users online.";
                else
                    reply += "Online: " + string.Join(", ", online) + ".";

                if (this.RelayMode == "guest" || this.RelayMode == "both")
                {
                    Dictionary<UInt32, Friend> list = this.Bot.PrivateChannel.List();
                    List<string> guests = new List<string>();
                    foreach (KeyValuePair<UInt32, Friend> guest in list)
                        guests.Add(Format.UppercaseFirst(guest.Value.User));
                    if (guests.Count > 0)
                        reply += " Guests: " + string.Join(", ", guests.ToArray());
                }
                IRCQueue.Enqueue(Plugins.IRCQueue.Priority.High, new Plugins.IRCQueue.IRCQueueItem(user.Nick, reply));
                return true;
            }

            // !is and !whois
            if (message.Trim().Contains(" "))
            {
                string[] parts = message.Trim().Split(' ');
                string command = parts[0];
                string username = parts[1];
                switch (command)
                {
                    case "!is":
                        UInt32 userid = this.Bot.GetUserID(username);
                        OnlineState state = this.Bot.FriendList.IsOnline(userid);
                        if (state == OnlineState.Timeout)
                        {
                            IRCQueue.Enqueue(Plugins.IRCQueue.Priority.High, new Plugins.IRCQueue.IRCQueueItem(user.Nick, "Request timed out. Please try again later"));
                            return true;
                        }
                        if (state == OnlineState.Unknown)
                        {
                            IRCQueue.Enqueue(Plugins.IRCQueue.Priority.High, new Plugins.IRCQueue.IRCQueueItem(user.Nick, "No such user: " + username));
                            return true;
                        }
                        string append = "Online";
                        if (state == OnlineState.Offline)
                        {
                            append = "Offline";
                            Int64 seen = this.Bot.FriendList.Seen(username);
                            if (seen > 1)
                                append += " and was last seen online at " + Format.DateTime(seen, FormatStyle.Large) + " GMT";
                        }
                        IRCQueue.Enqueue(Plugins.IRCQueue.Priority.High, new Plugins.IRCQueue.IRCQueueItem(user.Nick, String.Format("{0} is currently {1}", Format.UppercaseFirst(username), append)));
                        break;
                    case "!whois":
                        if (this.Bot.GetUserID(username) < 100)
                        {
                            IRCQueue.Enqueue(Plugins.IRCQueue.Priority.High, new Plugins.IRCQueue.IRCQueueItem(user.Nick, "No such user: " + username));
                            return true;
                        }
                        WhoisResult whois = XML.GetWhois(username, this.Bot.Dimension);
                        if (whois == null || !whois.Success)
                        {
                            IRCQueue.Enqueue(Plugins.IRCQueue.Priority.High, new Plugins.IRCQueue.IRCQueueItem(user.Nick, "Unable to gather information on that user"));
                            return true;
                        }
                        IRCQueue.Enqueue(Plugins.IRCQueue.Priority.High, new Plugins.IRCQueue.IRCQueueItem(user.Nick, Format.Whois(whois, FormatStyle.Large)));
                        break;
                }
            }
            return false;
        }

        private void Listener_OnPublic(UserInfo user, string channel, string message)
        {
            if (channel.ToLower() == this.Channel.ToLower() && user.Nick.ToLower() != this.IRC.ConnectionData.Nick.ToLower())
            {
                this.Output("[" + channel + "] " + user.Nick + ": " + this.StripControlChars(message));

                // stuff here
                if (message.StartsWith("!"))
                    if (this.Listener_OnCommand(user, message))
                        return;


                // Normal relay stuff
                if (this.ConnectSyntaxIrc != "off")
                    if (message.StartsWith(this.ConnectSyntaxIrc, StringComparison.CurrentCultureIgnoreCase))
                        message = message.Substring(this.ConnectSyntaxIrc.Length);
                    else
                        return;

                string nick = string.Empty;
                if (Regex.Match(message, "^" + this.BOLD + this.BOLD + this.BOLD + @"\[(.+)\]" + this.BOLD + @" (.+)").Success)
                    this.SendMessage(HTML.EscapeString(message), "both");
                else
                    this.SendMessage(string.Format("[IRC] {0}: {1}", user.Nick, HTML.EscapeString(message)), "both");
            }
        }

        private void Listener_OnPrivate(UserInfo user, string message)
        {
            if (message.StartsWith("!"))
                this.Listener_OnCommand(user, message);
        }

        private void Listener_OnAction(UserInfo user, string channel, string description)
        {
            if (channel.ToLower() == this.Channel.ToLower() && user.Nick.ToLower() != this.IRC.ConnectionData.Nick.ToLower())
            {
                this.Output("[" + channel + "] * " + user.Nick + " " + this.StripControlChars(description));

                if (this.ConnectSyntaxIrc != "off")
                    if (description.StartsWith(this.ConnectSyntaxIrc, StringComparison.CurrentCultureIgnoreCase))
                        description = description.Substring(this.ConnectSyntaxIrc.Length);
                    else
                        return;

                string formattedMessage = string.Format("[IRC] * {0} {1}", user.Nick, HTML.EscapeString(description));
                this.SendMessage(formattedMessage, "both");
            }
        }

        private void Listener_OnTopicChanged(UserInfo user, string channel, string newTopic)
        {
            if (channel.ToLower() == this.Channel.ToLower())
            {
                string formattedMessage = string.Format("{0} changed the topic to: {1}", user.Nick, HTML.EscapeString(newTopic));
                this.SendMessage("[IRC] " + formattedMessage, "both");
                this.Output("[" + channel + "] " + this.StripControlChars(formattedMessage));
            }
        }
        private void Reconnect(object sender, ElapsedEventArgs e)
        {
            this.ReconnectTimer.Enabled = false;
            this.StartLink();
        }

        private void SendMessage(string message, string mode)
        {
            if (mode == "irc")
            {
                if (this.IRC == null)
                    return;
                message = message.Replace("\n", " ");
                message = HTML.UnescapeString(HTML.StripTags(message));
                if (message.Length > this.MessageCap)
                    message = message.Substring(0, this.MessageCap) + "...";
                
                if (this.IRC.Connected)
                    if (this.RelayMode != "none")
                    {
                        IRCQueue.Enqueue(Plugins.IRCQueue.Priority.Normal, new Plugins.IRCQueue.IRCQueueItem(this.Channel, message));                        
                    }
                return;
            }

            MatchCollection matches = Regex.Matches(message, this.ItemPattern);
            foreach (Match match in matches)
            {
                try
                {
                    string raw = match.Groups[0].Value;
                    string name = match.Groups[1].Value;
                    Int32 lowid = Convert.ToInt32(match.Groups[3].Value);
                    Int32 highid = Convert.ToInt32(match.Groups[4].Value);
                    Int32 ql = Convert.ToInt32(match.Groups[5].Value);
                    message = message.Replace(raw, HTML.CreateItem(name, lowid, highid, ql));
                }
                catch { }
            }
            message = this.StripControlChars(message);
            message = HTML.CreateColorStart(this.GuildColor) + message + HTML.CreateColorEnd();

            if ((this.RelayMode == "guild" || this.RelayMode == "both") && (mode == "guild" || mode == "both"))
                this.Bot.SendOrganizationMessage(message);
            if ((this.RelayMode == "guest" || this.RelayMode == "both") && (mode == "guest" || mode == "both"))
                this.Bot.SendPrivateChannelMessage(message);
        }

        public string StripControlChars(string message)
        {
            for (int i = 15; i >= 0; i--)
            {
                message = message.Replace(Convert.ToChar(3).ToString() + i, "");
            }
            message = message.Replace(this.BOLD, "");
            message = message.Replace(this.COLOR, "");
            message = message.Replace(this.REVERSE, "");
            message = message.Replace(this.UNDERLINE, "");
            return message;
        }

        private void StopLink()
        {
            if (this.IRC != null)
            {
                this.Closing = true;
                this.ReconnectTimer.Enabled = false;
                this.QueueTimer.Enabled = false;
                this.Output("Disconnecting...");
                try
                {
                    this.IRC.Disconnect("So long and thanks for all the fish");
                }
                catch { }
                this._IRC = null;
            }
        }

        private void Output(string message)
        {
            Console.WriteLine(String.Format("[{0}@{1}] {2}", this.Nickname, this.Server, message));
        }

        public override void OnPluginMessage(BotShell bot, PluginMessage message)
        {
            switch (message.Command.ToLower())
            {
                case "irclog":
                    if (!this.IRC.Connected) return;
                    if (this.LogChannel != string.Empty && this.LogChannel != null)
                    {
                        string msg = (string)message.Args[0];
                        msg = msg.Replace("\n", " ");
                        msg = HTML.UnescapeString(HTML.StripTags(msg));
                        if (msg.Length > this.MessageCap)
                            msg = msg.Substring(0, this.MessageCap) + "...";
                        IRCQueue.Enqueue(Plugins.IRCQueue.Priority.Low, new Plugins.IRCQueue.IRCQueueItem(this.LogChannel, msg));
                    }
                    break;
            }
        }
    }
}
