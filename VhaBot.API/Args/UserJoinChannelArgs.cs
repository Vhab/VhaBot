using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class UserJoinChannelArgs : SenderArgs
    {
        private UInt32 _channelID;
        private string _channel;
        private bool _local;

        public UserJoinChannelArgs(BotShell bot, UInt32 senderID, string sender, UInt32 channelID, string channel, bool local)
        {
            this._dimension = bot.Dimension;
            this._senderID = senderID;
            this._sender = sender;
            this._channelID = channelID;
            this._channel = channel;
            this._local = local;
        }

        public UInt32 ChannelID { get { return this._channelID; } }
        public string Channel { get { return this._channel; } }
        public bool Local { get { return this._local; } }
    }
}
