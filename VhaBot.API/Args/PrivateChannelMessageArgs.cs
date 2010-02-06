using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class PrivateChannelMessageArgs : MessageArgs
    {
        private UInt32 _channelID;
        private string _channel;
        private bool _local;
        private bool _command;

        public PrivateChannelMessageArgs(BotShell bot, UInt32 senderID, string sender, UInt32 channelID, string channel, bool local, string message, bool command, bool self)
        {
            this._dimension = bot.Dimension;
            this._senderID = senderID;
            this._sender = sender;
            this._channelID = channelID;
            this._channel = channel;
            this._local = local;
            this._message = message;
            this._command = command;
            this._self = self;
        }

        public UInt32 ChannelID { get { return this._channelID; } }
        public string Channel { get { return this._channel; } }
        public bool Local { get { return this._local; } }
        public bool Command { get { return this._command; } set { this._command = value; } }
    }
}
