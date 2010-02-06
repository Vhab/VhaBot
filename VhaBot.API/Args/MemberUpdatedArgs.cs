using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class MemberUpdatedArgs : EventArgs
    {
        private UInt32 _userID;
        private string _user;
        private string _alts;
        private UserLevel _level;

        public MemberUpdatedArgs(UInt32 userID, string user, string alts, UserLevel level)
        {
            this._userID = userID;
            this._user = user;
            this._alts = alts;
            this._level = level;
        }

        public UInt32 UserID { get { return this._userID; } }
        public string User { get { return this._user; } }
        public string Alts { get { return this._alts; } }
        public UserLevel Level { get { return this._level; } }
    }
}
