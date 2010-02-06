using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    public enum CoreCommand
    {
        Shutdown,
        Restart,
        Start
    }

    [Serializable]
    public class CoreMessage : MessageBase
    {
        public readonly string SourcePlugin;
        public readonly CoreCommand Command;

        public CoreMessage(string target, string source, string sourcePlugin, CoreCommand command)
        {
            this._type = MessageType.Core;
            this._target = target;
            this._source = source;
            this.SourcePlugin = sourcePlugin;
            this.Command = command;
        }
    }
}
