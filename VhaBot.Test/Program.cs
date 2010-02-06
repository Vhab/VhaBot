using System;
using System.Collections.Generic;
using System.Text;
using VhaBot.Core;

namespace Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Call VhaBot.Loader and pretend it was executed normally
            VhaBot.Core.Program.Main(new string[] { "config.xml" });
            //VhaBot.Lite.Program.Main(new string[] { "config.xml" });
        }
    }
}
