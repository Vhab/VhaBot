using System;
using System.Collections.Generic;
using System.Text;
using VhaBot.Communication;

namespace VhaBot
{
    public delegate void BotStateChangedHandler(BotShell bot, BotStateChangedArgs e);
    public delegate void ChannelJoinEventHandler(BotShell bot, ChannelJoinEventArgs e);

    public delegate void UserJoinChannelHandler(BotShell bot, UserJoinChannelArgs e);
    public delegate void UserJoinChannelSlaveHandler(BotShell bot, SlaveArgs slave, UserJoinChannelArgs e);
    public delegate void UserLeaveChannelHandler(BotShell bot, UserLeaveChannelArgs e);
    public delegate void UserLeaveChannelSlaveHandler(BotShell bot, SlaveArgs slave, UserLeaveChannelArgs e);

    public delegate void UserLogonHandler(BotShell bot, UserLogonArgs e);
    public delegate void UserLogonSlaveHandler(BotShell bot, SlaveArgs slave, UserLogonArgs e);
    public delegate void UserLogoffHandler(BotShell bot, UserLogoffArgs e);
    public delegate void UserLogoffSlaveHandler(BotShell bot, SlaveArgs slave, UserLogoffArgs e);

    public delegate void PrivateMessageHandler(BotShell bot, PrivateMessageArgs e);
    public delegate void PrivateMessageSlaveHandler(BotShell bot, SlaveArgs slave, PrivateMessageArgs e);
    public delegate void PrivateChannelMessageHandler(BotShell bot, PrivateChannelMessageArgs e);
    public delegate void PrivateChannelMessageSlaveHandler(BotShell bot, SlaveArgs slave, PrivateChannelMessageArgs e);
    public delegate void ChannelMessageHandler(BotShell bot, ChannelMessageArgs e);
    public delegate void ChannelMessageSlaveHandler(BotShell bot, SlaveArgs slave, ChannelMessageArgs e);

    public delegate void MemberAddedHandler(BotShell bot, MemberAddedArgs e);
    public delegate void MemberRemovedHandler(BotShell bot, MemberRemovedArgs e);
    public delegate void MemberUpdatedHandler(BotShell bot, MemberUpdatedArgs e);

    public delegate void AltAddedHandler(BotShell bot, AltAddedArgs e);
    public delegate void AltRemovedHandler(BotShell bot, AltRemovedArgs e);

    public delegate void ConfigurationChangedHandler(BotShell bot, ConfigurationChangedArgs e);
    public delegate MessageResult SendMessageHandler(MessageBase message);
}
