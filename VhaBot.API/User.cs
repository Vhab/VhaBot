using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class User
    {
        public readonly string Username;
        public readonly UInt32 UserID;
        public readonly UserLevel UserLevel;
        public readonly string AddedBy;
        public readonly long AddedOn;
        public readonly string[] Alts;

        public User(string username, UInt32 userID, UserLevel userLevel, string addedBy, long addedOn, string[] alts)
        {
            this.Username = username;
            this.UserID = userID;
            this.UserLevel = userLevel;
            this.AddedBy = addedBy;
            this.AddedOn = addedOn;
            this.Alts = alts;
        }
    }
}
