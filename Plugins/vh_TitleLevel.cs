using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class Vhtitlelevel : PluginBase
    {
        public Vhtitlelevel()
        {
            this.Name = "TitleLevels";
            this.InternalName = "vhTitleLevel";
            this.Author = "Kevma";
            this.Version = 100;
            this.Description = "Gives information on TitleLevel IP.";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("titlelevel", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest),
                new Command("titles", "titlelevel"),
                new Command("tl", "titlelevel")
            };
        }

        public override void OnLoad(BotShell bot)
        {
        }

        public override void OnUnload(BotShell bot)
        {
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length == 0)
            {
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("TitleLevel IP");
                window.AppendLineBreak();
                window.AppendNormal("1. Level 1-14 - 4000 IP (Level 1 = 1500 IP)");
                window.AppendLineBreak();
                window.AppendNormal("2. Level 15-49 - 10,000 IP");
                window.AppendLineBreak();
                window.AppendNormal("3. Level 50-99 - 20,000 IP");
                window.AppendLineBreak();
                window.AppendNormal("4. Level 100-149 - 40,000 IP");
                window.AppendLineBreak();
                window.AppendNormal("5. Level 150-189 - 80,000 IP");
                window.AppendLineBreak();
                window.AppendNormal("6. Level 190-204 - 150,000 IP (160,000 at level 200)");
                window.AppendLineBreak();
                window.AppendNormal("7. Level 205-220 - 600,000 IP");

                bot.SendReply(e, "TitleLevels »» ", window);
            }
            else
            {
                switch (e.Args[0])
                {
                    case "1":
                        bot.SendReply(e, "Level 1-14 - 4000 IP (Level 1 = 1500 IP)");
                        break;
                    case "2":
                        bot.SendReply(e, "Level 15-49 - 10,000 IP");
                        break;
                    case "3":
                        bot.SendReply(e, "Level 50-99 - 20,000 IP");
                        break;
                    case "4":
                        bot.SendReply(e, "Level 100-149 - 40,000 IP");
                        break;
                    case "5":
                        bot.SendReply(e, "Level 150-189 - 80,000 IP");
                        break;
                    case "6":
                        bot.SendReply(e, "Level 190-204 - 150,000 IP (160.000 at level 200)");
                        break;
                    case "7":
                        bot.SendReply(e, "Level 205-220 - 600,000 IP");
                        break;
                    default:
                        bot.SendReply(e, "Correct Usage: tl {[1-8]}");
                        break;
                }
            }
        }


        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "titles":
                    return "titles\n" +
                        "Usage: /tell " + bot.Character + " To view the titlelevels type !titlelevel";
            }
            return null;
        }
    }
}
