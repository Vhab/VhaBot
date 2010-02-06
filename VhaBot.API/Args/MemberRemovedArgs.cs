using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class MemberRemovedArgs : EventArgs
    {
        private UInt32 _userID;
        private string _user;
        private string _alts;

        public MemberRemovedArgs(UInt32 userID, string user, string alts)
        {
            this._userID = userID;
            this._user = user;
            this._alts = alts;
        }

        public UInt32 UserID { get { return this._userID; } }
        public string User { get { return this._user; } }
        public string Alts { get { return this._alts; } }
    }
}
