using System;
using AoLib.Utils;

namespace VhaBot.Plugins 
{
    public class VhAIArmor : PluginBase
    {
        private int ql;
        private int src_ql;
        private string armortype;
        private AoItem target;
        private AoItem source;
        private AoItem result;
        private string[] allowedArgs = { "cc", "cm", "co", "cp", "cs", "ss", "arithmetic", "enduring", "observant", "spiritual", "strong", "supple" };

        public VhAIArmor()
        {
            this.Name = "AI Armor";
            this.InternalName = "VhAIArmor";
            this.Version = 100;
            this.Author = "Neksus / Naturalistic";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("aiarmor", true, UserLevel.Guest)
            };
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            int ql_chk = -1;
            if (e.Args.Length < 1 || e.Args.Length > 2)
            {
                bot.SendReply(e, "Correct Usage: aiarmor [[ql]] [armor type]");
                return;
            }
            else if (e.Args.Length == 1)
            {
                ql = 300;
                armortype = e.Args[0].ToLower();
            }
            else if (!int.TryParse(e.Args[0], out ql_chk) || ql_chk < 1 || ql_chk > 300)
            {
                bot.SendReply(e, "Correct Usage: aiarmor [[ql]] [armor type]");
                return;
            }
            else
            {
                double num;
                bool isNum = double.TryParse(e.Args[0], out num);
                if (isNum)
                {
                    ql = Convert.ToInt32(e.Args[0]);
                }
                else
                {
                    bot.SendReply(e, "Correct Usage: aiarmor [[ql]] [armor type]");
                    return;
                }
                armortype = e.Args[1].ToLower();
            }
            bool realAibot = false;
            foreach (string test in allowedArgs)
            {
                if (armortype.Equals(test))
                {
                    realAibot = true;
                }
            }
            if (!realAibot)
            {
                bot.SendReply(e, "Correct Usage: aiarmor [[ql]] [armor type]");
                return;
            }

            src_ql = (int)Math.Ceiling(ql * 0.8);
            // Ai Armor
            AoItem arithmetic = new AoItem("Arithmetic", 246559, 246560, 300, "256314");
            AoItem enduring = new AoItem("Enduring", 246579, 246580, 300, "256344");
            AoItem observant = new AoItem("Observant", 246591, 246592, 300, "256338");
            AoItem spiritual = new AoItem("Spiritual", 246599, 246600, 300, "256332");
            AoItem strong = new AoItem("Strong", 246615, 246616, 300, "256362");
            AoItem supple = new AoItem("Supple", 246621, 246622, 300, "256296");

            if (armortype.Length == 2)
            {
                // Combined Ai Armor
                AoItem cc = new AoItem("Combined Commando's", 246659, 246659, ql, "256308");
                AoItem cm = new AoItem("Combined Mercenary's", 246637, 246638, ql, "256356");
                AoItem co = new AoItem("Combined Officer's", 246671, 246672, ql, "256320");
                AoItem cp = new AoItem("Combined Paramedic's", 246647, 246648, ql, "256350");
                AoItem cs = new AoItem("Combined Scout's", 246683, 246684, ql, "256326");
                AoItem ss = new AoItem("Combined Sharpshooter's", 246695, 246696, ql, "256302");
                switch (armortype)
                {
                    case "cc":
                        result = cc;
                        source = strong;
                        target = supple;
                        break;
                    case "cm":
                        result = cm;
                        source = strong;
                        target = enduring;
                        break;
                    case "co":
                        result = co;
                        source = spiritual;   
                        target = arithmetic;
                        break;
                    case "cp":
                        result = cp;
                        source = spiritual;
                        target = enduring;
                        break;
                    case "cs":
                        result = cs;
                        source = observant;
                        target = arithmetic;
                        break;
                    case "ss":
                        result = ss;
                        source = observant;	   
                        target = supple;
                        break;
                    default:
                        return;
                }

                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Building process for quality " + ql + " " + result.Name);
                window.AppendLineBreak();
                window.AppendHighlight("Result:\n");
                window.AppendItemStart(result.LowID, result.HighID, ql);
                window.AppendIcon(Convert.ToInt32(result.Raw));
                window.AppendLineBreak();
                window.AppendString(result.Name);
                window.AppendItemEnd();
                window.AppendLineBreak(2);
                window.AppendHighlight("Source:\n");
                window.AppendItemStart(source.LowID, source.HighID, src_ql);
                window.AppendIcon(Convert.ToInt32(source.Raw));
                window.AppendLineBreak();
                window.AppendString(source.Name);
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendCommand("Tradeskill process for this item", "/tell " + bot.Character + " aiarmor " + src_ql + " " + source.Name);
                window.AppendLineBreak(2);
                window.AppendHighlight("Target:\n");
                window.AppendItemStart(target.LowID, target.HighID, ql);
                window.AppendIcon(Convert.ToInt32(target.Raw));
                window.AppendLineBreak();
                window.AppendString(target.Name);
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendCommand("Tradeskill process for this item", "/tell " + bot.Character + " aiarmor " + ql + " " + target.Name);

                bot.SendReply(e, result.Name + " »» ", window);
            }
            else
            {
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Building process for ql" + ql + " " + armortype);
                window.AppendNormal("You need the following items to build " + armortype + " Armor:\n");
                window.AppendNormal("- Kyr'Ozch Viralbots\n");
                window.AppendNormal("- Kyr'Ozch Atomic Re-Structulazing Tool\n");
                window.AppendNormal("- Solid Clump of Kyr'Ozch Biomaterial\n");
                window.AppendNormal("- Arithmetic / Strong / Enduring / Spiritual / Observant / Supple Viralbots\n\n");
                // Step 1
                window.AppendHighlight("Step 1:\n");
                window.AppendItemStart(247113, 247114, src_ql);
                window.AppendIcon(100330);
                window.AppendLineBreak();
                window.AppendNormal("Kyr'Ozch Viralbots");
                window.AppendItemEnd();
                window.AppendNormal(" (Drops off Alien City Generals)\n\t\t\t+\n");
                window.AppendItemStart(247099, 247099, 100);
                window.AppendIcon(247098);
                window.AppendLineBreak();
                window.AppendNormal("Kyr'Ozch Atomic Re-Structuralizing Tool");
                window.AppendItemEnd();
                window.AppendNormal(" (Drops off every Alien)\n\t\t\t=\n");
                window.AppendItemStart(247118, 247119, src_ql);
                window.AppendIcon(100331);
                window.AppendLineBreak();
                window.AppendNormal("Memory-Wiped Kyr'Ozch Viralbots");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendHighlight("Required Skills:\n");
                window.AppendNormal("- " + (ql * 4.5) + " Computer Literacy\n");
                window.AppendNormal("- " + (ql * 4.5) + " Nano Programming\n\n");
                // Step 2
                window.AppendHighlight("Step 2:\n");
                window.AppendItemStart(161699, 161699, 1);
                window.AppendIcon(99279);
                window.AppendLineBreak();
                window.AppendNormal("Nano Programming Interface");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t+\n");
                window.AppendItemStart(247118, 247119, src_ql);
                window.AppendIcon(100331);
                window.AppendLineBreak();
                window.AppendNormal("Memory-Wiped Kyr'Ozch Viralbots");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t=\n");
                window.AppendItemStart(247120, 247121, src_ql);
                window.AppendIcon(100334);
                window.AppendLineBreak();
                window.AppendNormal("Formatted Kyr'Ozch Viralbots");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendHighlight("Required Skills:\n");
                window.AppendNormal("- " + (ql * 6) + " Nano Programming\n\n");
                // Step 3
                window.AppendHighlight("Step 3:\n");
                window.AppendItemStart(247100, 247100, 100);
                window.AppendIcon(247097);
                window.AppendLineBreak();
                window.AppendNormal("Kyr'Ozch Structural Analyzer");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t+\n");
                window.AppendItemStart(247102, 247103, ql);
                window.AppendIcon(247101);
                window.AppendLineBreak();
                window.AppendNormal("ql" + ql + " Solid Clump of Kyr'Ozch Biomaterial");
                window.AppendItemEnd();
                window.AppendNormal(" (Drops off every Alien)\n\t\t\t=\n");
                window.AppendItemStart(247108, 247109, ql);
                window.AppendIcon(255705);
                window.AppendLineBreak();
                window.AppendNormal("ql" + ql + " Mutated Kyr'Ozch Biomaterial");
                window.AppendItemEnd();
                window.AppendNormal(" or\n");
                window.AppendItemStart(247106, 247107, ql);
                window.AppendIcon(255705);
                window.AppendLineBreak();
                window.AppendNormal("ql" + ql + " Pristine Kyr'Ozch Biomaterial");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendHighlight("Required Skills:\n");
                window.AppendNormal("- " + (ql * 4.5) + " Chemistry (for Pristine)\n");
                window.AppendNormal("- " + (ql * 7) + " Chemistry (for Mutated)\n\n");
                //Step 4
                window.AppendHighlight("Step 4:\n");
                window.AppendItemStart(247108, 247109, ql);
                window.AppendIcon(255705);
                window.AppendLineBreak();
                window.AppendNormal("ql" + ql + " Mutated Kyr'Ozch Biomaterial");
                window.AppendItemEnd();
                window.AppendNormal(" or\n");
                window.AppendItemStart(247106, 247107, ql);
                window.AppendIcon(255705);
                window.AppendLineBreak();
                window.AppendNormal("ql" + ql + " Pristine Kyr'Ozch Biomaterial");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t+\n");
                window.AppendItemStart(247110, 247110, 100);
                window.AppendIcon(255705);
                window.AppendLineBreak();
                window.AppendNormal("Uncle Bazzit's Generic Nano Solvent");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendNormal(" (Can be bought in Bazzit Shop in MMD)\n\t\t\t=\n");
                window.AppendItemStart(247111, 247112, ql);
                window.AppendIcon(247115);
                window.AppendLineBreak();
                window.AppendNormal("Generic Kyr'Ozch DNA Soup");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendHighlight("Required Skills:\n");
                window.AppendNormal("- " + (ql * 4.5) + " Chemistry (for Pristine)\n");
                window.AppendNormal("- " + (ql * 7) + " Chemistry (for Mutated)\n\n");
                //Step 5
                window.AppendHighlight("Step 5:\n");
                window.AppendItemStart(247111, 247112, ql);
                window.AppendIcon(247115);
                window.AppendLineBreak();
                window.AppendNormal("Generic Kyr'Ozch DNA Soup");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t+\n");
                window.AppendItemStart(247123, 247123, 100);
                window.AppendIcon(247122);
                window.AppendLineBreak();
                window.AppendNormal("Essential Human DNA");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendNormal(" (Can be bought in Bazzit Shop in MMD)\n\t\t\t=\n");
                window.AppendItemStart(247124, 247125, ql);
                window.AppendIcon(247116);
                window.AppendLineBreak();
                window.AppendNormal("DNA Cocktail");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendHighlight("Required Skills:\n");
                window.AppendNormal("- " + (ql * 6) + " Pharma Tech\n\n");
                //Step 6
                window.AppendHighlight("Step 6:\n");
                window.AppendItemStart(247120, 247121, ql);
                window.AppendIcon(100334);
                window.AppendLineBreak();
                window.AppendNormal("Formatted Kyr'Ozch Viralbots");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t+\n");
                window.AppendItemStart(247124, 247125, ql);
                window.AppendIcon(247116);
                window.AppendLineBreak();
                window.AppendNormal("DNA Cocktail");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t=\n");
                window.AppendItemStart(247126, 247127, ql);
                window.AppendIcon(247117);
                window.AppendLineBreak();
                window.AppendNormal("Kyr'Ozch Formatted Viralbot Solution");
                window.AppendItemEnd();
                window.AppendLineBreak();
                window.AppendHighlight("Required Skills:\n");
                window.AppendNormal("- " + (ql * 6) + " Pharma Tech\n\n");
                //Step 7
                window.AppendHighlight("Step 7:\n");
                window.AppendItemStart(247126, 247127, ql);
                window.AppendIcon(247117);
                window.AppendLineBreak();
                window.AppendNormal("Kyr'Ozch Formatted Viralbot Solution");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t+\n");
                window.AppendItemStart(247163, 247163, 1);
                window.AppendIcon(245924);
                window.AppendLineBreak();
                window.AppendNormal("Basic Vest");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t=\n");
                window.AppendItemStart(247172, 247173, ql);
                window.AppendIcon(245924);
                window.AppendLineBreak();
                window.AppendNormal("Formatted Viralbot Vest");
                window.AppendItemEnd();
                window.AppendLineBreak(2);
                //Step 8
                window.AppendHighlight("Step 8:\n");
                window.AppendIcon(100337);
                window.AppendLineBreak();
                switch (armortype)
                {
                    case "arithmetic":
                        window.AppendItem("QL " + src_ql + " Arithmetic Lead Viralbots", 247144, 247145, src_ql);
                        break;
                    case "supple":
                        window.AppendItem("QL " + src_ql + " Supple Lead Viralbots", 247140, 247141, src_ql);
                        break;
                    case "enduring":
                        window.AppendItem("QL " + src_ql + " Enduring Lead Viralbots", 247136, 247137, src_ql);
                        break;
                    case "observant":
                        window.AppendItem("QL " + src_ql + " Observant Lead Viralbots", 247142, 247143, src_ql);
                        break;
                    case "strong":
                        window.AppendItem("QL " + src_ql + " Strong Lead Viralbots", 247138, 247139, src_ql);
                        break;
                    case "spiritual":
                        window.AppendItem("QL " + src_ql + " Spiritual Lead Viralbots", 247146, 247147, src_ql);
                        break;
                }
                window.AppendNormal(" (Rare drop off Alien City Generals)");
                window.AppendNormal("\n\t\t\t+\n");
                window.AppendItemStart(247172, 247173, ql);
                window.AppendIcon(245924);
                window.AppendLineBreak();
                window.AppendNormal("Formatted Viralbot Vest");
                window.AppendItemEnd();
                window.AppendNormal("\n\t\t\t=\n");
                switch (armortype)
                {
                    case "arithmetic":
                        window.AppendItemStart(arithmetic.LowID, arithmetic.HighID, ql);
                        window.AppendIcon(Convert.ToInt32(arithmetic.Raw));
                        window.AppendLineBreak();
                        window.AppendString(arithmetic.Name + " Body Armor");
                        window.AppendItemEnd();
                        window.AppendLineBreak();
                        break;
                    case "supple":
                        window.AppendItemStart(supple.LowID, supple.HighID, ql);
                        window.AppendIcon(Convert.ToInt32(supple.Raw));
                        window.AppendLineBreak();
                        window.AppendString(supple.Name + " Body Armor");
                        window.AppendItemEnd();
                        window.AppendLineBreak();
                        break;
                    case "enduring":
                        window.AppendItemStart(enduring.LowID, enduring.HighID, ql);
                        window.AppendIcon(Convert.ToInt32(enduring.Raw));
                        window.AppendLineBreak();
                        window.AppendString(enduring.Name + " Body Armor");
                        window.AppendItemEnd();
                        window.AppendLineBreak();
                        break;
                    case "observant":
                        window.AppendItemStart(observant.LowID, observant.HighID, ql);
                        window.AppendIcon(Convert.ToInt32(observant.Raw));
                        window.AppendLineBreak();
                        window.AppendString(observant.Name + " Body Armor");
                        window.AppendItemEnd();
                        window.AppendLineBreak();
                        break;
                    case "strong":
                        window.AppendItemStart(strong.LowID, strong.HighID, ql);
                        window.AppendIcon(Convert.ToInt32(strong.Raw));
                        window.AppendLineBreak();
                        window.AppendString(strong.Name + " Body Armor");
                        window.AppendItemEnd();
                        window.AppendLineBreak();
                        break;
                    case "spiritual":
                        window.AppendItemStart(spiritual.LowID, spiritual.HighID, ql);
                        window.AppendIcon(Convert.ToInt32(spiritual.Raw));
                        window.AppendLineBreak();
                        window.AppendString(spiritual.Name + " Body Armor");
                        window.AppendItemEnd();
                        window.AppendLineBreak();
                        break;
                }
                window.AppendHighlight("Required Skills:\n");
                window.AppendNormal("- " + (ql * 6) + " Psychology\n\n");

                bot.SendReply(e, "Building process for " + armortype + " »» ", window);
            }
        }
        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "aiarmor":
                    return "Shows the process of making Alien Armor\n" +
                        "Usage: /tell " + bot.Character + " aiarmor [[ql]] [armortype]\n" +
                        "Allowed armortype options are: cc, cm, co, cp, cs, ss, arithmetic, enduring, observant, spiritual, strong, supple";
            }
            return null;
        }
    }
}