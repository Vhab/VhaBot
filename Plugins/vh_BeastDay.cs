using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Globalization;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class VhBeastDay : PluginBase
    {
        public VhBeastDay()
        {
            this.Name = "RK1 Beast Day";
            this.InternalName = "vhBeastDay";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;

            this.Commands = new Command[] {
                new Command("beastday", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }
        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            Int32 cycle = 7 + 7 + 7;
            string today = string.Empty;
            DateTimeFormatInfo dtfi = new CultureInfo("en-US", false).DateTimeFormat;
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Beast Days");

            for (int i = 0; i < cycle; i++)
            {
                Int64 current = TimeStamp.Now + ((24 * 60 * 60) * i);
                Int64 secondsSinceStart = current - TimeStamp.FromDateTime(new DateTime(2006, 10, 23));
                Int64 seconds = secondsSinceStart % (cycle * (24 * 60 * 60));
                Int64 day = (seconds / (24 * 60 * 60)) + 1;

                string type = string.Empty;
                if (day <= 7)
                    type = HTML.CreateColorString(RichTextWindow.ColorBlue, "Omni");
                else if (day > 7 && day <= 14)
                    type = HTML.CreateColorString(RichTextWindow.ColorGreen, "FFA");
                else if (day > 14)
                    type = HTML.CreateColorString(RichTextWindow.ColorOrange, "Clan");

                if (type == string.Empty)
                    continue;

                if (i == 0)
                    today = type;

                window.AppendHighlight(TimeStamp.ToDateTime(current).ToString("[dd/MM/yyyy]", dtfi));
                window.AppendNormal(TimeStamp.ToDateTime(current).ToString(" dddd: ", dtfi));
                window.AppendRawString(type);
                window.AppendLineBreak();
            }

            bot.SendReply(e, "Today is " + today + " day »» " + window.ToString());
        }
        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "beastday":
                    return "Displays to which faction the current Beast spawn belongs.\n" +
                        "This plugin assumes the 7/7/7 agreement on RK1 starting on 2006/10/23.\n" +
                        "The order of the current rotation is: Omni, FFA, Clan.\n" +
                        "Usage: /tell " + bot.Character + " beastday";
            }
            return null;
        }
    }
}