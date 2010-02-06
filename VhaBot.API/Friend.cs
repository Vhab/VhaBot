using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class Friend
    {
        private string _user;
        private UInt32 _userID;
        private DateTime _time;
        private bool _onSlave;
        private Int32 _botID;

        public Friend(string user, UInt32 userID, DateTime time, bool onSlave, Int32 botID)
        {
            this._user = user;
            this._userID = userID;
            this._time = time;
            this._onSlave = onSlave;
            this._botID = botID;
        }

        public string User { get { return this._user; } }
        public UInt32 UserID { get { return this._userID; } }
        public DateTime Time { get { return this._time; } }
        public bool OnSlave { get { return this._onSlave; } }
        public Int32 BotID { get { return this._botID; } }
    }
}
