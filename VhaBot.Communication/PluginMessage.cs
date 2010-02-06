using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    [Serializable]
    public class PluginMessage : MessageBase
    {
        public readonly string TargetPlugin;
        public readonly string SourcePlugin;
        public readonly string Command;
        public readonly object[] Args;

        public PluginMessage(string target, string source, string targetPlugin, string sourcePlugin, string command, params object[] args)
        {
            this._type = MessageType.Plugin;
            this._target = target;
            this._source = source;
            this.TargetPlugin = targetPlugin;
            this.SourcePlugin = sourcePlugin;
            this.Command = command;
            this.Args = args;
        }
    }
}
