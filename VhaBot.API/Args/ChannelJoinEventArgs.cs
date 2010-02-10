using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class ChannelJoinEventArgs : EventArgs
    {
        private BigInteger _groupID;
        private String _groupName;
        private bool _mute;
        private bool _logging;
        private ChannelType _groupType;

        public ChannelJoinEventArgs(BigInteger GroupID, String GroupName, bool Mute, bool Logging, ChannelType GroupType)
        {
            this._groupID = GroupID;
            this._groupName = GroupName;
            this._mute = Mute;
            this._logging = Logging;
            this._groupType = GroupType;
        }

        public BigInteger GroupID { get { return this._groupID; } }
        public String GroupName { get { return this._groupName; } }
        public bool Mute { get { return this._mute; } }
        public bool Logging { get { return this._logging; } }
        public ChannelType GroupType { get { return this._groupType; } }
    }
}