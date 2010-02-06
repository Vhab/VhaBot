using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class CommandRights
    {
        public UserLevel PrivateChannel = UserLevel.Guest;
        public UserLevel PrivateMessage = UserLevel.Guest;
        public UserLevel Organization = UserLevel.Guest;
        public string Plugin = "";
        public bool IsAlias = false;
        public string MainCommand = "";
        public bool Help = false;

        public bool Equals(CommandRights obj)
        {
            if (this.Organization != obj.Organization)
                return false;
            if (this.PrivateChannel != obj.PrivateChannel)
                return false;
            if (this.PrivateMessage != obj.PrivateMessage)
                return false;
            return true;
        }
    }
}
