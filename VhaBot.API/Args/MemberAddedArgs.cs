using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class MemberAddedArgs : EventArgs
    {
        private UInt32 _userID;
        private string _user;
        private UserLevel _level;

        public MemberAddedArgs(UInt32 userID, string user, UserLevel level)
        {
            this._userID = userID;
            this._user = user;
            this._level = level;
        }

        public UInt32 UserID { get { return this._userID; } }
        public string User { get { return this._user; } }
        public UserLevel Level { get { return this._level; } }
    }
}
