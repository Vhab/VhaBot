using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using VhaBot.Configuration;

namespace VhaBot.Lite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Error handling
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);

            // Fix working directory
            string path = Assembly.GetExecutingAssembly().Location;
            if (path.LastIndexOf(Path.DirectorySeparatorChar) > 0)
            {
                path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                Environment.CurrentDirectory = path;
            }
            Console.WriteLine("Path set to: " + Environment.CurrentDirectory);

            // Read configuration file
            string configurationFile = "config.xml";
            if (args.Length > 0) configurationFile = string.Join(" ", args);
            Console.WriteLine("Loading configuration file: " + configurationFile);
            ConfigurationBase configuration = ConfigurationReader.Read(configurationFile);
            if (configuration == null || configuration.Core == null || configuration.Bots.Length < 1)
            {
                Environment.Exit(1);
                return;
            }
            ConfigurationBot configurationBot = configuration.Bots[0];
            
            // Start bot
            try { Console.Title = "VhaBot " + BotShell.VERSION + " " + BotShell.EDITION + " Edition [" + configurationBot.GetID() + "]"; }
            catch { }
            Console.WriteLine("Starting " + configurationBot.GetID());
            BotShell bot = new BotShell(configurationBot, configuration.Core, null);

            // Main loop, just keep it alive
            int collectTimeout = 1;
            while (true)
            {
                collectTimeout--;
                if (collectTimeout < 1)
                {
                    bot.Clean();
                    collectTimeout = 600;
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception: " + e.ExceptionObject.ToString());
            Environment.Exit(2);
        }
    }
}
