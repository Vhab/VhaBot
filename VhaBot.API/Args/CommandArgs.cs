using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot
{
    public class CommandArgs : MessageArgs
    {
        private CommandType _type;
        private UInt32 _channelID;
        private string _command;
        private bool _fromSlave;
        private SlaveArgs _slaveArgs;
        private bool _authorized = true;

        // Override SenderArgs
        private new string _sender;
        private new WhoisResult _senderWhois;

        public CommandArgs(CommandType type, UInt32 channelID, UInt32 senderID, string sender, WhoisResult senderWhois, string command, string message, bool fromSlave, SlaveArgs slaveArgs)
        {
            this._senderWhois = senderWhois;
            this._type = type;
            this._senderID = senderID;
            this._channelID = channelID;
            this._sender = sender;
            this._message = message.Trim();
            this._command = command.Trim();
            this._fromSlave = fromSlave;
            this._slaveArgs = slaveArgs;
            this._self = false;
        }

        public CommandType Type { get { return this._type; } }
        public UInt32 ChannelID { get { return this._channelID; } }
        public string Command { get { return this._command; } }
        public bool FromSlave { get { return this._fromSlave; } }
        public SlaveArgs SlaveArgs { get { return this._slaveArgs; } }
        public bool Authorized { get { return this._authorized; } set { this._authorized = value; } }

        //Override SenderArgs
        public new string Sender { get { return this._sender; } }
        public new WhoisResult SenderWhois { get { return this._senderWhois; } }
    }
}
