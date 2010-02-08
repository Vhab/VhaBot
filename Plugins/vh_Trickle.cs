using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class VhTrickle : PluginBase
    {
        public VhTrickle()
        {
            this.Name = "Trickle Amount Calculator";
            this.InternalName = "VhTrickle";
            this.Version = 1;
            this.Author = "Neksus - Recoded and modded by Naturalistic";
            this.Description = "Shows how much skill you will get from ability trickle-downs.";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("trickle str", true, UserLevel.Guest),
                new Command("trickle sta", true, UserLevel.Guest),
                new Command("trickle agi", true, UserLevel.Guest),
                new Command("trickle sen", true, UserLevel.Guest),
                new Command("trickle int", true, UserLevel.Guest),
                new Command("trickle psy", true, UserLevel.Guest),
                new Command("trickle", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            lock (this)
            {
                if (e.Args.Length < 1)
                {
                    bot.SendReply(e, "Correct Usage: trickle str/sta/agi/sen/int/psy [amount]");
                    return;
                }
                switch (e.Command.ToLower())
                {
                    case "trickle str":
                        this.TrickleStr(bot, e);
                        break;
                    case "trickle sta":
                        this.TrickleSta(bot, e);
                        break;
                    case "trickle agi":
                        this.TrickleAgi(bot, e);
                        break;
                    case "trickle sen":
                        this.TrickleSen(bot, e);
                        break;
                    case "trickle int":
                        this.TrickleInt(bot, e);
                        break;
                    case "trickle psy":
                        this.TricklePsy(bot, e);
                        break;
                    case "trickle":
                        this.TrickleBase(bot, e);
                        break;
                }
            }
        }

        public override void OnUnload(BotShell bot) { }

        private void TrickleStr(BotShell bot, CommandArgs e)
        {
            Double Test;
            Test = Convert.ToInt32(e.Args[0]);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendNormalStart();
            window.AppendTitle();
            window.AppendString("             Body"); window.AppendLineBreak();
            window.AppendString("Adventuring: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Brawling: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Martial Arts: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Swimming: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Melee"); window.AppendLineBreak();
            window.AppendString("1 Handed Blunt: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("1 Handed Edged: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("2 Handed Blunt: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("2 Handed Edged: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Multiple Melee: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Parry: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Piercing: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Misc"); window.AppendLineBreak();
            window.AppendString("Heavy Weapons: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Sharp Object: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Ranged"); window.AppendLineBreak();
            window.AppendString("Assault Rifle: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Bow Special Attack: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Bow: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Burst: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Full Auto: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Shotgun: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("SMG/MG: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Speed"); window.AppendLineBreak();
            window.AppendString("Runspeed: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Trade & Repair"); window.AppendLineBreak();
            window.AppendString("Weapon Smithing: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Nano and Aiding"); window.AppendLineBreak();
            window.AppendString("Sensory Improvement: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();

            bot.SendReply(e, bot.ColorHeader + "Trickling " + bot.ColorHighlight + e.Args[0] + bot.ColorHeader + " To Strength " + bot.ColorHighlight + " »» ", window);
        }
        private void TrickleSta(BotShell bot, CommandArgs e)
        {
            Double Test;
            Test = Convert.ToInt32(e.Args[0]);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendNormalStart();
            window.AppendTitle();
            window.AppendString("             Body"); window.AppendLineBreak();
            window.AppendString("Adventuring: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Body Dev: "); window.AppendNormal(Math.Floor(((Test / 4) * 1)).ToString()); window.AppendLineBreak();
            window.AppendString("Brawling: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Nano Pool: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Swimming: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Melee"); window.AppendLineBreak();
            window.AppendString("1 Handed Blunt: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("1 Handed Edged: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("2 Handed Blunt: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("2 Handed Edged: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Melee Energy: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Multiple Melee: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Piercing: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Ranged"); window.AppendLineBreak();
            window.AppendString("Assault Rifle: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Burst: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Full Auto: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("SMG/MG: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Speed"); window.AppendLineBreak();
            window.AppendString("Runspeed: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Trade & Repair"); window.AppendLineBreak();
            window.AppendString("Chemistry: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Electrical Engineering: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Nano and Aiding"); window.AppendLineBreak();
            window.AppendString("Matter Creations: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();

            bot.SendReply(e, bot.ColorHeader + "Trickling " + bot.ColorHighlight + e.Args[0] + bot.ColorHeader + " To Stamina " + bot.ColorHighlight + " »» ", window);
        }
        private void TrickleAgi(BotShell bot, CommandArgs e)
        {
            Double Test;
            Test = Convert.ToInt32(e.Args[0]);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendNormalStart();
            window.AppendTitle();
            window.AppendString("             Body"); window.AppendLineBreak();
            window.AppendString("Adventuring: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Martial Arts: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Riposte: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Swimming: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Melee"); window.AppendLineBreak();
            window.AppendString("1 Handed Blunt: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("1 Handed Edged: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Fast Attack: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("Multiple Melee: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Parry: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Piercing: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("             Misc"); window.AppendLineBreak();
            window.AppendString("Grenade: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Heavy Weapons: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Sharp Object: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Ranged"); window.AppendLineBreak();
            window.AppendString("Assault Rifle: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Bow Special Attack: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Bow: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Burst: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Fling Shot: "); window.AppendNormal(Math.Floor(((Test / 4) * 1)).ToString()); window.AppendLineBreak();
            window.AppendString("Multiple Ranged: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Pistol: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Rifle: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Shotgun: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("SMG/MG: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Speed"); window.AppendLineBreak();
            window.AppendString("Dodge Ranged: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Duck Explosions: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Evade Close: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Melee Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Nano Cast Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Physical Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Ranged Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Runspeed: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Trade & Repair"); window.AppendLineBreak();
            window.AppendString("Electrical Engineering: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Mechanical Engineering: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Nano and Aiding"); window.AppendLineBreak();
            window.AppendString("First Aid: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Time and Space: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Treatment: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("           Spying"); window.AppendLineBreak();
            window.AppendString("Breaking and Entering: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Concealment: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Trap Disarm: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("          Navigation"); window.AppendLineBreak();
            window.AppendString("Vehicle Air: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Ground: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Water: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();

            bot.SendReply(e, bot.ColorHeader + "Trickling " + bot.ColorHighlight + e.Args[0] + bot.ColorHeader + " To Agility " + bot.ColorHighlight + " »» ", window);
        }
        private void TrickleSen(BotShell bot, CommandArgs e)
        {
            Double Test;
            Test = Convert.ToInt32(e.Args[0]);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendNormalStart();
            window.AppendTitle();
            window.AppendString("             Body"); window.AppendLineBreak();
            window.AppendString("Dimach: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Nano Pool: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Riposte: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Melee"); window.AppendLineBreak();
            window.AppendString("Fast Attack: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Parry: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Sneak Attack: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Misc"); window.AppendLineBreak();
            window.AppendString("Grenade: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Sharp Object: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Ranged"); window.AppendLineBreak();
            window.AppendString("Aimed Shot: "); window.AppendNormal(Math.Floor(((Test / 4) * 1)).ToString()); window.AppendLineBreak();
            window.AppendString("Assault Rifle: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Bow Special Attack: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Bow: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Pistol: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Ranged Energy: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Rifle: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("SMG/MG: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Speed"); window.AppendLineBreak();
            window.AppendString("Dodge Ranged: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Duck Explosions: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Evade Close: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Melee Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Nano Cast Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Physical Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Ranged Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Trade & Repair"); window.AppendLineBreak();
            window.AppendString("Tutoring: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Nano and Aiding"); window.AppendLineBreak();
            window.AppendString("First Aid: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Psychological Modifications: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Treatment: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("           Spying"); window.AppendLineBreak();
            window.AppendString("Breaking and Entering: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Concealment: "); window.AppendNormal(Math.Floor(((Test / 4) * .70)).ToString()); window.AppendLineBreak();
            window.AppendString("Perception: "); window.AppendNormal(Math.Floor(((Test / 4) * .70)).ToString()); window.AppendLineBreak();
            window.AppendString("Trap Disarm: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("          Navigation"); window.AppendLineBreak();
            window.AppendString("Map Navigations: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Air: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Ground: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Water: "); window.AppendNormal(Math.Floor(((Test / 4) * .60)).ToString()); window.AppendLineBreak();

            bot.SendReply(e, bot.ColorHeader + "Trickling " + bot.ColorHighlight + e.Args[0] + bot.ColorHeader + " To Sense " + bot.ColorHighlight + " »» ", window);
        }
        private void TrickleInt(BotShell bot, CommandArgs e)
        {
            Double Test;
            Test = Convert.ToInt32(e.Args[0]);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendNormalStart();
            window.AppendTitle();
            window.AppendString("             Body"); window.AppendLineBreak();
            window.AppendString("Nano Pool: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Melee"); window.AppendLineBreak();
            window.AppendString("Melee Energy: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Misc"); window.AppendLineBreak();
            window.AppendString("Grenade: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Ranged"); window.AppendLineBreak();
            window.AppendString("Multiple Ranged: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Ranged Energy: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Speed"); window.AppendLineBreak();
            window.AppendString("Dodge Ranged: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Duck Explosions: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Evade Close: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Melee Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Nano Resist: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Physical Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();
            window.AppendString("Ranged Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Trade & Repair"); window.AppendLineBreak();
            window.AppendString("Chemistry: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Computer Literacy: "); window.AppendNormal(Math.Floor(((Test / 4) * 1)).ToString()); window.AppendLineBreak();
            window.AppendString("Electrical Engineering: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Mechanical Engineering: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Nano Programming: "); window.AppendNormal(Math.Floor(((Test / 4) * 1)).ToString()); window.AppendLineBreak();
            window.AppendString("Pharmacological Technologies: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Psychology: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Quantum FT: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Tutoring: "); window.AppendNormal(Math.Floor(((Test / 4) * .70)).ToString()); window.AppendLineBreak();
            window.AppendString("Weapon Smithing: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Nano and Aiding"); window.AppendLineBreak();
            window.AppendString("Biological Metamorphasis: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("First Aid: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Matter Creations: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Matter Metamophasis: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Psychological Modifications: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Sensory Improvement: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Time and Space: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Treatment: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("           Spying"); window.AppendLineBreak();
            window.AppendString("Perception: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak();
            window.AppendString("Trap Disarm: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("          Navigation"); window.AppendLineBreak();
            window.AppendString("Map Navigation: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Air: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Ground: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Vehicle Water: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();

            bot.SendReply(e, bot.ColorHeader + "Trickling " + bot.ColorHighlight + e.Args[0] + bot.ColorHeader + " To Intelligence " + bot.ColorHighlight + " »» ", window);
        }
        private void TricklePsy(BotShell bot, CommandArgs e)
        {
            Double Test;
            Test = Convert.ToInt32(e.Args[0]);
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendNormalStart();
            window.AppendTitle();
            window.AppendString("             Body"); window.AppendLineBreak();
            window.AppendString("Dimach: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Martial Arts: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("             Melee"); window.AppendLineBreak();
            window.AppendString("Sneak Attack: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Ranged"); window.AppendLineBreak();
            window.AppendString("Ranged Energy: "); window.AppendNormal(Math.Floor(((Test / 4) * .40)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("            Speed"); window.AppendLineBreak();
            window.AppendString("Melee Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Nano Resist: "); window.AppendNormal(Math.Floor(((Test / 4) * .80)).ToString()); window.AppendLineBreak();
            window.AppendString("Physical Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Ranged Initiative: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Trade & Repair"); window.AppendLineBreak();
            window.AppendString("Psychology: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Quantum FT: "); window.AppendNormal(Math.Floor(((Test / 4) * .50)).ToString()); window.AppendLineBreak();
            window.AppendString("Tutoring: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("        Nano and Aiding"); window.AppendLineBreak();
            window.AppendString("Biological Metamorphasis: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak();
            window.AppendString("Matter Metamorphasis: "); window.AppendNormal(Math.Floor(((Test / 4) * .20)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("           Spying"); window.AppendLineBreak();
            window.AppendString("Break And Enter: "); window.AppendNormal(Math.Floor(((Test / 4) * .30)).ToString()); window.AppendLineBreak(); window.AppendLineBreak();
            window.AppendString("          Navigation"); window.AppendLineBreak();
            window.AppendString("Map Navigation: "); window.AppendNormal(Math.Floor(((Test / 4) * .10)).ToString()); window.AppendLineBreak();

            bot.SendReply(e, bot.ColorHeader + "Trickling " + bot.ColorHighlight + e.Args[0] + bot.ColorHeader + " To Psychic " + bot.ColorHighlight + " »» ", window);
        }
        private void TrickleBase(BotShell bot, CommandArgs e)
        {
            bot.SendReply(e, "Usage: trickle str/sta/agi/sen/int/psy [amount]");
            return;
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "trickle":
                    return "Displays trickle information for the attribute specified.\n" +
                        "Usage: /tell " + bot.Character + " trickle str/sta/agi/sen/int/psy [amount]";
            }
            return null;
        }
    }
}
