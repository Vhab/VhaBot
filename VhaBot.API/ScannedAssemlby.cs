using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace VhaBot
{
    public class ScannedAssembly
    {
        public readonly Assembly Assembly;
        public readonly AssemlbyType Type;
        public readonly string File;

        public ScannedAssembly(Assembly assembly, AssemlbyType type, string file)
        {
            this.Assembly = assembly;
            this.Type = type;
            this.File = file;
        }
    }
}
