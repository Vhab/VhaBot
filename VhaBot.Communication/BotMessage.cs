using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    [Serializable]
    public class BotMessage : MessageBase
    {
        public readonly string SourcePlugin;
        public readonly string Command;
        public readonly object[] Args;

        public BotMessage(string target, string source, string sourcePlugin, string command, params object[] args)
        {
            this._type = MessageType.Plugin;
            this._target = target;
            this._source = source;
            this.SourcePlugin = sourcePlugin;
            this.Command = command;
            this.Args = args;
        }
    }
}
