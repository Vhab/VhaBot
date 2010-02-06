using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class IntraBotMessageArgs : MarshalByRefObject
    {
        public readonly string CallingBot;
        public readonly string CallingPlugin;
        public readonly string TargetBot;
        public readonly string TargetPlugin;
        public readonly string Command;
        public readonly object[] Args;

        public IntraBotMessageArgs(string callingBot, string callingPlugin, string targetBot, string targetPlugin, string command, object[] args)
        {
            this.CallingBot = callingBot;
            this.CallingPlugin = callingPlugin;
            this.TargetBot = targetBot;
            this.TargetPlugin = targetPlugin;
            this.Command = command;
            this.Args = args;
        }
    }
}
