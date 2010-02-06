using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class VhGateTime : PluginBase
    {
        private string Url = "http://zibby.isa-geek.net/apf.timestamp";
        private Int64 GateTime = 0;
        private int Cycle = ((7 * 60) + 12) * 60; // 7 hours and 12 minutes

        public VhGateTime()
        {
            this.Name = "Outzones Gate Timer";
            this.InternalName = "vhApfTimer";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;

            this.Commands = new Command[] {
                new Command("gates", true, UserLevel.Member),
                new Command("gates get", true, UserLevel.Leader),
                new Command("gates reset", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.String, this.InternalName, "url", "Apf Timer URL", this.Url);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "gatetime", "Current Gate TimeStamp", (Int32)this.GateTime);
            this.LoadSettings(bot);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
            {
                this.LoadSettings(bot);
            }
        }

        private void LoadSettings(BotShell bot)
        {
            this.Url = bot.Configuration.GetString(this.InternalName, "url", this.Url);
            this.GateTime = bot.Configuration.GetInteger(this.InternalName, "gatetime", (Int32)this.GateTime);
            if (this.GateTime < 1)
                this.FetchWebTime(bot);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "gates":
                    TimeSpan time = this.GetTimeLeft();
                    DateTime now = DateTime.Now.ToUniversalTime();
                    RichTextWindow window = new RichTextWindow(bot);
                    window.AppendTitle("Outzone Gates Opening Times");
                    DateTime next = now.AddSeconds(time.TotalSeconds);
                    for (int i = 0; i < 14; i++)
                    {
                        window.AppendHighlight(next.ToString("dd/MM/yyyy HH:mm:ss") + " GMT ");
                        TimeSpan span = next - now;
                        window.AppendNormal(string.Format("({0:00}:{1:00}:{2:00} from now)", Math.Floor(span.TotalHours), span.Minutes, span.Seconds));
                        window.AppendLineBreak();
                        next = next.AddSeconds(this.Cycle);
                    }
                    string result = string.Empty;
                    int last = this.Cycle - (int)time.TotalSeconds;
                    if (last < (60 * 2))
                    {
                        result = "Unicorn Gatekeeper is currently opening the Outzones gates";
                    }
                    else if (last < (60 * 12))
                    {
                        result = "The Outzones gates are currently open";
                    }
                    else
                    {
                        result = "Unicorn Gatekeeper will open the Outzones gates in " + HTML.CreateColorString(bot.ColorHeaderHex, string.Format("{0} hours, {1} minutes and {2} seconds", time.Hours, time.Minutes, time.Seconds));
                    }
                    bot.SendReply(e, result + " »» " + window.ToString());
                    break;
                case "gates get":
                    if (this.FetchWebTime(bot))
                        bot.SendReply(e, "Synchronized the Outzones gates timer");
                    else
                        bot.SendReply(e, "Unable to synchronize the Outzones gates timer. Please try again later");
                    break;
                case "gates reset":
                    bot.Configuration.SetInteger(this.InternalName, "gatetime", (Int32)TimeStamp.Now);
                    this.LoadSettings(bot);
                    bot.SendReply(e, "The Outzones gates timer has been reset");
                    break;
            }
        }

        private bool FetchWebTime(BotShell bot)
        {
            string data = HTML.GetHtml(this.Url);
            if (data == null || data == string.Empty)
                return false;
            string[] parts = data.Split(' ');
            if (parts.Length < 3)
                return false;

            string type = parts[0];
            string[] date = parts[1].Split('-');
            string[] time = parts[2].Split(':');
            if (type != "GATETIME" && type != "UPTIME")
                return false;

            try
            {
                int year = Convert.ToInt32(date[0]);
                int month = Convert.ToInt32(date[1]);
                int day = Convert.ToInt32(date[2]);

                int hour = Convert.ToInt32(time[0]);
                int minute = Convert.ToInt32(time[1]);
                int second = Convert.ToInt32(time[2]);

                DateTime gateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                if (type == "UPTIME")
                    gateTime.AddMinutes(440);

                bot.Configuration.SetInteger(this.InternalName, "gatetime", (Int32)TimeStamp.FromDateTime(gateTime));
                this.LoadSettings(bot);
                return true;
            }
            catch { }
            return false;
        }

        private TimeSpan GetTimeLeft()
        {
            Int64 gateTime = this.GateTime;
            Int64 now = TimeStamp.Now;
            while (gateTime < now)
                gateTime += this.Cycle;

            return TimeStamp.ToDateTime(gateTime) - TimeStamp.ToDateTime(now);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "gates":
                    return "Displays the time untill the Outzones gates are opened.\nAfter the gates have been opened you will have a 10 minutes window to enter.\n" +
                        "Usage: /tell " + bot.Character + " gates";
                case "gates get":
                    return "Allows you to update the time untill the Outzones gates open using an external information source.\nThe external data source is provided by Glarawyn on Atlantean\n" +
                        "Usage: /tell " + bot.Character + " gates get";
                case "gates reset":
                    return "Allows you to reset the time untill the Outzones gates are opened.\nPlease reset the timer at the exact moment the gatekeeper starts opening gates.\n" +
                        "Usage: /tell " + bot.Character + " gates reset";
                }
            return null;
        }
    }
}