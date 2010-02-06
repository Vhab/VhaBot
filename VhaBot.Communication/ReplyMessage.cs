using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    [Serializable]
    public class ReplyMessage : MessageBase
    {
        public readonly string SourcePlugin;
        public readonly object[] Args;
        public readonly DateTime Time;

        public ReplyMessage(MessageBase message, string sourcePlugin, params object[] args)
        {
            this._type = MessageType.Reply;
            this._id = message.ID;
            this._target = message.Source;
            this._source = message.Target;
            this.SourcePlugin = sourcePlugin;
            this.Args = args;
            this.Time = DateTime.Now;
        }
    }
}
