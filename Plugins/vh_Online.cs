using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class OnlineBase : PluginBase
    {
        private bool IncludeNotifyList = true;
        private bool IncludePrivateChannel = true;
        private bool SeperateSections = false;
        private string DisplayOrganization = "Never";
        private bool DisplayRanks = true;
        private string DisplayMode = "Profession";
        private string DisplayHeaders = "Text";
        private bool DisplayAfk = true;
        private bool DisplayAlts = true;
        private bool SendLogon = false;
        private bool AutoAFK = false;
        private Dictionary<string, string> Afk;
        private Dictionary<string, int> Icons; 

        public OnlineBase()
        {
            this.Name = "Online Tracker";
            this.InternalName = "vhOnline";
            this.Author = "Vhab";
            this.Version = 100;
            this.Description = "Provides an interface to the user to view who is online and/or on the private channel. Profession Icon modification ported from Akarah's BeBot module.";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("online", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest),
                new Command("afk", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "include_notify", "Include Notify List", this.IncludeNotifyList);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "include_pg", "Include Private Channel", this.IncludePrivateChannel);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "seperate_sections", "Seperate Sections", this.SeperateSections);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "display_org", "Display Organization", this.DisplayOrganization, "Never", "Foreign", "Always");
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "display_ranks", "Display Ranks", this.DisplayRanks);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "display_mode", "Display Mode", this.DisplayMode, "Profession", "Alphabetical", "Level");
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "display_afk", "Display AFK", this.DisplayAfk);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "display_alts", "Display Alts", this.DisplayAlts);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "display_headers", "Display Sub-Headers", this.DisplayHeaders, "Off", "Text", "Icons");
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "send_logon", "Send Online on Logon", this.SendLogon);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "auto_afk", "Set user afk on Channel Message", this.AutoAFK);

            this.IncludeNotifyList = bot.Configuration.GetBoolean(this.InternalName, "include_notify", this.IncludeNotifyList);
            this.IncludePrivateChannel = bot.Configuration.GetBoolean(this.InternalName, "include_pg", this.IncludePrivateChannel);
            this.SeperateSections = bot.Configuration.GetBoolean(this.InternalName, "seperate_sections", this.SeperateSections);
            this.DisplayOrganization = bot.Configuration.GetString(this.InternalName, "display_org", this.DisplayOrganization);
            this.DisplayRanks = bot.Configuration.GetBoolean(this.InternalName, "display_ranks", this.DisplayRanks);
            this.DisplayMode = bot.Configuration.GetString(this.InternalName, "display_mode", this.DisplayMode);
            this.DisplayAfk = bot.Configuration.GetBoolean(this.InternalName, "display_afk", this.DisplayAfk);
            this.DisplayAlts = bot.Configuration.GetBoolean(this.InternalName, "display_alts", this.DisplayAlts);
            this.DisplayHeaders = bot.Configuration.GetString(this.InternalName, "display_headers", this.DisplayHeaders);
            this.SendLogon = bot.Configuration.GetBoolean(this.InternalName, "send_logon", this.SendLogon);
            this.AutoAFK = bot.Configuration.GetBoolean(this.InternalName, "auto_afk", this.AutoAFK);

            this.Afk = new Dictionary<string, string>();
            loadIcons();

            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
            
            bot.Events.ChannelMessageEvent += new ChannelMessageHandler(Events_ChannelMessageEvent);
            bot.Events.PrivateChannelMessageEvent += new PrivateChannelMessageHandler(Events_PrivateChannelMessageEvent);
            bot.Events.UserLogonEvent += new UserLogonHandler(Events_UserLogonEvent);
            bot.Events.BotStateChangedEvent += new BotStateChangedHandler(Events_BotStateChangedEvent);
        }

        void Events_BotStateChangedEvent(BotShell bot, BotStateChangedArgs e)
        {
            if (e.State==0)
            {
                bot.SendOrganizationMessage(bot.ColorHighlight + bot.Character + " has connected!");
            }
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }

        private void Events_ChannelMessageEvent(BotShell bot, ChannelMessageArgs e)
        {
            if (this.AutoAFK)
            {
                if (e.Message.ToLower().StartsWith("afk"))
                {
                    lock (this.Afk)
                    {
                        if (this.Afk.ContainsKey(e.Sender))
                        {
                            this.Afk.Remove(e.Sender);
                            bot.SendOrganizationMessage(bot.ColorHighlight + e.Sender + " is back");
                        }
                        else
                        {
                            this.Afk.Add(e.Sender, "AFK");
                            bot.SendOrganizationMessage(bot.ColorHighlight + e.Sender + " is afk.");
                        }
                    }
                    return;
                }
            }
            if (e.Command) return;
            this.RemoveAfk(bot, e.Sender);
        }

        private void Events_PrivateChannelMessageEvent(BotShell bot, PrivateChannelMessageArgs e)
        {
            if (this.AutoAFK)
            {
                if (e.Message.ToLower().StartsWith("afk"))
                {
                    lock (this.Afk)
                    {
                        if (this.Afk.ContainsKey(e.Sender))
                        {
                            this.Afk.Remove(e.Sender);
                            bot.SendPrivateChannelMessage(bot.ColorHighlight + e.Sender + " is back");
                        }
                        else
                        {
                            this.Afk.Add(e.Sender, "AFK");
                            bot.SendPrivateChannelMessage(bot.ColorHighlight + e.Sender + " is afk.");
                        }
                    }
                    return;
                }
            }
            if (e.Command) return;
            this.RemoveAfk(bot, e.Sender);
        }

        private void Events_UserLogonEvent(BotShell bot, UserLogonArgs e)
        {
            if (this.SendLogon && e.Sections.Contains("notify"))
            {
                // Fake the user sending !online to the bot ;)
                CommandArgs args = new CommandArgs(CommandType.Tell, 0, e.SenderID, e.Sender, e.SenderWhois, "online", "", false, null);
                this.OnOnlineCommand(bot, args);
            }
        }

        private void RemoveAfk(BotShell bot, string user)
        {
            lock (this.Afk)
            {
                if (!this.Afk.ContainsKey(user)) return;
                this.Afk.Remove(user);
                bot.SendPrivateChannelMessage(bot.ColorHighlight + user + " is back");
                bot.SendOrganizationMessage(bot.ColorHighlight + user + " is back");
            }
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section.Equals(this.InternalName, StringComparison.CurrentCultureIgnoreCase))
                switch (e.Key.ToLower())
                {
                    case "include_notify":
                        this.IncludeNotifyList = (bool)e.Value;
                        break;
                    case "include_pg":
                        this.IncludePrivateChannel = (bool)e.Value;
                        break;
                    case "seperate_sections":
                        this.SeperateSections = (bool)e.Value;
                        break;
                    case "display_org":
                        this.DisplayOrganization = (string)e.Value;
                        break;
                    case "display_ranks":
                        this.DisplayRanks = (bool)e.Value;
                        break;
                    case "display_mode":
                        this.DisplayMode = (string)e.Value;
                        break;
                    case "display_afk":
                        this.DisplayAfk = (bool)e.Value;
                        break;
                    case "display_alts":
                        this.DisplayAlts = (bool)e.Value;
                        break;
                    case "display_headers":
                        this.DisplayHeaders = (string)e.Value;
                        break;
                    case "send_logon":
                        this.SendLogon = (bool)e.Value;
                        break;
                    case "auto_afk":
                        this.AutoAFK = (bool)e.Value;
                        break;
                }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "online":
                    this.OnOnlineCommand(bot, e);
                    break;
                case "afk":
                    this.OnAfkCommand(bot, e);
                    break;
            }
        }

        public void OnOnlineCommand(BotShell bot, CommandArgs e)
        {
            List<string> titles = new List<string>();
            List<RichTextWindow> windows = new List<RichTextWindow>();
            string args = string.Empty;
            if (e.Args.Length > 0)
                args = e.Args[0];

            // Notify List
            if (this.IncludeNotifyList)
            {
                string[] online = bot.FriendList.Online("notify");
                if (online.Length > 0)
                {
                    RichTextWindow notifyWindow = new RichTextWindow(bot);
                    notifyWindow.AppendHeader(online.Length + " Users Online");
                    int results = 0;
                    this.BuildList(bot, online, ref notifyWindow, ref results, args);
                    if (results > 0)
                    {
                        titles.Add(HTML.CreateColorString(bot.ColorHeaderHex, results.ToString()) + " Users Online");
                        windows.Add(notifyWindow);
                    }
                }
            }

            // Private Channel
            if (this.IncludePrivateChannel)
            {
                Dictionary<UInt32, Friend> list = bot.PrivateChannel.List();
                List<string> guests = new List<string>();
                foreach (KeyValuePair<UInt32, Friend> user in list)
                    guests.Add(user.Value.User);
                if (guests.Count > 0)
                {
                    RichTextWindow guestsWindow = new RichTextWindow(bot);
                    guestsWindow.AppendHeader(guests.Count + " Users on the Private Channel");
                    int results = 0;
                    this.BuildList(bot, guests.ToArray(), ref guestsWindow, ref results, args);
                    if (results > 0)
                    {
                        titles.Add(HTML.CreateColorString(bot.ColorHeaderHex, results.ToString()) + " Users on the Private Channel");
                        windows.Add(guestsWindow);
                    }
                }
            }

            // Output
            if (titles.Count < 1)
            {
                bot.SendReply(e, "The are currently no users online");
                return;
            }
            if (SeperateSections)
            {
                for (int i = 0; i < windows.Count; i++)
                {
                    RichTextWindow window = new RichTextWindow(bot);
                    window.AppendTitle();
                    window.AppendRawString(windows[i].Text);
                    bot.SendReply(e, titles[i] + " »» ", window);
                }
            }
            else
            {
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle();
                foreach (RichTextWindow subWindow in windows)
                    window.AppendRawString(subWindow.Text);
                bot.SendReply(e, string.Join(", ", titles.ToArray()) + " »» ", window);
            }
        }

        public void OnAfkCommand(BotShell bot, CommandArgs e)
        {
            lock (this.Afk)
            {
                if (this.Afk.ContainsKey(e.Sender))
                {
                    this.Afk.Remove(e.Sender);
                    bot.SendReply(e, "You are no longer marked AFK");
                    return;
                }
            }
            string afkmsg = "";
            if (e.Args.Length < 1)
            {
                afkmsg = "AFK";
            }
            else
                afkmsg = e.Words[0];
            lock (this.Afk)
            {
                this.Afk.Add(e.Sender, afkmsg);
            }
            bot.SendReply(e, "You are now AFK");
        }

        public void BuildList(BotShell bot, string[] users, ref RichTextWindow window) { int results = 0; this.BuildList(bot, users, ref window, ref results, string.Empty); }
        public void BuildList(BotShell bot, string[] users, ref RichTextWindow window, ref int results, string profs)
        {
            if (window == null)
                return;
            if (users.Length == 0)
                return;

            SortedDictionary<string, SortedDictionary<string, WhoisResult>> list = new SortedDictionary<string, SortedDictionary<string, WhoisResult>>();
            Dictionary<string, WhoisResult> whoisResults = new Dictionary<string, WhoisResult>();
            foreach (string user in users)
            {
                if (user == null || user == string.Empty)
                    continue;

                string header;
                WhoisResult whois = XML.GetWhois(user, bot.Dimension);
                if (profs != null && profs != string.Empty)
                {
                    if (whois == null || whois.Stats == null)
                        continue;
                    if (!whois.Stats.Profession.StartsWith(profs, StringComparison.CurrentCultureIgnoreCase))
                        continue;
                }
                if (whois != null && whois.Stats != null)
                {
                    if (this.DisplayMode == "Level")
                        header = whois.Stats.Level.ToString();
                    else
                        header = whois.Stats.Profession;

                    if (!whoisResults.ContainsKey(user.ToLower()))
                        whoisResults.Add(user.ToLower(), whois);
                }
                else
                    if (this.DisplayMode == "Level")
                        header = "0";
                    else
                        header = "Unknown";

                if (this.DisplayMode == "Alphabetical")
                    header = user.ToUpper().ToCharArray()[0].ToString();

                if (!list.ContainsKey(header))
                    list.Add(header, new SortedDictionary<string, WhoisResult>());
                list[header].Add(user, whois);
            }
            results = 0;
            foreach (KeyValuePair<string, SortedDictionary<string, WhoisResult>> prof in list)
            {
                if (this.DisplayHeaders == "Text")
                {
                    window.AppendHighlight(prof.Key);
                    window.AppendLineBreak();
                }
                else if (this.DisplayHeaders == "Icons")
                {
                    window.AppendImage("GFX_GUI_FRIENDLIST_SPLITTER");
                    window.AppendLineBreak();
                    window.AppendIcon(Icons[prof.Key]);
                    window.AppendHighlight(prof.Key);
                    window.AppendLineBreak();
                    window.AppendImage("GFX_GUI_FRIENDLIST_SPLITTER");
                    window.AppendLineBreak();
                }
                foreach (KeyValuePair<string, WhoisResult> user in prof.Value)
                {
                    // Name
                    window.AppendNormalStart();
                    window.AppendString("   ");
                    int level = 0;
                    int ailevel = 0;
                    string name = Format.UppercaseFirst(user.Key);

                    // Level
                    if (user.Value != null && user.Value.Stats != null)
                    {
                        level = user.Value.Stats.Level;
                        ailevel = user.Value.Stats.DefenderLevel;
                    }
                    if (level < 10)
                        window.AppendColorString("000000", "00");
                    else if (level < 100)
                        window.AppendColorString("000000", "0");
                    window.AppendString(level + " ");

                    if (ailevel < 10)
                        window.AppendColorString("000000", "0");
                    window.AppendColorString(RichTextWindow.ColorGreen, ailevel + " ");
                    window.AppendString(name);

                    // Organization
                    bool displayOrganization = false;
                    if (this.DisplayOrganization == "Always")
                        displayOrganization = true;

                    if (this.DisplayOrganization == "Foreign")
                    {
                        displayOrganization = true;
                        if (bot.InOrganization && whoisResults.ContainsKey(name.ToLower()) && whoisResults[name.ToLower()].InOrganization && bot.Organization == whoisResults[name.ToLower()].Organization.Name)
                            displayOrganization = false;
                    }

                    if (displayOrganization)
                    {
                        if (!whoisResults.ContainsKey(name.ToLower()))
                        {
                            window.AppendString(" (Unknown)");
                        }
                        else if (whoisResults[name.ToLower()].InOrganization)
                        {
                            window.AppendString(" (" + whoisResults[name.ToLower()].Organization.ToString() + ")");
                        }
                        else
                        {
                            window.AppendString(" (Not Guilded)");
                        }
                    }

                    // Rank
                    if (this.DisplayRanks)
                    {
                        UserLevel userLevel = bot.Users.GetUser(name);
                        if (userLevel > UserLevel.Member)
                        {
                            window.AppendString(" (");
                            window.AppendColorString(RichTextWindow.ColorRed, bot.Users.GetUser(name).ToString());
                            window.AppendString(")");
                        }
                    }

                    // Alts
                    if (this.DisplayAlts)
                    {
                        string main = bot.Users.GetMain(name);
                        string[] alts = bot.Users.GetAlts(main);
                        if (alts.Length > 0)
                        {
                            window.AppendString(" :: ");
                            if (main == name)
                                window.AppendBotCommand("Alts", "alts " + main);
                            else
                                window.AppendBotCommand(main + "'s Alts", "alts " + main);
                        }
                    }

                    // Afk
                    if (this.DisplayAfk)
                    {
                        lock (this.Afk)
                        {
                            if (this.Afk.ContainsKey(name))
                            {
                                window.AppendString(" :: ");
                                window.AppendColorString(RichTextWindow.ColorRed, "AFK");
                            }
                        }
                    }
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                    results++;
                }
                if (this.DisplayHeaders == "Text" || this.DisplayHeaders == "Icons")
                    window.AppendLineBreak();
            }
            if (this.DisplayHeaders == "Off")
                window.AppendLineBreak();
        }

        public void loadIcons()
        {
            Icons = new Dictionary<string, int>();
            Icons.Add("Meta-Physicist", 16308);
			Icons.Add("Adventurer", 84203);
			Icons.Add("Engineer", 16252);
			Icons.Add("Soldier", 16237);
			Icons.Add("Keeper", 84197);
			Icons.Add("Shade", 39290);
			Icons.Add("Fixer", 16300);
			Icons.Add("Agent", 16186);
			Icons.Add("Trader", 117993);
			Icons.Add("Doctor", 44235);
			Icons.Add("Enforcer", 100998);
			Icons.Add("Bureaucrat", 16341);
			Icons.Add("Martial Artist", 16196);
            Icons.Add("Nano-Technician", 16283);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "online":
                    return "Displays all users online and/or on the private channel.\nIf [profession] is specified, it will display only people online and/or on the private channel that are the specified profession.\n" +
                        "Usage: /tell " + bot.Character + " online [[profession]]";
                case "afk":
                    return "Sets yourself AFK (Away From Keyboard) on the bot. Reasons are optional.\n" +
                        "Usage: /tell " + bot.Character + " afk [reason]";
            }
            return null;
        }
    }
}
