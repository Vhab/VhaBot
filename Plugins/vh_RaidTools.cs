using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class RaidTools : PluginBase
    {
        private enum ShadowbreedState
        {
            Up,
            Down,
            Unknown,
            Unavailable
        }
        private List<string> Callers = new List<string>();
        private bool Counting = false;
        private bool Abort = false;
        private Dictionary<string, ShadowbreedState> Shadowbreeds = new Dictionary<string, ShadowbreedState>();

        public RaidTools()
        {
            this.Name = "Raid Tools";
            this.InternalName = "vhRaidTools";
            this.Author = "Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("callers", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest),
                new Command("callers add", true, UserLevel.Leader),
                new Command("callers remove", true, UserLevel.Leader),

                new Command("assist", true, UserLevel.Guest),
                new Command("target", true, UserLevel.Leader),
                new Command("t", "target"),
                new Command("order", true, UserLevel.Leader),
                new Command("o", "order"),
                new Command("command", true, UserLevel.Leader),
                new Command("c", "command"),
                new Command("cd", true, UserLevel.Leader),

                new Command("sb", true, UserLevel.Guest, UserLevel.Member, UserLevel.Disabled),
                new Command("sb set", false, UserLevel.Leader),
                new Command("sb get", false, UserLevel.Leader),
                new Command("sb reset", false, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Events.UserJoinChannelEvent += new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.UserLeaveChannelEvent += new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.UserJoinChannelEvent -= new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.UserLeaveChannelEvent -= new UserLeaveChannelHandler(Events_UserLeaveChannelEvent);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "callers":
                    this.OnCallersCommand(bot, e);
                    break;
                case "callers add":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: callers add [username]");
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    lock (this.Callers)
                    {
                        if (this.Callers.Contains(e.Args[0].ToLower()))
                        {
                            bot.SendReply(e, "That user is already on the callers list");
                            break;
                        }
                        this.Callers.Add(e.Args[0].ToLower());
                    }
                    bot.SendReply(e, "Added " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]) + " to the callers list");
                    this.OnCallersCommand(bot, e);
                    break;
                case "callers remove":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: callers remove [username]");
                        break;
                    }
                    if (e.Args[0].ToLower() == "all")
                    {
                        this.Callers.Clear();
                        bot.SendReply(e, "Clearing the callers list");
                        break;
                    }
                    lock (this.Callers)
                    {
                        if (!this.Callers.Contains(e.Args[0].ToLower()))
                        {
                            bot.SendReply(e, "That user is not on the callers list");
                            break;
                        }
                        this.Callers.Remove(e.Args[0].ToLower());
                    }
                    bot.SendReply(e, "Removed " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]) + " from the callers list");
                    break;
                case "assist":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: assist [username]");
                        break;
                    }
                    if (bot.GetUserID(e.Args[0]) < 1)
                    {
                        bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]));
                        break;
                    }
                    string assist = Format.UppercaseFirst(e.Args[0]);
                    RichTextWindow assistWindow = new RichTextWindow(bot);
                    assistWindow.AppendTitle("Assist " + assist);
                    assistWindow.AppendHighlight("Create Macro: ");
                    assistWindow.AppendCommand("Click", "/macro " + assist + " /assist " + assist);
                    assistWindow.AppendLineBreak();
                    assistWindow.AppendHighlight("Assist: ");
                    assistWindow.AppendCommand("Click", "/assist " + assist);
                    assistWindow.AppendLineBreak();
                    assistWindow.AppendHighlight("Manual Macro: ");
                    assistWindow.AppendNormal("/macro assist /assist " + assist);
                    assistWindow.AppendLineBreak();
                    bot.SendReply(e, "Assist " + assist + " »» " + assistWindow.ToString());
                    break;
                case "target":
                    if (e.Words.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: target [target]");
                        break;
                    }
                    string target = string.Format("{0}::: {1}Assist {3} to Terminate {0}::: {2}{4}", bot.ColorHighlight, HTML.CreateColorStart(RichTextWindow.ColorRed), bot.ColorNormal, e.Sender, e.Words[0]);
                    if (e.Type == CommandType.Organization)
                        this.SendMessage(bot, "gc", target);
                    else
                        this.SendMessage(bot, "pg", target);
                    break;
                case "command":
                    if (e.Words.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: command [command]");
                        break;
                    }
                    string command = string.Format("{0}::: {1}Raid Command from {3} {0}:::\n¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯\n    {2}{4}{0}\n______________________________________________", bot.ColorHighlight, HTML.CreateColorStart(RichTextWindow.ColorOrange), bot.ColorNormal, e.Sender, e.Words[0]);
                    if (e.Type == CommandType.Organization)
                        this.SendMessage(bot, "gc", command);
                    else
                        this.SendMessage(bot, "pg", command);
                    break;
                case "order":
                    if (e.Words.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: order [order]");
                        break;
                    }
                    string order = string.Format("{0}::: {1}Raid Order from {3} {0}::: {2}{4}", bot.ColorHighlight, HTML.CreateColorStart(RichTextWindow.ColorOrange), bot.ColorNormal, e.Sender, e.Words[0]);
                    if (e.Type == CommandType.Organization)
                        this.SendMessage(bot, "gc", order);
                    else
                        this.SendMessage(bot, "pg", order);
                    break;
                case "cd":
                    string channel = "pg";
                    if (e.Type == CommandType.Organization)
                        channel = "gc";

                    if (e.Args.Length > 0)
                    {
                        if (e.Args[0].ToLower() == "abort")
                        {
                            if (this.Counting)
                            {
                                this.Abort = true;
                                return;
                            }
                            bot.SendReply(e, "No countdown in progress");
                            return;
                        }
                    }
                    if (this.Counting)
                    {
                        bot.SendReply(e, "Countdown already in progress");
                        return;
                    }
                    this.Counting = true;
                    this.Abort = false;
                    string onThree = string.Empty;
                    if (e.Words.Length > 0)
                        onThree = e.Words[0];

                    for (int i = 5; i >= 0; i--)
                    {
                        if (this.Abort == true)
                        {
                            this.SendMessage(bot, channel, bot.ColorHighlight + "Countdown aborted!");
                            break;
                        }
                        if (i == 0)
                        {
                            this.SendMessage(bot, channel, bot.ColorHighlight + "-- " + HTML.CreateColorString(bot.ColorHeaderHex, "GO") + " --");
                            break;
                        }
                        this.SendMessage(bot, channel, bot.ColorHighlight + "--- " + HTML.CreateColorString(bot.ColorHeaderHex, i.ToString()) + " ---");
                        if (i == 3 && onThree != string.Empty)
                            this.SendMessage(bot, channel, bot.ColorHighlight + "-- " + HTML.CreateColorString(bot.ColorHeaderHex, onThree));
                        Thread.Sleep(1000);
                    }
                    this.Counting = false;
                    break;
                case "sb":
                    this.OnSbCommand(bot, e);
                    break;
                case "sb set":
                    if (e.Args.Length < 2)
                    {
                        bot.SendReply(e, "Correct Usage: sb adminset [username] [state]");
                        break;
                    }
                    lock (this.Shadowbreeds)
                    {
                        if (!this.Shadowbreeds.ContainsKey(e.Args[0].ToLower()))
                        {
                            bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]) + " is not on the Shadowbreed list");
                            break;
                        }
                    }
                    ShadowbreedState state = ShadowbreedState.Unknown;
                    try
                    {
                        state = (ShadowbreedState)Enum.Parse(typeof(ShadowbreedState), Format.UppercaseFirst(e.Args[1]));
                    }
                    catch
                    {
                        bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, e.Args[1]) + " is not a valid state.");
                        break;
                    }
                    lock (this.Shadowbreeds)
                        this.Shadowbreeds[e.Args[0].ToLower()] = state;
                    bot.SendReply(e, "Shadowbreed state for " + HTML.CreateColorString(bot.ColorHeaderHex, e.Args[0]) + " has been set to " + this.ColorizeState(state));
                    break;
                case "sb reset":
                    lock (this.Shadowbreeds)
                    {
                        this.Shadowbreeds.Clear();
                        bot.SendReply(e, "The Shadowbreed list has been reseted");
                    }
                    this.OnSbCommand(bot, e);
                    break;
                case "sb get":
                    List<string> list = new List<string>();
                    lock (this.Shadowbreeds)
                    {
                        if (this.Shadowbreeds.Count < 1)
                        {
                            bot.SendReply(e, "The Shadowbreeds list is empty");
                            return;
                        }

                        foreach (KeyValuePair<string, ShadowbreedState> kvp in this.Shadowbreeds)
                            if (kvp.Value == ShadowbreedState.Unknown)
                                list.Add(Format.UppercaseFirst(kvp.Key));
                    }
                    if (list.Count < 1)
                    {
                        bot.SendReply(e, "All Shadowbreed states are known.");
                        return;
                    }
                    bot.SendReply(e, "Requesting Shadowbreed state from: " + HTML.CreateColorString(bot.ColorHeaderHex, string.Join(", ", list.ToArray())));
                    foreach (string user in list)
                        this.RequestSbState(bot, user);
                    break;
            }
        }

        public override void OnUnauthorizedCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "sb set":
                    if (e.Args.Length > 1 && e.Args[0].ToLower() == e.Sender.ToLower())
                    {
                        e.Authorized = true;
                        this.OnCommand(bot, e);
                    }
                    break;
                case "target":
                case "command":
                case "order":
                case "cd":
                    lock (this.Callers)
                    {
                        if (this.Callers.Contains(e.Sender.ToLower()))
                        {
                            e.Authorized = true;
                            this.OnCommand(bot, e);
                        }
                    }
                    break;
            }
        }

        private void Events_UserJoinChannelEvent(BotShell bot, UserJoinChannelArgs e)
        {
            lock (this.Shadowbreeds)
                if (!this.Shadowbreeds.ContainsKey(e.Sender.ToLower()))
                    this.Shadowbreeds.Add(e.Sender.ToLower(), ShadowbreedState.Unknown);

            WhoisResult whois = XML.GetWhois(e.Sender, bot.Dimension);
            if (whois == null || !whois.Success)
                return;

            lock (this.Shadowbreeds)
                if (whois.Stats.Level >= 205)
                    this.Shadowbreeds[e.Sender.ToLower()] = ShadowbreedState.Unknown;
                else
                    this.Shadowbreeds[e.Sender.ToLower()] = ShadowbreedState.Unavailable;
        }

        private void Events_UserLeaveChannelEvent(BotShell bot, UserLeaveChannelArgs e)
        {
            lock (this.Shadowbreeds)
                if (this.Shadowbreeds.ContainsKey(e.Sender.ToLower()))
                    this.Shadowbreeds.Remove(e.Sender.ToLower());
            lock (this.Callers)
                if (this.Callers.Contains(e.Sender.ToLower()))
                {
                    this.Callers.Remove(e.Sender.ToLower());
                    bot.SendPrivateChannelMessage(bot.ColorHighlight + "Removed " + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " from the callers list");
                }
        }

        private void OnCallersCommand(BotShell bot, CommandArgs e)
        {
            if (this.Callers.Count < 1)
            {
                bot.SendReply(e, "There are no assigned callers");
                return;
            }
            RichTextWindow callersWindow = new RichTextWindow(bot);
            callersWindow.AppendTitle("Callers");
            string assistAll = string.Empty;
            lock (this.Callers)
            {
                foreach (string caller in this.Callers)
                {
                    callersWindow.AppendHighlight(Format.UppercaseFirst(caller));
                    callersWindow.AppendNormalStart();
                    callersWindow.AppendString(" [");
                    callersWindow.AppendCommand("Assist", "/assist " + caller);
                    callersWindow.AppendString("] [");
                    callersWindow.AppendCommand("Macro", "/macro " + caller + " /assist " + caller);
                    callersWindow.AppendString("] [");
                    callersWindow.AppendBotCommand("Remove", "callers remove " + caller);
                    callersWindow.AppendString("]");
                    callersWindow.AppendColorEnd();
                    callersWindow.AppendLineBreak();
                    assistAll += "/assist " + caller + "\\n ";
                }
            }
            callersWindow.AppendLineBreak();
            callersWindow.AppendHeader("Options");
            callersWindow.AppendHighlight("Assist All: ");
            callersWindow.AppendCommand("Click", assistAll.Substring(0, assistAll.Length - 3));
            callersWindow.AppendLineBreak();
            callersWindow.AppendHighlight("Assist All Macro: ");
            callersWindow.AppendNormal("/macro assist " + assistAll.Substring(0, assistAll.Length - 3));
            callersWindow.AppendLineBreak();
            callersWindow.AppendHighlight("Clear List: ");
            callersWindow.AppendBotCommand("Click", "callers remove all");
            bot.SendReply(e, "Callers »» ", callersWindow);
        }

        private void OnSbCommand(BotShell bot, CommandArgs e)
        {
            List<string> atrox = new List<string>();
            List<string> nanomage = new List<string>();
            List<string> opifex = new List<string>();
            List<string> solitus = new List<string>();

            foreach (KeyValuePair<UInt32, Friend> kvp in bot.PrivateChannel.List())
            {
                WhoisResult whois = XML.GetWhois(kvp.Value.User, bot.Dimension);
                if (whois == null || whois.Stats == null || whois.Stats.Breed == null)
                    continue;

                lock (this.Shadowbreeds)
                    if (!this.Shadowbreeds.ContainsKey(kvp.Value.User.ToLower()))
                        if (whois.Stats.Level >= 205)
                            this.Shadowbreeds.Add(kvp.Value.User.ToLower(), ShadowbreedState.Unknown);
                        else
                            this.Shadowbreeds.Add(kvp.Value.User.ToLower(), ShadowbreedState.Unavailable);

                switch (whois.Stats.Breed.ToLower())
                {
                    case "atrox":
                        atrox.Add(whois.Name.Nickname.ToLower());
                        break;
                    case "nano":
                        nanomage.Add(whois.Name.Nickname.ToLower());
                        break;
                    case "opifex":
                        opifex.Add(whois.Name.Nickname.ToLower());
                        break;
                    case "solitus":
                        solitus.Add(whois.Name.Nickname.ToLower());
                        break;
                }
            }

            lock (this.Shadowbreeds)
            {
                List<string> remove = new List<string>();
                foreach (KeyValuePair<string, ShadowbreedState> shadowbreed in this.Shadowbreeds)
                {
                    if (atrox.Contains(shadowbreed.Key.ToLower()))
                        continue;
                    if (nanomage.Contains(shadowbreed.Key.ToLower()))
                        continue;
                    if (opifex.Contains(shadowbreed.Key.ToLower()))
                        continue;
                    if (solitus.Contains(shadowbreed.Key.ToLower()))
                        continue;
                    remove.Add(shadowbreed.Key);
                }
                foreach (string shadowbreed in remove)
                    this.Shadowbreeds.Remove(shadowbreed);

                if (this.Shadowbreeds.Count < 1)
                {
                    bot.SendReply(e, "The Shadowbreeds list is empty");
                    return;
                }
            }

            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            if (atrox.Count > 0)
            {
                window.AppendHeader("Atrox");
                this.CreateSbList(bot, ref window, atrox);
                window.AppendLineBreak();
            }
            if (nanomage.Count > 0)
            {
                window.AppendHeader("Nanomage");
                this.CreateSbList(bot, ref window, nanomage);
                window.AppendLineBreak();
            }
            if (opifex.Count > 0)
            {
                window.AppendHeader("Opifex");
                this.CreateSbList(bot, ref window, opifex);
                window.AppendLineBreak();
            }
            if (solitus.Count > 0)
            {
                window.AppendHeader("Solitus");
                this.CreateSbList(bot, ref window, solitus);
                window.AppendLineBreak();
            }

            window.AppendHeader("Options");
            window.AppendBotCommand("Request Shadowbreed State", "sb get");
            window.AppendLineBreak();
            window.AppendBotCommand("Reset Shadowbreed List", "sb reset");

            bot.SendReply(e, "Shadowbreeds »» ", window);
        }

        public void CreateSbList(BotShell bot, ref RichTextWindow window, List<string> users)
        {
            users.Sort();
            RichTextWindow up = new RichTextWindow(bot);
            RichTextWindow unknown = new RichTextWindow(bot);
            RichTextWindow down = new RichTextWindow(bot);
            foreach (string user in users)
            {
                ShadowbreedState state = ShadowbreedState.Unknown;
                lock (this.Shadowbreeds)
                    if (this.Shadowbreeds.ContainsKey(user))
                        state = this.Shadowbreeds[user];

                RichTextWindow tmp;
                switch (state)
                {
                    case ShadowbreedState.Up:
                        tmp = up;
                        break;
                    case ShadowbreedState.Unknown:
                        tmp = unknown;
                        break;
                    default:
                        tmp = down;
                        break;
                }

                tmp.AppendHighlight(Format.UppercaseFirst(user) + " ");
                tmp.AppendNormalStart();
                tmp.AppendString("(");
                tmp.AppendRawString(this.ColorizeState(state));
                tmp.AppendString(") ");
                if (state != ShadowbreedState.Up)
                {
                    tmp.AppendString("[");
                    tmp.AppendBotCommand("Up", "sb set " + user + " up");
                    tmp.AppendString("] ");
                }
                if (state != ShadowbreedState.Down)
                {
                    tmp.AppendString("[");
                    tmp.AppendBotCommand("Down", "sb set " + user + " down");
                    tmp.AppendString("] ");
                }
                if (state != ShadowbreedState.Unavailable)
                {
                    tmp.AppendString("[");
                    tmp.AppendBotCommand("Unavailable", "sb set " + user + " unavailable");
                    tmp.AppendString("] ");
                }
                tmp.AppendColorEnd();
                tmp.AppendLineBreak();
            }
            window.AppendRawString(up.Text);
            window.AppendRawString(unknown.Text);
            window.AppendRawString(down.Text);
        }

        private void RequestSbState(BotShell bot, string user)
        {
            ShadowbreedState state;
            lock (this.Shadowbreeds)
            {
                if (!this.Shadowbreeds.ContainsKey(user.ToLower()))
                    return;
                state = this.Shadowbreeds[user.ToLower()];
            }

            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Shadowbreed");

            window.AppendHighlight("Intro");
            window.AppendLineBreak();
            window.AppendNormal("You have been requested to set your Shadowbreed state.");
            window.AppendLineBreak();
            window.AppendNormal("Your current Shadowbreed state is: ");
            window.AppendRawString(this.ColorizeState(state));
            window.AppendLineBreak();
            window.AppendNormal("Please select one of the following states that best suits your situation.");
            window.AppendLineBreak(2);

            window.AppendHighlight("Up ");
            window.AppendNormal("[");
            window.AppendBotCommand("Set", "sb set " + user + " up");
            window.AppendNormal("] ");
            window.AppendLineBreak();
            window.AppendNormal("Your Shadowbreed is up and available to use.");
            window.AppendLineBreak();
            window.AppendNormal("You're alive and present at the raid.");
            window.AppendLineBreak(2);

            window.AppendHighlight("Down ");
            window.AppendNormal("[");
            window.AppendBotCommand("Set", "sb set " + user + " down");
            window.AppendNormal("] ");
            window.AppendLineBreak();
            window.AppendNormal("Your Shadowbreed is not up and can't be used.");
            window.AppendLineBreak();
            window.AppendNormal("You're alive and present at the raid.");
            window.AppendLineBreak(2);

            window.AppendHighlight("Unavailable ");
            window.AppendNormal("[");
            window.AppendBotCommand("Set", "sb set " + user + " unavailable");
            window.AppendNormal("] ");
            window.AppendLineBreak();
            window.AppendNormal("You don't have a Shadowbreed.");
            window.AppendLineBreak();
            window.AppendNormal("Or you're not present at the raid.");

            bot.SendPrivateMessage(user, bot.ColorHighlight + "Please set your Shadowbreed state »» " + window.ToString(), AoLib.Net.PacketQueue.Priority.Urgent, true);
        }

        private string ColorizeState(ShadowbreedState state)
        {
            switch (state)
            {
                case ShadowbreedState.Up:
                    return HTML.CreateColorString(RichTextWindow.ColorGreen, state.ToString());
                case ShadowbreedState.Unknown:
                    return HTML.CreateColorString(RichTextWindow.ColorOrange, state.ToString());
                default:
                    return HTML.CreateColorString(RichTextWindow.ColorRed, state.ToString());
            }
        }

        private void SendMessage(BotShell bot, string target, string message)
        {
            if (target == "gc")
                bot.SendOrganizationMessage(message);
            if (target == "pg")
                bot.SendPrivateChannelMessage(message);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "callers":
                    return "Displays a list of the currently assigned callers.\n" +
                        "Usage: /tell " + bot.Character + " callers";
                case "callers add":
                    return "Adds a user to the caller list.\n" +
                        "Usage: /tell " + bot.Character + " callers add [username]";
                case "callers remove":
                    return "Removes a user to the caller list.\n" +
                        "Usage: /tell " + bot.Character + " callers remove [username]";
                case "assist":
                    return "Creates an assist macro which you can place on your hotbar.\n" +
                        "Usage: /tell " + bot.Character + " assist [username]";
                case "target":
                    return "Displays a colored message in the private channel ordering everyone to assist you and kill the target.\n" +
                        "Usage: /tell " + bot.Character + " target [target]";
                case "order":
                    return "Displays a colored message in the private channel with the specified order.\n" +
                        "Usage: /tell " + bot.Character + " order [order]";
                case "command":
                    return "Displays a colored and multi-lined message in the private channel with the specified command.\n" +
                        "Usage: /tell " + bot.Character + " command [command]";
                case "cd":
                    return "Counts down from 5 to 0 in the private channel.\n" +
                        "If a command is specified it will display that command on the count of 3.\n" +
                        "Usage: /tell " + bot.Character + " cd [[command]]";
                case "sb":
                    return "Displays an interface for managing shadowbreeds of everyone on the private channel.\n" +
                        "Usage: /tell " + bot.Character + " sb";
            }
            return null;
        }
    }
}
