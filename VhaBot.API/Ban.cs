using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class Ban
    {
        public readonly string Character;
        public readonly UInt32 UserID;
        public readonly string AddedBy;
        public readonly Int64 AddedOn;

        public Ban(string character, UInt32 userID, string addedBy, Int64 addedOn)
        {
            this.Character = character;
            this.UserID = userID;
            this.AddedBy = addedBy;
            this.AddedOn = addedOn;
        }
    }
}
