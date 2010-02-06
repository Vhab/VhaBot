using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using VhaBot.Communication;
using VhaBot.Configuration;

namespace VhaBot.Shell
{
    class Program
    {
        static void Main(string[] args)
        {
            // Error handling
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);

            // Parse arguments
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            foreach (string arg in args)
            {
                if (!arg.Contains("=") || arg.IndexOf("=") + 1 == arg.Length)
                {
                    Console.WriteLine("Invalid argument: " + arg);
                    continue;
                }
                string argKey = arg.Substring(0, arg.IndexOf("="));
                string argValue = arg.Substring(arg.IndexOf("=") + 1);
                arguments.Add(argKey, argValue);
            }

            // Verify arguments
            int port = -1;
            if (arguments.ContainsKey("port"))
            {
                try { port = Convert.ToInt32(arguments["port"]); }
                catch { }
            }
            if (port < 1)
            {
                Console.WriteLine("No valid remoting port specified");
                Environment.Exit(ExitCodes.INVALID_ARGUMENT);
                return;
            }
            string id = null;
            if (arguments.ContainsKey("id"))
                id = arguments["id"];
            if (id == null || id == string.Empty)
            {
                Console.WriteLine("No valid id specified");
                Environment.Exit(ExitCodes.INVALID_ARGUMENT);
                return;
            }
            string key = null;
            if (arguments.ContainsKey("key"))
                key = arguments["key"];
            if (key == null || key == string.Empty)
            {
                Console.WriteLine("No valid key specified");
                Environment.Exit(ExitCodes.INVALID_ARGUMENT);
                return;
            }
            int pid = -1;
            if (arguments.ContainsKey("pid"))
            {
                try { pid = Convert.ToInt32(arguments["pid"]); }
                catch { }
            }
            if (pid < 1)
            {
                Console.WriteLine("No valid core process id specified");
                Environment.Exit(ExitCodes.INVALID_ARGUMENT);
                return;
            }

            // Be sure we know when the core goes down
            Process coreProcess = Process.GetProcessById(pid);
            if (coreProcess == null)
            {
                Console.WriteLine("Unable to access VhaBot.Core process");
                Environment.Exit(ExitCodes.COMMUNICATION_LOST);
                return;
            }
            coreProcess.Exited += new EventHandler(OnCoreShutdown);

            // Fix working directory
            string path = Assembly.GetExecutingAssembly().Location;
            if (path.LastIndexOf(Path.DirectorySeparatorChar) > 0)
            {
                path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                Environment.CurrentDirectory = path;
            }

            // Connect to core
            Console.WriteLine("Connecting to VhaBot.Core on port " + port);
            ClientCommunication communication;
            try
            {
                ServerCommunication server = (ServerCommunication)Activator.GetObject(typeof(ServerCommunication), "tcp://127.0.0.1:" + port + "/ServerCommunication");
                communication = server.AuthorizeClient(id, key);
                if (communication == null)
                {
                    Console.WriteLine("Unable to authorize client on VhaBot.Core");
                    Environment.Exit(ExitCodes.REMOTING_FAILED);
                    return;
                }
            }
            catch
            {
                Console.WriteLine("An error occurred while connecting to VhaBot.Core");
                Environment.Exit(ExitCodes.REMOTING_FAILED);
                return;
            }
            Console.WriteLine("Connected to VhaBot.Core (" + communication.CoreID + ")");
            ConfigurationBot configurationBot = communication.GetConfigurationBot();
            ConfigurationCore configurationCore = communication.GetConfigurationCore();
            if (configurationBot == null || configurationCore == null)
            {
                Console.WriteLine("An error occurred while fetching configuration from VhaBot.Core");
                Environment.Exit(ExitCodes.NO_CONFIGURATION);
                return;
            }
            Console.WriteLine("Received bot configuration for " + configurationBot.ToString());

            // Verify core PID
            if (pid != communication.CoreID)
            {
                Console.WriteLine("Core process id appeared to be invalid. " + pid + " was specified but the actual id is " + communication.CoreID);
                Environment.Exit(ExitCodes.INVALID_ARGUMENT);
                return;
            }

            // Initiate the bot
            Console.WriteLine("Starting BotShell");
            BotShell bot = new BotShell(configurationBot, configurationCore, new SendMessageHandler(communication.SendMessage));

            // Main loop, check for messages and relay
            int collectTimeout = 1;
            while (true)
            {
                coreProcess.Refresh();
                if (coreProcess.HasExited)
                {
                    Console.WriteLine("Core shutdown");
                    Environment.Exit(ExitCodes.SHUTDOWN);
                }
                try { communication.Ping(); }
                catch
                {
                    Environment.Exit(ExitCodes.COMMUNICATION_LOST);
                    return;
                }
                while (communication.QueueSize > 0)
                {
                    MessageBase message = communication.Dequeue();
                    bot.RelayMessage(message);
                }
                // Cleanup duties every 60 seconds
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
            Environment.Exit(ExitCodes.UNKNOWN_ERROR);
        }

        static void OnCoreShutdown(object sender, EventArgs e)
        {
            Console.WriteLine("Core shutdown");
            Environment.Exit(ExitCodes.SHUTDOWN);
        }
    }
}
