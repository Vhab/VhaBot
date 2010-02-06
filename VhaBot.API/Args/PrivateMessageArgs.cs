using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class PrivateMessageArgs : MessageArgs
    {
        private bool _command;
        private bool _disableAutoMessage = false;

        public PrivateMessageArgs(BotShell bot, UInt32 senderID, string sender, string message, bool command, bool self)
        {
            this._dimension = bot.Dimension;
            this._senderID = senderID;
            this._sender = sender;
            this._message = message;
            this._command = command;
            this._self = self;
        }

        public bool Command { get { return this._command; } set { this._command = value; } }
        public bool DisableAutoMessage
        {
            get { return this._disableAutoMessage; }
            set { this._disableAutoMessage = value; }
        }
    }
}