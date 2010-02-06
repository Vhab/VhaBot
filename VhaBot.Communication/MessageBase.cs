using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    public enum MessageResult
    {
        Success,
        InvalidTarget,
        InvalidMessage,
        InvalidSource,
        NotConnected,
        NotAuthorized,
        Error
    }

    public enum MessageType
    {
        Core,
        Plugin,
        Bot,
        Reply
    }

    [Serializable]
    public abstract class MessageBase
    {
        protected MessageType _type;
        public MessageType Type { get { return this._type; } }

        protected string _target;
        public string Target { get { return this._target; } }

        protected string _source;
        public string Source { get { return this._source; } }

        protected int _id = -1;
        public int ID
        {
            get
            {
                if (this._id == -1)
                {
                    Random rand = new Random();
                    this._id = rand.Next(1000000000, 2147483647);
                }
                return this._id;
            }
        }
    }
}
