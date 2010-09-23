using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Timers;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class VhPrivateChannel : PluginBase
    {
        private bool _locked = false;
        private string _lockedBy = string.Empty;
        private bool _announce = true;
        private Config _database;
        private Timer _timer;
        private BotShell _bot;
        private bool _sendgc = true;
        private bool _sendpg = true;

        public VhPrivateChannel()
        {
            this.Name = "Private Channel Manager";
            this.InternalName = "vhPrivateChannelManager";
            this.Author = "Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("join", true, UserLevel.Member),
                new Command("leave", true, UserLevel.Guest),
                new Command("invite", true, UserLevel.Leader, UserLevel.Leader, UserLevel.Member),
                new Command("kick", true, UserLevel.Leader, UserLevel.Leader, UserLevel.Member),
                new Command("lock", true, UserLevel.Leader),
                new Command("unlock", true, UserLevel.Leader),

                new Command("guestlist", true, UserLevel.Leader),
                new Command("guestlist add", true, UserLevel.Leader),
                new Command("guestlist remove", true, UserLevel.Leader),
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "announce", "Announce joins and leaves", this._announce);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendgc", "Send notifications to the organization channel", this._sendgc);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendpg", "Send notifications to the private channel", this._sendpg);

            bot.Events.UserJoinChannelEvent += new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.UserLeaveChannelEvent += new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
            bot.Events.UserLogonEvent += new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            bot.Events.BotStateChangedEvent += new BotStateChangedHandler(Events_BotStateChangedEvent);

            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS pg (user INTEGER)");
            this._timer = new Timer(5000);
            this._timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            this._timer.AutoReset = false;
            this._bot = bot;
            this.LoadConfiguration(bot);
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section != this.InternalName) return;
            this.LoadConfiguration(bot);
        }

        private void LoadConfiguration(BotShell bot)
        {
            this._sendgc = bot.Configuration.GetBoolean(this.InternalName, "sendgc", this._sendgc);
            this._sendpg = bot.Configuration.GetBoolean(this.InternalName, "sendpg", this._sendpg);
            this._announce = bot.Configuration.GetBoolean(this.InternalName, "announce", this._announce);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.UserJoinChannelEvent -= new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.UserLeaveChannelEvent -= new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
            bot.Events.UserLogonEvent -= new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            bot.Events.BotStateChangedEvent -= new BotStateChangedHandler(Events_BotStateChangedEvent);
        }

        private void Events_UserLeaveChannelEvent(BotShell bot, UserLeaveChannelArgs e)
        {
            if (this._announce)
            {
                string message = bot.ColorHeader + e.Sender + bot.ColorHighlight + " has left the private channel";
                this.SendMessage(bot, message);
                this._database.ExecuteNonQuery("DELETE FROM pg WHERE user = " + e.SenderID);
            }
        }

        private void Events_UserJoinChannelEvent(BotShell bot, UserJoinChannelArgs e)
        {
            if (this._announce)
            {
                string message;
                if (e.SenderWhois != null)
                    message = bot.ColorHeader + Format.Whois(e.SenderWhois, FormatStyle.Medium) + bot.ColorHighlight + " has joined the private channel";
                else
                    message = bot.ColorHeader + e.Sender + bot.ColorHighlight + " has joined the private channel";
                this.SendMessage(bot, message);
                this._database.ExecuteNonQuery("INSERT INTO pg VALUES (" + e.SenderID + ")");
            }
        }

        private void Events_UserLogonEvent(BotShell bot, UserLogonArgs e)
        {
            if (e.First) return;
            if (e.Sections.Contains("guestlist"))
            {
                bot.SendPrivateMessage(e.SenderID, bot.ColorHighlight + "You have been invited to the private channel because you're on this bot's guestlist. To remove yourself from the guestlist use: " + HTML.CreateColorString(bot.ColorHeaderHex, "/tell " + bot.Character + " guestlist remove"));
                bot.PrivateChannel.Invite(e.SenderID);
            }
        }

        private void Events_BotStateChangedEvent(BotShell bot, BotStateChangedArgs e)
        {
            if (e.IsSlave) return;
            if (e.State != BotState.Connected) return;
            this._timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                List<UInt32> users = new List<UInt32>();
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT user FROM pg";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read()) users.Add((UInt32)reader.GetInt32(0));
                    reader.Close();
                }
                foreach (UInt32 user in users)
                {
                    this._database.ExecuteNonQuery("DELETE FROM pg WHERE user = " + user);
                    this._bot.SendPrivateMessage(user, this._bot.ColorHighlight + "You have been reinvited to the private channel");
                    this._bot.PrivateChannel.Invite(user);
                }
            }
            catch { }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.FromSlave && e.Type == CommandType.PrivateChannel)
                return;

            switch (e.Command)
            {
                case "lock":
                    if (this._locked)
                    {
                        bot.SendReply(e, "The private channel already is locked");
                        return;
                    }
                    this._lockedBy = e.Sender;
                    this._locked = true;
                    string lockMessage = bot.ColorHeader + e.Sender + bot.ColorHighlight + " has locked the private channel";
                    this.SendMessage(bot, lockMessage);
                    break;
                case "unlock":
                    if (!this._locked)
                    {
                        bot.SendReply(e, "The private channel already is unlocked");
                        return;
                    }
                    this._locked = false;
                    string unlockMessage = bot.ColorHeader + e.Sender + bot.ColorHighlight + " has unlocked the private channel";
                    this.SendMessage(bot, unlockMessage);
                    break;
                case "join":
                    if (bot.PrivateChannel.IsOn(e.SenderID))
                    {
                        bot.SendReply(e, "You already are on the private channel");
                        return;
                    }
                    if (this._locked && bot.Users.GetUser(e.Sender) < UserLevel.Leader)
                    {
                        bot.SendReply(e, "The private channel is currently locked by " + bot.ColorHeader + this._lockedBy);
                        return;
                    }
                    bot.SendReply(e, "Inviting you to the private channel");
                    bot.PrivateChannel.Invite(e.SenderID);
                    break;
                case "leave":
                    if (!bot.PrivateChannel.IsOn(e.SenderID))
                    {
                        bot.SendReply(e, "You're not on the private channel");
                        return;
                    }
                    if (e.Type != CommandType.PrivateChannel)
                        bot.SendReply(e, "Kicking you from the private channel");

                    // Remove a user from the vhabot raid system before letting him leave
                    if (bot.Plugins.IsLoaded("raidcore"))
                        bot.SendPluginMessageAndWait(this.InternalName, "raidcore", "RemoveRaider", 1000, e.Sender);

                    bot.PrivateChannel.Kick(e.SenderID);
                    break;
                case "invite":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: invite [username]");
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    if (bot.PrivateChannel.IsOn(e.Args[0]))
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " already is on the private channel");
                        break;
                    }
                    if (this._locked && bot.Users.GetUser(e.Sender) < UserLevel.Leader)
                    {
                        bot.SendReply(e, "The private channel is currently locked by " + bot.ColorHeader + this._lockedBy);
                        return;
                    }
                    bot.SendReply(e, "Inviting " + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " to the private channel");
                    bot.PrivateChannel.Invite(e.Args[0]);
                    break;
                case "kick":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: kick [username]");
                        break;
                    }
                    if (e.Args[0].ToLower() == "all")
                    {
                        bot.SendReply(e, "Clearing the private channel");
                        bot.PrivateChannel.KickAll();
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    if (!bot.PrivateChannel.IsOn(e.Args[0]))
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " isn't on the private channel");
                        break;
                    }
                    if (bot.Users.GetMain(e.Args[0]).Equals(bot.Admin, StringComparison.CurrentCultureIgnoreCase))
                    {
                        bot.SendReply(e, "You can't kick the bot owner");
                        break;
                    }
                    if (bot.Users.GetUser(e.Args[0]) > bot.Users.GetUser(e.Sender))
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " outranks you");
                        break;
                    }
                    // Remove a user from the vhabot raid system before kicking them
                    if (bot.Plugins.IsLoaded("RaidCore"))
                        bot.SendPluginMessageAndWait(this.InternalName, "raidcore", "RemoveRaider", 1000, e.Args[0]);

                    bot.SendReply(e, "Kicking " + HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " from the private channel");
                    bot.PrivateChannel.Kick(e.Args[0]);
                    break;
                case "guestlist":
                    string[] guestlist = bot.FriendList.List("guestlist");
                    if (guestlist.Length == 0)
                    {
                        bot.SendReply(e, "There are no users on the guestlist");
                        break;
                    }
                    RichTextWindow window = new RichTextWindow(bot);
                    window.AppendTitle("Guestlist");
                    foreach (string guest in guestlist)
                    {
                        window.AppendHighlight(Format.UppercaseFirst(guest));
                        window.AppendNormalStart();
                        window.AppendString(" [");
                        window.AppendBotCommand("Remove", "guestlist remove " + guest);
                        window.AppendString("]");
                        window.AppendColorEnd();
                        window.AppendLineBreak();
                    }
                    bot.SendReply(e, guestlist.Length + " Guests »» ", window);
                    break;
                case "guestlist add":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: guestlist add [username]");
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    if (bot.FriendList.IsFriend("guestlist", e.Args[0]))
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " already is on the guestlist");
                        break;
                    }
                    bot.FriendList.Add("guestlist", e.Args[0]);
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " has been added to the guestlist");
                    break;
                case "guestlist remove":
                    if (e.Args.Length < 1)
                    {
                        if (bot.FriendList.IsFriend("guestlist", e.Sender))
                        {
                            bot.SendReply(e, "You have been removed from the guestlist");
                            bot.FriendList.Remove("guestlist", e.Sender);
                        }
                        else
                        {
                            bot.SendReply(e, "You're not on the guestlist");
                        }
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    if (!bot.FriendList.IsFriend("guestlist", e.Args[0]))
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " is not on the guestlist");
                        break;
                    }
                    bot.FriendList.Remove("guestlist", e.Args[0]);
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, Format.UppercaseFirst(e.Args[0])) + " has been removed from the guestlist");
                    break;
            }
        }

        public override void OnUnauthorizedCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "join":
                    if (bot.FriendList.IsFriend("guestlist", e.Sender))
                    {
                        e.Authorized = true;
                        this.OnCommand(bot, e);
                    }
                    break;
                case "guestlist remove":
                    if (e.Args.Length == 0)
                    {
                        e.Authorized = true;
                        this.OnCommand(bot, e);
                    }
                    break;
            }
        }

        private void SendMessage(BotShell bot, string message)
        {
            if (this._sendgc) bot.SendOrganizationMessage(bot.ColorHighlight + message);
            if (this._sendpg) bot.SendPrivateChannelMessage(bot.ColorHighlight + message);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "join":
                    return "Invites you to the bot's private channel.\n" +
                        "Usage: /tell " + bot.Character + " join";
                case "leave":
                    return "Kicks you from the bot's private channel.\n" +
                        "Usage: /tell " + bot.Character + " leave";
                case "invite":
                    return "Invites [username] to the bot's private channel.\n" +
                        "Usage: /tell " + bot.Character + " invite [username]";
                case "kick":
                    return "Kicks [username] from the bot's private channel.\n" +
                        "Usage: /tell " + bot.Character + " kick [username]";
                case "lock":
                    return "Locks the private channel from everyone ranked lower than Leader.\n" +
                        "Usage: /tell " + bot.Character + " lock";
                case "unlock":
                    return "Unlocks the private channel.\n" +
                        "Usage: /tell " + bot.Character + " unlock";
                case "guestlist":
                    return "Displays the current guestlist.\nUsers on the guestlist will be automatically invited to the private channel when they log on.\n" +
                        "Usage: /tell " + bot.Character + " guestlist";
                case "guestlist add":
                    return "Adds [username] to the guestlist.\nUsers on the guestlist will be automatically invited to the private channel when they log on.\n" +
                        "Usage: /tell " + bot.Character + " guestlist add [username]";
                case "guestlist remove":
                    return "Removes [username] from the guestlist.\nIf [username] is not supplied, it will remove you from the guestlist instead.\n" +
                        "Usage: /tell " + bot.Character + " guestlist remove [[username]]";
            }
            return null;
        }
    }
}
