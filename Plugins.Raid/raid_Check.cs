using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;
using VhaBot;
using MySql.Data.MySqlClient;

namespace VhaBot.Plugins
{
    public class RaidCheck : PluginBase
    {
        private RaidCore _core;

        public RaidCheck()
        {
            this.Name = "Raid :: Check";
            this.InternalName = "RaidCheck";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidCore" };
            this.Description = "Allows you to check the presence of all active members on the raid";
            this.Commands = new Command[] {
                new Command("raid check", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            RaidCore.Raider[] raiders = this._core.GetActiveRaiders();
            int i = 0;
            int ii = 0;
            string assist = string.Empty;
            if (raiders.Length == 0)
            {
                bot.SendReply(e, "There's nobody on the raid to check");
                return;
            }
            foreach (RaidCore.Raider raider in raiders)
            {
                assist += "/assist " + raider.Character + " \\n ";
                i++;
                ii++;
                if (i == 100 || raiders.Length == ii)
                {
                    RichTextWindow window = new RichTextWindow(bot);
                    window.AppendTitle("Raid Check");
                    window.AppendHighlight("This command will execute a /assist command on all raiders currently present in the raid");
                    window.AppendLineBreak();
                    window.AppendHighlight("If the player is present it won't display anything if they are in combat and if they're not in combat it will display 'Target is not in a fight'");
                    window.AppendLineBreak();
                    window.AppendHighlight("If the player isn't present it will display 'Can't find target >player<'");
                    window.AppendLineBreak(2);
                    window.AppendNormal("[");
                    window.AppendCommand("Check all Raiders", assist);
                    window.AppendNormal("]");
                    i = 0;
                    assist = string.Empty;
                    bot.SendReply(e, "Raid Check »» ", window);
                }
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "raid check":
                    return "This command will execute a /assist command on all raiders currently present in the raid.\n" +
                        "If the player is present it won't display anything if they are in combat and if they're not in combat it will display 'Target is not in a fight'\n" +
                        "If the player isn't present it will display 'Can't find target >player<'\n" +
                        "Usage: /tell " + bot.Character + " raid check";
            }
            return null;
        }
    }
}
