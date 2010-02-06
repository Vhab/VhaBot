using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;
using System.Diagnostics;

namespace VhaBot.Plugins
{
    public class Version : PluginBase
    {
        public Version()
        {
            this.Name = "Version and Statistics";
            this.InternalName = "vhVersion";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Core;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("version", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }
        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            // Version Information
            window.AppendTitle("Version Information");

            window.AppendHighlight("Core Version: ");
            window.AppendNormal(BotShell.VERSION + " " + BotShell.BRANCH + " (Build: " + BotShell.BUILD + ")");
            window.AppendLineBreak();

            window.AppendHighlight("AoLib Version: ");
            window.AppendNormal(AoLib.Net.Chat.VERSION + " (Build: " + AoLib.Net.Chat.BUILD + ")");
            window.AppendLineBreak();

            window.AppendHighlight("CLR Version: ");
            window.AppendNormal(Environment.Version.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Bot Owner: ");
            window.AppendNormal(bot.Admin);
            window.AppendLineBreak(2);

            // Credits
            window.AppendHeader("Credits");
            window.AppendHighlight("*Helpbot Developers*");
            window.AppendLineBreak();
            window.AppendNormal("- Naturalistic (rk1)");
            window.AppendLineBreak();
            window.AppendNormal("- Iriche (rk1)");
            window.AppendLineBreak();
            window.AppendNormal("- Toxor (rk2) (aka Demerzel)");
            window.AppendLineBreak();
            window.AppendHighlight("*Core Developer*");
            window.AppendLineBreak();
            window.AppendNormal("- Vhab (rk1)");
            window.AppendLineBreak();
            window.AppendHighlight("*Vhabot Developers*");
            window.AppendLineBreak();
            window.AppendNormal("- Naturalistic (rk1)"); 
            window.AppendLineBreak();
            window.AppendNormal("- Iriche (rk1)");
            window.AppendLineBreak();
            window.AppendNormal("- Toxor (rk2) (aka Demerzel)");
            window.AppendLineBreak();
            window.AppendNormal("- Tsuyoi (rk1)");
            window.AppendLineBreak();
            window.AppendHighlight("*Contributors*");
            window.AppendLineBreak();
            window.AppendNormal("- Telperion (rk1)");
            window.AppendLineBreak();
            window.AppendNormal("- Moepl (rk1)");
            window.AppendLineBreak();
            window.AppendNormal("- Fayelure (rk1)");
            window.AppendLineBreak();
            window.AppendNormal("- Neksus (rk2)");
            window.AppendLineBreak(2);

            // Statistics
            window.AppendHeader("Statistics");
            window.AppendHighlight("Bot Uptime: ");
            window.AppendNormal(string.Format("{0} Days, {1} Hours, {2} Minutes, {3} Seconds", Math.Floor(bot.Stats.Uptime.TotalDays), bot.Stats.Uptime.Hours, bot.Stats.Uptime.Minutes, bot.Stats.Uptime.Seconds));
            window.AppendLineBreak();

            // Mono doesn't support performance counters
            Type mono = Type.GetType("Mono.Runtime");
            if (mono == null)
            {
                try
                {
                    PerformanceCounter pc = new PerformanceCounter("System", "System Up Time");
                    pc.NextValue();
                    TimeSpan ts = TimeSpan.FromSeconds(pc.NextValue());
                    window.AppendHighlight("Host Uptime: ");
                    window.AppendNormal(string.Format("{0} Days, {1} Hours, {2} Minutes, {3} Seconds", Math.Floor(ts.TotalDays), ts.Hours, ts.Minutes, ts.Seconds));
                    window.AppendLineBreak();
                }
                catch { }
            }

            window.AppendHighlight("Host OS: ");
            window.AppendNormal(Environment.OSVersion.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Commands Processed: ");
            window.AppendNormal(bot.Stats.Counter_Commands.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Private Messages Received: ");
            window.AppendNormal(bot.Stats.Counter_PrivateMessages_Received.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Channel Messages Received: ");
            window.AppendNormal(bot.Stats.Counter_ChannelMessages_Received.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Private Channel Messages Received: ");
            window.AppendNormal(bot.Stats.Counter_PrivateChannelMessages_Received.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Private Messages Sent: ");
            window.AppendNormal(bot.Stats.Counter_PrivateMessages_Sent.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Channel Messages Sent: ");
            window.AppendNormal(bot.Stats.Counter_ChannelMessages_Sent.ToString());
            window.AppendLineBreak();

            window.AppendHighlight("Private Channel Messages Sent: ");
            window.AppendNormal(bot.Stats.Counter_PrivateChannelMessages_Sent.ToString());
            window.AppendLineBreak();

            window.AppendLineBreak();
            window.AppendHeader("Links");
            window.AppendCommand("VhaBot Central", "/start http://www.vhabot.net/");
            window.AppendLineBreak();
            window.AppendCommand("VhaBot Forums", "/start http://forums.vhabot.net/");
            window.AppendLineBreak();
            window.AppendCommand("VhaBot Characters Database", "/start http://characters.vhabot.net/");
            window.AppendLineBreak();
            window.AppendCommand("VhaBot Tools", "/start http://tools.vhabot.net/");

            bot.SendReply(e, "VhaBot " + BotShell.VERSION + " " + BotShell.BRANCH + " - " + BotShell.EDITION + " Edition »» " + window.ToString("More Information"));
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "version":
                    return "Displays the bot version, credits and other statistics.\n" +
                        "Usage: /tell " + bot.Character + " version";
            }
            return null;
        }
    }
}
