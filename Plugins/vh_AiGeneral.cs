using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class VhAigen : PluginBase
    {
        public VhAigen()
        {
            this.Name = "AI Generals Loot";
            this.InternalName = "VhAigen";
            this.Author = "Moepl";
            this.Version = 100;
            this.Description = "Displays information about AI Generals loot.\nThis plugin is a port from the BudaBot 'aigen' module.";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("aigen", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: aigen [ankari/ilari/rimah/jaax/xoch/cha]");
                return;
            }

            string msg = String.Empty;
            string botName = String.Empty;
            string bioName1 = String.Empty;
            string bioName2 = String.Empty;
            string genName = String.Empty;
            string botInfo = String.Empty;
            int bioId1 = 0;
            int bioId2 = 0;
            int botImageId = 100337;
            int bioImageId = 255705;
            int botId = 0;

            switch (e.Args[0])
            {
                case "ankari":
                    genName = "General - Ankari'Khaz";
                    botId = 247145;
                    botName = "Arithmetic Lead Viralbots";
                    bioId1 = 247684;
                    bioName1 = "Kyr'Ozch Bio-Material - Type 1";
                    bioId2 = 247685;
                    bioName2 = "Kyr'Ozch Bio-Material - Type 2";
                    msg = "Low Evade/Dodge,low AR, casting Viral/Virral nukes.";
                    botInfo = "(Nanoskill / Tradeskill)";
                    break;
                case "ilari":
                    genName = "General - Ilari'Khaz";
                    botId = 247146;
                    botName = "Spiritual Lead Viralbots";
                    bioId1 = 247681;
                    bioName1 = "Kyr'Ozch Bio-Material - Type 992";
                    bioId2 = 247679;
                    bioName2 = "Kyr'Ozch Bio-Material - Type 880";
                    msg = "Low Evade/Dodge.";
                    botInfo = "(Nanocost / Nanopool / Max Nano)";
                    break;
                case "rimah":
                    genName = "General - Rimah'Khaz";
                    botId = 247143;
                    botName = "Observant Lead Viralbots";
                    bioId1 = 247675;
                    bioName1 = "Kyr'Ozch Bio-Material - Type 112";
                    bioId2 = 247678;
                    bioName2 = "Kyr'Ozch Bio-Material - Type 240";
                    msg = "Low Evade/Dodge.";
                    botInfo = "(Init / Evades)";
                    break;
                case "jaax":
                    genName = "General - Jaax'Khaz";
                    botId = 247139;
                    botName = "Strong Lead Viralbots";
                    bioId1 = 247694;
                    bioName1 = "Kyr'Ozch Bio-Material - Type 3";
                    bioId2 = 247688;
                    bioName2 = "Kyr'Ozch Bio-Material - Type 4";
                    msg = "High Evade, Low Dodge.";
                    botInfo = "(Melee / Spec Melee / Add All Def / Add Damage)";
                    break;
                case "xoch":
                    genName = "General - Xoch'Khaz";
                    botId = 247137;
                    botName = "Enduring Lead Viralbots";
                    bioId1 = 247690;
                    bioName1 = "Kyr'Ozch Bio-Material - Type 5";
                    bioId2 = 247692;
                    bioName2 = "Kyr'Ozch Bio-Material - Type 12";
                    msg = "High Evade/Dodge, casting Ilari Biorejuvenation heals.";
                    botInfo = "(Max Health / Body Dev)";
                    break;
                case "cha":
                    genName = "General - Cha'Khaz";
                    botId = 247141;
                    botName = "Supple Lead Viralbots";
                    bioId1 = 247696;
                    bioName1 = "Kyr'Ozch Bio-Material - Type 13";
                    bioId2 = 247674;
                    bioName2 = "Kyr'Ozch Bio-Material - Type 76";
                    msg = "High Evade/NR, Low Dodge.";
                    botInfo = "(Ranged / Spec Ranged / Add All Off)";
                    break;
                default:
                    bot.SendReply(e, "Unknown Alien General. Available options are: " + HTML.CreateColorString(bot.ColorHeaderHex, "ankari, ilari, rimah, jaax, xoch, cha"));
                    return;
            }


            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle(genName);

            window.AppendHighlight("Type: ");
            window.AppendNormal(msg);
            window.AppendLineBreak(2);

            window.AppendHeader("Drops");
            window.AppendHighlight(botName + " ");
            window.AppendNormalStart();
            window.AppendString(botInfo + " [");
            window.AppendItem("QL " + 300, botId, botId, 300);
            window.AppendString("] ");
            window.AppendColorEnd();
            window.AppendLineBreak();
            window.AppendIcon(botImageId);
            window.AppendLineBreak(2);

            window.AppendHighlight(bioName1 + " ");
            window.AppendNormalStart();
            window.AppendString("[");
            window.AppendItem("QL " + 300, bioId1, bioId1, 300);
            window.AppendString("] ");
            window.AppendColorEnd();
            window.AppendLineBreak();
            window.AppendIcon(bioImageId);
            window.AppendLineBreak();

            window.AppendLineBreak();
            window.AppendHighlight(bioName2 + " ");
            window.AppendNormalStart();
            window.AppendString("[");
            window.AppendItem("QL " + 300, bioId2, bioId2, 300);
            window.AppendString("] ");
            window.AppendColorEnd();
            window.AppendLineBreak();
            window.AppendIcon(bioImageId);
            window.AppendLineBreak();

            bot.SendReply(e, genName + " »» ", window);
        }


        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "aigen":
                    return "Displays information and loot drops about a specific alien general.\n" +
                        "Usage: /tell " + bot.Character + " aigen [ankari/ilari/rimah/jaax/xoch/cha]";
            }
            return null;
        }
    }
}