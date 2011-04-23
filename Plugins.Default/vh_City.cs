using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Data;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{
    public class VhCity : PluginBase
    {
        public enum VhCityState
        {
            Unknown,
            Enabled,
            Disabled
        }
        public enum VhCityRemindState
        {
             Unknown,
             Waiting,
             Halfway,
             FiveMinutes,
             PreAnnounce,
             Announced,
             PostAnnounce
        }
        private VhCityState _state = VhCityState.Unknown;
        private VhCityRemindState _reminders = VhCityRemindState.Unknown;
        public VhCityState State { get { return this._state; } }
        public VhCityRemindState Reminded { get { return this._reminders; } }
        private DateTime _time = DateTime.Now;
        private BotShell _bot;
        // private bool _sentAnnounce = false;
        // private bool _sentPreAnnounce = false;
        // private bool _sentPostAnnounce = false;
        public TimeSpan TimeLeft
        {
            get
            {
                TimeSpan span = DateTime.Now - this._time;
                if (span.TotalHours >= 1) return new TimeSpan(0);
                return new TimeSpan(1, 0, 0) - span;
            }
        }
        public TimeSpan TimeExpired
        {
            get
            {
                TimeSpan span = DateTime.Now - this._time;
                return span;
            }
        }
        private string _username = string.Empty;
        private bool _sendgc = true;
        private bool _sendpg = true;
        //private bool _sendirc = true;

        public VhCity()
        {
            this.Name = "City Cloak Tracker";
            this.InternalName = "VhCity";
            this.Author = "Iriche / Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("cloak", false, UserLevel.Guest, UserLevel.Member, UserLevel.Guest),
                new Command("cloak reset", false, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Events.ChannelMessageEvent += new ChannelMessageHandler(OnChannelMessageEvent);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(ConfigurationChangedEvent);
            bot.Timers.Minute += new EventHandler(OnMinute);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendgc", "Send notifications to the organization channel", this._sendgc);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendpg", "Send notifications to the private channel", this._sendpg);
            //bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendirc", "Send notifications to IRC", this._sendirc);
            this.LoadConfiguration(bot);
            this._bot = bot;
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ChannelMessageEvent -= new ChannelMessageHandler(OnChannelMessageEvent);
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(ConfigurationChangedEvent);
            bot.Timers.Minute -= new EventHandler(OnMinute);
        }

        private void ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section != this.InternalName) return;
            this.LoadConfiguration(bot);
        }

        private void LoadConfiguration(BotShell bot)
        {
            this._sendgc = bot.Configuration.GetBoolean(this.InternalName, "sendgc", this._sendgc);
            this._sendpg = bot.Configuration.GetBoolean(this.InternalName, "sendpg", this._sendpg);
            //this._sendirc = bot.Configuration.GetBoolean(this.InternalName, "sendirc", this._sendirc);
        }

        private void OnChannelMessageEvent(BotShell bot, ChannelMessageArgs e)
        {
            if (e.Type != ChannelType.Organization) return;
            if (!e.IsDescrambled) return;
            if (e.Descrambled.CategoryID != 1001) return;
            switch (e.Descrambled.EntryID)
            {
                case 1: // %s turned the cloaking device in your city %s.
                    this._time = DateTime.Now;
                    this._username = e.Descrambled.Arguments[0].Text;
                    //this._sentAnnounce = false;
                    //this._sentPreAnnounce = false;
                    //this._sentPostAnnounce = false;
                    this._reminders = VhCityRemindState.Waiting;
                    if (e.Descrambled.Arguments[1].Text.Equals("on", StringComparison.CurrentCultureIgnoreCase))
                    {
                        this._state = VhCityState.Enabled;
                        this.SendMessage(bot, HTML.CreateColorString(bot.ColorHeaderHex, this._username) + " has " + HTML.CreateColorString(RichTextWindow.ColorGreen, "enabled") + " the city cloaking device");
                    }
                    else if (e.Descrambled.Arguments[1].Text.Equals("off", StringComparison.CurrentCultureIgnoreCase))
                    {
                        this._state = VhCityState.Disabled;
                        this.SendMessage(bot, HTML.CreateColorString(bot.ColorHeaderHex, this._username) + " has " + HTML.CreateColorString(RichTextWindow.ColorRed, "disabled") + " the city cloaking device");
                    }
                    else
                    {
                        this._state = VhCityState.Unknown;
                    }
                    break;
            }
        }

        private void OnMinute(object sender, EventArgs e)
        {
            //if (this._state == VhCityState.Unknown) return;
            if (this._reminders == VhCityRemindState.Unknown) return;
            TimeSpan span = this.TimeExpired;
            int minutes = 30;
            if (this._reminders == VhCityRemindState.FiveMinutes)
               minutes += 25;
            else if (this._reminders == VhCityRemindState.Halfway)
               minutes += 15;
            else if (this._reminders == VhCityRemindState.PostAnnounce)
               minutes += 60;
            else if (this._reminders == VhCityRemindState.Announced)
               minutes += 45;
            switch (this._reminders)
            {
                 case VhCityRemindState.FiveMinutes:
                 case VhCityRemindState.Halfway:
                 case VhCityRemindState.Waiting:
                    if (span.TotalMinutes >= minutes)
                    {
                        string state;
                        if (this._state == VhCityState.Enabled)
                            state = HTML.CreateColorString(RichTextWindow.ColorGreen, "enabled");
                        else
                            state = HTML.CreateColorString(RichTextWindow.ColorRed, "disabled");
                        this.SendMessage(this._bot, HTML.CreateColorString(this._bot.ColorHeaderHex, minutes.ToString()) + " minutes have passed since " + HTML.CreateColorString(this._bot.ColorHeaderHex, this._username) + " has " + state + " the city cloaking device");
                        if (this._reminders == VhCityRemindState.Waiting)
                          this._reminders = VhCityRemindState.Halfway;
                        else if (this._reminders == VhCityRemindState.Halfway)
                          this._reminders = VhCityRemindState.FiveMinutes;
                        else
                          this._reminders = VhCityRemindState.PreAnnounce;
                    }
                    break;

                 case VhCityRemindState.PreAnnounce:
                    if (span.TotalMinutes >= 60)
                    {
                        if (this._state == VhCityState.Disabled)
                            this.SendMessage(this._bot, "The city cloaking device has fully recovered from the alien attack. Please enable the city cloaking device");
                        else
                            this.SendMessage(this._bot, "The city cloaking device has finished charging. New alien attacks can now be initiated");
                          this._reminders = VhCityRemindState.Announced;
                    }
                    break;

                 case VhCityRemindState.PostAnnounce:
                 case VhCityRemindState.Announced:
                    if (span.TotalMinutes >= minutes && this._state == VhCityState.Disabled)
                    {
                         minutes -= 60;
                         this.SendMessage(this._bot, HTML.CreateColorString(this._bot.ColorHeaderHex, minutes.ToString()) + " minutes have passed since the city cloaking device has fully recovered. Please enable the city cloaking device");
                         if (this._reminders == VhCityRemindState.Announced)
                             this._reminders = VhCityRemindState.PostAnnounce;
                         else
                             this._reminders = VhCityRemindState.Unknown;
                    }
                    break;
            }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "cloak":
                    if (this.State == VhCityState.Enabled && this.TimeLeft.TotalMinutes <= 0)
                        bot.SendReply(e, "The city cloak is currently " + HTML.CreateColorString(RichTextWindow.ColorGreen, "enabled") + " and has been fully charged");
                    else if (this.State == VhCityState.Enabled)
                        bot.SendReply(e, "The city cloak is currently " + HTML.CreateColorString(RichTextWindow.ColorGreen, "enabled") + " and will finish charging in " + Format.Time(this.TimeLeft, FormatStyle.Medium));
                    else if (this.State == VhCityState.Disabled && this.TimeLeft.TotalMinutes <= 0)
                        bot.SendReply(e, "The city cloak is currently " + HTML.CreateColorString(RichTextWindow.ColorRed, "disabled") + " and requires enabling");
                    else if (this.State == VhCityState.Disabled)
                        bot.SendReply(e, "The city cloak is currently " + HTML.CreateColorString(RichTextWindow.ColorRed, "disabled") + " and will require enabling in " + Format.Time(this.TimeLeft, FormatStyle.Medium));
                    else
                        bot.SendReply(e, "The city cloak state is currently " + HTML.CreateColorString(RichTextWindow.ColorOrange, "unknown"));
                    break;
                case "cloak reset":
                    this._state = VhCityState.Unknown;
                    bot.SendReply(e, "The city cloak state has been reset to unknown");
                    break;
            }
        }

        public void SendMessage(BotShell bot, string message)
        {
            if (this._sendgc) bot.SendOrganizationMessage(bot.ColorHighlight + message);
            if (this._sendpg) bot.SendPrivateChannelMessage(bot.ColorHighlight + message);
            //if (this._sendirc)
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "cloak":
                    return "Displays the current state of the city cloak.\n" +
                        "Usage: /tell " + bot.Character + " cloak";
                case "cloak reset":
                    return "Resets the current state of the city cloak to unknown.\n" +
                        "Usage: /tell " + bot.Character + " cloak reset";
            }
            return null;
        }
    }
}
