using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class AltAddedArgs : EventArgs
    {
        private UInt32 _altID;
        private string _alt;
        private UInt32 _userID;
        private string _user;
        private UserLevel _level;

        public AltAddedArgs(UInt32 altID, string alt, UInt32 userID, string user, UserLevel level)
        {
            this._altID = altID;
            this._alt = alt;
            this._userID = userID;
            this._user = user;
            this._level = level;
        }

        public UInt32 AltID { get { return this._altID; } }
        public string Alt { get { return this._alt; } }
        public UInt32 UserID { get { return this._userID; } }
        public string User { get { return this._user; } }
        public UserLevel Level { get { return this._level; } }
    }
}
