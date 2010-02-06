using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class UserLogonArgs : SenderArgs
    {
        private bool _first;
        private List<string> _sections;

        public UserLogonArgs(BotShell bot, UInt32 senderID, string sender, bool first, List<string> sections)
        {
            this._dimension = bot.Dimension;
            this._senderID = senderID;
            this._sender = sender;
            this._first = first;
            this._sections = sections;
        }

        public bool First { get { return this._first; } }
        public List<string> Sections { get { return this._sections; } }
    }
}
