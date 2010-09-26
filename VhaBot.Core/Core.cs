using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using VhaBot.Configuration;
using VhaBot.Communication;

namespace VhaBot.Core
{
    public class Core
    {
        public static readonly string VERSION = "0.7.7";
        public static readonly string BRANCH = "Beta";
        public static readonly int BUILD = 20100101;

        private static object _consoleLock = new object();
        private string _configurationFile;
        private Dictionary<string, BotManager> _bots = new Dictionary<string,BotManager>();
        private Dictionary<int, string> _processes = new Dictionary<int, string>();
        private ConfigurationCore _configuration;
        private bool _running = true;
        private IpcChannel _channel = null;
        private string _channelName = "VhaBotCore";
        private Queue<CoreMessage> _queue = new Queue<CoreMessage>();
        private static Dictionary<string, ConsoleColor> _colors = new Dictionary<string, ConsoleColor>();
        private static int _colorsCount = 0;

        public Core(string configurationFile)
        {
            // Fix working directory
            string path = Assembly.GetExecutingAssembly().Location;
            if (path.LastIndexOf(Path.DirectorySeparatorChar) > 0)
            {
                path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                Environment.CurrentDirectory = path;
            }
            Core.Output("Core", "Path set to: " + Environment.CurrentDirectory);

            // Read configuration file
            this._configurationFile = configurationFile;
            if (!this.ReadConfiguration())
            {
                Environment.Exit(ExitCodes.NO_CONFIGURATION);
                return;
            }

            // Prepare remoting callbacks
            ServerCommunication.OnAuthorizeClient = new AuthorizeClientDelegate(this.AuthorizeClient);
            Core.Output("Core", "Registered remoting callbacks");

            // Start remoting server
            try
            {
                BinaryServerFormatterSinkProvider serverFormatter = new BinaryServerFormatterSinkProvider();
                serverFormatter.TypeFilterLevel = TypeFilterLevel.Full;
                this._channelName += new Random().Next(int.MaxValue);
                this._channel = new IpcChannel(this._channelName);
                ChannelServices.RegisterChannel(this._channel, false);
                Core.Output("Core", "Registered remoting channel");
            }
            catch
            {
                Core.Output("Core", "Unable to registered remoting channel");
                Environment.Exit(ExitCodes.REMOTING_FAILED);
            }

            // Register remoting API
            try
            {
                WellKnownObjectMode mode = WellKnownObjectMode.Singleton;
                WellKnownServiceTypeEntry entry = new WellKnownServiceTypeEntry(typeof(ServerCommunication), "ServerCommunication", mode);
                RemotingConfiguration.RegisterWellKnownServiceType(entry);
                Core.Output("Core", "Registered remoting API");
            }
            catch
            {
                Core.Output("Core", "Unable to registered remoting API");
                Environment.Exit(ExitCodes.REMOTING_FAILED);
            }

            // Initial start of bots
            List<string> bots;
            lock (this._bots)
            {
                bots = new List<string>(this._bots.Keys);
                foreach (string bot in bots)
                {
                    if (this._bots[bot].Enabled)
                        this.StartShell(bot);
                    Thread.Sleep(1000);
                }
            }

            // Clean shutdown
            /*lock (this._bots) bots = new List<string>(this._bots.Keys);
            foreach (string bot in bots)
            {
                lock (this._bots)
                {
                    if (!this._bots[bot].Enabled) continue;

                }
            }*/
        }

        public void Loop()
        {
            while (this._running)
            {
                this.LoopOne();
                Thread.Sleep(1000);
            }
        }

        public void LoopOne()
        {

            // Verify bots and handle messages
            List<string> bots;
            lock (this._bots) bots = new List<string>(this._bots.Keys);
            foreach (string bot in bots)
            {
                lock (this._bots)
                {
                    if (!this._bots[bot].Enabled) continue;
                    if (!this._bots[bot].ProcessRunning)
                    {
                        if (this._bots[bot].Process != null)
                        {
                            switch (this._bots[bot].Process.ExitCode)
                            {
                                case ExitCodes.SHUTDOWN:
                                    this._bots[bot].Enabled = false;
                                    Core.Output("Core", bot + " has shutdown and marked disabled");
                                    break;
                                case ExitCodes.RESTART:
                                    this._bots[bot].Enabled = true;
                                    Core.Output("Core", bot + " has shutdown pending a restart");
                                    this.StartShell(bot);
                                    break;
                                default:
                                    Core.Output("Core", bot + " has terminated with the exit code " + this._bots[bot].Process.ExitCode);
                                    this.StartShell(bot);
                                    break;
                            }
                        }
                    }
                    else if (this._bots[bot].Communication != null)
                    {
                        if (this._bots[bot].Communication.IdleTime.TotalMinutes > 5)
                        {
                            Core.Output("Core", bot + " hasn't pinged for over 5 minutes");
                            this.StartShell(bot);
                        }
                    }
                    else
                    {
                        if (((TimeSpan)(DateTime.Now - this._bots[bot].ProcessStartTime)).TotalMinutes > 1)
                        {
                            Core.Output("Core", bot + " hasn't connected within 1 minute");
                            this.StartShell(bot);
                        }
                    }
                }
            }
            // Process core messages
            lock (this._queue)
            {
                while (this._queue.Count > 0)
                {
                    CoreMessage message = this._queue.Dequeue();
                    bool enabled;
                    lock (this._bots)
                    {
                        if (!this._bots.ContainsKey(message.Target))
                            continue;
                        enabled = this._bots[message.Target].ProcessRunning;
                    }
                    switch (message.Command)
                    {
                        case CoreCommand.Restart:
                            if (enabled) this.StopShell(message.Target);
                            this.StartShell(message.Target);
                            break;
                        case CoreCommand.Shutdown:
                            if (enabled) this.StopShell(message.Target);
                            break;
                        case CoreCommand.Start:
                            if (!enabled) this.StartShell(message.Target);
                            break;
                    }
                }
            }
        }

        public bool ReadConfiguration()
        {
            // Read configuration
            ConfigurationBase configuration = ConfigurationReader.Read(this._configurationFile);
            if (configuration == null)
            {
                Core.Output("Error", "Unable to load configuration file: " + this._configurationFile);
                return false;
            }
            this._configuration = configuration.Core;
            Core.Output("Core", "Loaded configuration file: " + this._configurationFile);

            // Prepare bot managers
            foreach (ConfigurationBot configurationBot in configuration.Bots)
            {
                lock (this._bots)
                {
                    BotManager bot;
                    if (!this._bots.ContainsKey(configurationBot.GetID()))
                    {
                        bot = new BotManager();
                        bot.Connected = false;
                        bot.Enabled = configurationBot.Enabled;
                        bot.Configuration = configurationBot;
                        this._bots.Add(bot.ID, bot);
                    }
                    else
                    {
                        bot = this._bots[configurationBot.GetID()];
                        bot.Enabled = configurationBot.Enabled;
                        bot.Configuration = configurationBot;
                    }
                    Core.Output("Core", "Registered " + configurationBot.ToString() + " as " + bot.ID);
                }
            }
            return true;
        }

        public bool StartShell(string bot)
        {
            // Get the bot manager
            BotManager botManager;
            lock (this._bots)
            {
                if (!this._bots.ContainsKey(bot))
                {
                    Core.Output("Core", "Unable to start " + bot + " (No such bot)");
                    return false;
                }
                botManager = this._bots[bot];
                botManager.Connected = false;
                botManager.Enabled = true;
                // Close previous process
                if (botManager.ProcessRunning)
                {
                    try
                    {
                        botManager.Process.Kill();
                        botManager.Process.WaitForExit(5000);
                    }
                    catch { }
                    Core.Output("Core", "Closed previous running process of " + botManager.ID);
                }
                // Prepare new process
                string args = "channel=" + this._channelName + " id=" + bot + " key=" + botManager.Configuration.ToSecretKey() + " pid=" + Process.GetCurrentProcess().Id;
                ProcessStartInfo startInfo;
                Type mono = Type.GetType("Mono.Runtime");
                if (mono == null)
                    startInfo = new ProcessStartInfo("VhaBot.Shell.exe", args);
                else
                    startInfo = new ProcessStartInfo("mono", "VhaBot.Shell.exe " + args);

                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                Core.Output("Core", "Starting " + botManager.ID);
                try
                {
                    // Start new process
                    botManager.Process = Process.Start(startInfo);
                    // Redirect output
                    botManager.Process.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputReceived);
                    lock (this._processes)
                        if (this._processes.ContainsKey(botManager.Process.Id))
                            this._processes[botManager.Process.Id] = botManager.ID;
                        else
                            this._processes.Add(botManager.Process.Id, botManager.ID);
                    botManager.Process.BeginOutputReadLine();
                    botManager.ProcessStartTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Core.Output("Core", "Unable to start " + botManager.ID + " (" + ex.Message + ")");
                    return false;
                }
                return true;
            }
        }

        public void StopShell(string bot)
        {
            BotManager botManager;
            lock (this._bots)
            {
                if (!this._bots.ContainsKey(bot))
                {
                    Core.Output("Core", "Unable to stop " + bot + " (No such bot)");
                    return;
                }
                botManager = this._bots[bot];
                botManager.Connected = false;
                // Close previous process
                Core.Output("Core", "Initiating forced shutdown of " + botManager.ID);
                if (botManager.ProcessRunning)
                {
                    try
                    {
                        botManager.Process.Kill();
                        botManager.Process.WaitForExit(15000);
                        Core.Output("Core", "Closed process of " + botManager.ID);
                    }
                    catch { }
                }
                botManager.Communication = null;
                botManager.Enabled = false;
                if (botManager.Process != null)
                {
                    botManager.Process.Dispose();
                    botManager.Process = null;
                }
                if (botManager.Queue != null)
                    lock (botManager.Queue)
                        botManager.Queue.Clear();
            }
        }

        private void ProcessOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (sender == null) return;
            if (e.Data == null || e.Data == string.Empty) return;
            string tag = "Unknown Source";
            lock (this._processes)
                if (this._processes.ContainsKey(((Process)sender).Id))
                    tag = this._processes[((Process)sender).Id];
            Core.Output(tag, e.Data);
        }

        public ClientCommunication AuthorizeClient(string id, string key)
        {
            BotManager botManager = null;
            lock (this._bots)
            {
                if (this._bots.ContainsKey(id) && this._bots[id].Configuration.ToSecretKey() == key)
                    botManager = this._bots[id];
            }
            if (botManager == null)
                return null;
            if (botManager.Communication != null)
                botManager.Communication.Dispose();
            botManager.Queue = new Queue<MessageBase>();
            botManager.Communication = new ClientCommunication(
                botManager.ID,
                botManager.Queue,
                new SendMessageDelegate(this.SendMessage),
                new GetConfigurationBotDelegate(botManager.GetConfigurationBot),
                new GetConfigurationCoreDelegate(this.GetConfigurationCore)
            );
            botManager.Connected = true;
            return botManager.Communication;
        }

        public MessageResult SendMessage(ClientCommunication sender, MessageBase message)
        {
            if (message == null) return MessageResult.InvalidMessage;
            if (sender == null) return MessageResult.InvalidSource;
            if (sender.ID != message.Source) return MessageResult.InvalidSource;

            lock (this._bots)
            {
                if (!this._bots.ContainsKey(message.Target))
                    return MessageResult.InvalidTarget;
                if (message.Type == MessageType.Core)
                {
                    if (!message.Target.Equals(message.Source, StringComparison.CurrentCultureIgnoreCase))
                        if (!this._bots[message.Source].Configuration.Master)
                            return MessageResult.NotAuthorized;
                    lock (this._queue)
                        this._queue.Enqueue((CoreMessage)message);
                }
                else
                {
                    this._bots[message.Target].Queue.Enqueue(message);
                }
            }
            return MessageResult.Success;
        }

        public ConfigurationCore GetConfigurationCore()
        {
            if (this._configuration == null)
                return null;
            return this._configuration;
        }

        public static void Output(string tag, string message)
        {
            // Define tag color
            ConsoleColor color = ConsoleColor.White;
            lock (_colors)
            {
                if (_colors.ContainsKey(tag.ToLower()))
                    color = _colors[tag.ToLower()];
                else
                {
                    _colorsCount++;
                    int colorCode = 0;
                    Math.DivRem(_colorsCount, 9, out colorCode);
                    switch (colorCode)
                    {
                        case 1:
                            color = ConsoleColor.Blue;
                            break;
                        case 2:
                            color = ConsoleColor.Cyan;
                            break;
                        case 3:
                            color = ConsoleColor.Green;
                            break;
                        case 4:
                            color = ConsoleColor.DarkYellow;
                            break;
                        case 5:
                            color = ConsoleColor.Red;
                            break;
                        case 6:
                            color = ConsoleColor.Blue;
                            break;
                        case 7:
                            color = ConsoleColor.Yellow;
                            break;
                        case 8:
                            color = ConsoleColor.Gray;
                            break;
                        case 9:
                            color = ConsoleColor.Magenta;
                            break;
                    }
                    _colors[tag.ToLower()] = color;
                }
            }
            lock (Core._consoleLock)
            {
                // Write to console
                Console.Write("[");
                try { Console.ForegroundColor = ConsoleColor.Blue; }
                catch { }
                Console.Write(DateTime.Now.ToLongTimeString());
                try { Console.ResetColor(); }
                catch { }
                Console.Write("] [");
                try { Console.ForegroundColor = color; }
                catch { }
                Console.Write(tag);
                try { Console.ResetColor(); }
                catch { }
                Console.Write("] ");
                Console.WriteLine(message);
                Trace.WriteLine("[" + tag + "] " + message);
                // Write to log
                try
                {
                    FileStream stream = File.Open(tag.ToLower() + ".log", FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write);
                    StreamWriter writer = new StreamWriter(stream);
                    writer.WriteLine(string.Format("{0:dd}-{0:MM}-{0:yy} {0:hh}:{0:mm}:{0:ss} {1}", DateTime.Now, message));
                    writer.Close();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
                catch { }
            }
        }
    }
}
