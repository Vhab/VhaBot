using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.ShellModules
{
    public class Events
    {
        public event BotStateChangedHandler BotStateChangedEvent;
        public event ChannelJoinEventHandler ChannelJoinEvent;

        public event UserJoinChannelHandler UserJoinChannelEvent;
        public event UserLeaveChannelHandler UserLeaveChannelEvent;

        public event UserLogonHandler UserLogonEvent;
        public event UserLogoffHandler UserLogoffEvent;

        public event PrivateMessageHandler PrivateMessageEvent;
        public event PrivateChannelMessageHandler PrivateChannelMessageEvent;
        public event ChannelMessageHandler ChannelMessageEvent;

        public event MemberAddedHandler MemberAddedEvent;
        public event MemberRemovedHandler MemberRemovedEvent;
        public event MemberUpdatedHandler MemberUpdatedEvent;

        public event AltAddedHandler AltAddedEvent;
        public event AltRemovedHandler AltRemovedEvent;

        public event ConfigurationChangedHandler ConfigurationChangedEvent;

        internal void OnBotStateChanged(BotShell bot, BotStateChangedArgs e)
        {
            if (this.BotStateChangedEvent != null)
                try { this.BotStateChangedEvent(bot, e); }
                catch { }
        }
        internal void OnChannelJoin(BotShell bot, ChannelJoinEventArgs e)
        {
            if (this.ChannelJoinEvent != null)
                try { this.ChannelJoinEvent(bot, e); }
                catch { }
        }
        internal void OnUserJoinChannel(BotShell bot, UserJoinChannelArgs e)
        {
            if (this.UserJoinChannelEvent != null)
                try { this.UserJoinChannelEvent(bot, e); }
                catch { }
        }
        internal void OnUserLeaveChannel(BotShell bot, UserLeaveChannelArgs e)
        {
            if (this.UserLeaveChannelEvent != null)
                try { this.UserLeaveChannelEvent(bot, e); }
                catch { }
        }
        internal void OnUserLogon(BotShell bot, UserLogonArgs e)
        {
            if (this.UserLogonEvent != null)
                try { this.UserLogonEvent(bot, e); }
                catch { }
        }
        internal void OnUserLogoff(BotShell bot, UserLogoffArgs e)
        {
            if (this.UserLogoffEvent != null)
                try { this.UserLogoffEvent(bot, e); }
                catch { }
        }
        internal void OnPrivateMessage(BotShell bot, PrivateMessageArgs e)
        {
            if (this.PrivateMessageEvent != null)
                try { this.PrivateMessageEvent(bot, e); }
                catch { }
        }
        internal void OnPrivateChannelMessage(BotShell bot, PrivateChannelMessageArgs e)
        {
            if (this.PrivateChannelMessageEvent != null)
                try { this.PrivateChannelMessageEvent(bot, e); }
                catch { }
        }
        internal void OnChannelMessage(BotShell bot, ChannelMessageArgs e)
        {
            if (this.ChannelMessageEvent != null)
                try { this.ChannelMessageEvent(bot, e); }
                catch { }
        }
        internal void OnMemberAdded(BotShell bot, MemberAddedArgs e)
        {
            if (this.MemberAddedEvent != null)
                try { this.MemberAddedEvent(bot, e); }
                catch { }
        }
        internal void OnMemberRemoved(BotShell bot, MemberRemovedArgs e)
        {
            if (this.MemberRemovedEvent != null)
                try { this.MemberRemovedEvent(bot, e); }
                catch { }
        }
        internal void OnMemberUpdated(BotShell bot, MemberUpdatedArgs e)
        {
            if (this.MemberUpdatedEvent != null)
                try { this.MemberUpdatedEvent(bot, e); }
                catch { }
        }
        internal void OnAltAdded(BotShell bot, AltAddedArgs e)
        {
            if (this.AltAddedEvent != null)
                try { this.AltAddedEvent(bot, e); }
                catch { }
        }
        internal void OnAltRemoved(BotShell bot, AltRemovedArgs e)
        {
            if (this.AltRemovedEvent != null)
                try { this.AltRemovedEvent(bot, e); }
                catch { }
        }
        internal void OnConfigurationChanged(BotShell bot, ConfigurationChangedArgs e)
        {
            if (this.ConfigurationChangedEvent != null)
                try { this.ConfigurationChangedEvent(bot, e); }
                catch { }
        }
    }
}
