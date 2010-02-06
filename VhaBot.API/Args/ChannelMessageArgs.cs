using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class ChannelMessageArgs : MessageArgs
    {
        private Int32 _channelID;
        private string _channel;
        private ChannelType _type;
        private bool _command;

        public ChannelMessageArgs(BotShell bot, UInt32 senderID, string sender, Int32 channelID, string channel, string message, ChannelType type, bool command, bool self)
        {
            this._dimension = bot.Dimension;
            this._senderID = senderID;
            this._sender = sender;
            this._channelID = channelID;
            this._channel = channel;
            this._message = message;
            this._type = type;
            this._command = command;
            this._self = self;
        }

        public Int32 ChannelID { get { return this._channelID; } }
        public string Channel { get { return this._channel; } }
        public ChannelType Type { get { return this._type; } }
        public bool Command { get { return this._command; } set { this._command = value; } }
    }
}
