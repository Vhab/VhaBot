using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Data;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{
    public class VhBuffs : PluginBase
    {
        private BotShell _bot;

        public VhBuffs()
        {
            this.Name = "Buff Listing Function";
            this.InternalName = "VhBuffs";
            this.Author = "Fletche (Atlantean)";
            this.DefaultState = PluginState.Installed;
            this.Description = "Responds to buffs messages";
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("buffs", true, UserLevel.Guest),
                new Command("basebuffs", true, UserLevel.Guest),
                new Command("skillbuffs", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "buffs":
                	this.OnBuffsCommand(bot, e);
                	break;

		case "basebuffs":
			this.OnBaseBuffsCommand(bot, e);
			break;

		case "skillbuffs":
			this.OnSkillBuffsCommand(bot, e);
			break;
            }
        }

        
	public void OnBuffsCommand(BotShell bot, CommandArgs e)
	{
		RichTextWindow buffsWindow = new RichTextWindow(bot);
		buffsWindow.AppendTitle("Class Buffs");
		buffsWindow.AppendLineBreak();

		buffsWindow.AppendCommand("Base Attribute Buffs", "/tell " + this._bot.Character + " basebuffs");
		buffsWindow.AppendLineBreak(2);
		buffsWindow.AppendCommand("Skill Buffs", "/tell " + this._bot.Character + " skillbuffs");

		bot.SendReply(e, " Class Buffs »» ", buffsWindow); 				
	}

	public void OnBaseBuffsCommand(BotShell bot, CommandArgs e)
        {  
		RichTextWindow baseBuffsWindow = new RichTextWindow(bot);
		baseBuffsWindow.AppendTitle("Base Attribute Buffs");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorOrange, ">> Buffs listed in the same color do not stack with one another. <<");
		
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendHighlight("Strength");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Enforcer");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorRed, "  * +5 to +27 - Essence of <...> (5-47 NCU)");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +54 - Imp. Essence of Behemoth (56 NCU, Level 215)");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +40 - Prodigious Strength (42 NCU)");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Martial Artist");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +25 - Muscle Booster (22 NCU)");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +12 - Muscle Stim (7 NCU)");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Doctor");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +10 - Enlarge (12 NCU)");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +20 - Iron Circle (26 NCU)");

		baseBuffsWindow.AppendLineBreak(3);
		baseBuffsWindow.AppendHighlight("Agility");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Agent");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendNormal("  * +25 - Feline Grace (17 NCU)");

		baseBuffsWindow.AppendLineBreak(3);
		baseBuffsWindow.AppendHighlight("Stamina");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Enforcer");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorRed, "  * +5 to +27 - Essence of <...> (5-47 NCU)");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +54 - Imp. Essence of Behemoth (56 NCU, Level 215)");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Doctor");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +10 - Enlarge (12 NCU)");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendColorString(RichTextWindow.ColorGreen, "  * +20 - Iron Circle (26 NCU)");

		baseBuffsWindow.AppendLineBreak(3);
		baseBuffsWindow.AppendHighlight("Intelligence");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Nano-Technician");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendNormal("  * +20 - Neuronal Stimulator (7 NCU)");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Bureaucrat");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendNormal("  * +3 - Imp. Cut Red Tape (29 NCU)");

		baseBuffsWindow.AppendLineBreak(3);
		baseBuffsWindow.AppendHighlight("Sense");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Agent");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendNormal("  * +15 - Enhanced Senses (9 NCU)");

		baseBuffsWindow.AppendLineBreak(3);
		baseBuffsWindow.AppendHighlight("Psychic");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Nano-Technician");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendNormal("  * +20 - Neuronal Stimulator (7 NCU)");
		baseBuffsWindow.AppendLineBreak(2);
		baseBuffsWindow.AppendNormal("  Bureaucrat");
		baseBuffsWindow.AppendLineBreak();
		baseBuffsWindow.AppendNormal("  * +3 - Imp. Cut Red Tape (29 NCU)");

		bot.SendReply(e, " Base Attribute Buffs »» ", baseBuffsWindow);

        }
	
	public void OnSkillBuffsCommand(BotShell bot, CommandArgs e)
        {  
		RichTextWindow skillBuffsWindow = new RichTextWindow(bot);
		skillBuffsWindow.AppendTitle("Skill Buffs");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendHighlight("Note:  This list does not include self only buffs.");
		
		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Meta-Physicist Nano Skills (Individual)");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  +25 - Teachings (6 NCU)\n");
		skillBuffsWindow.AppendNormal("  +50 - Mastery (16 NCU)\n");
		skillBuffsWindow.AppendNormal("  +90 - Infuse (39 NCU)\n");
		skillBuffsWindow.AppendNormal("  +140 - Mochams (50 NCU)");
		
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendHighlight("Meta-Physicist Nano Skills (Composite)");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  +25 - Teachings (6 NCU)  [SL, Level 15]\n");
		skillBuffsWindow.AppendNormal("  +50 - Mastery (13 NCU) [SL, Level 40]\n");
		skillBuffsWindow.AppendNormal("  +90 - Infuse (25 NCU) [SL, Level 90]\n");
		skillBuffsWindow.AppendNormal("  +140 - Mochams (48 NCU) [SL, Level 175]");

		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendHighlight("The Wrangle");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  Trader");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * All attack and nano skills can be buffed from 3 to 131 [153 w/perks]\n    (3 NCU - 58 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("1 Handed Blunt");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Enforcer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +87 - Brutal Thug (24 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("2 Handed Blunt");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Enforcer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +87 - Brutal Thug (24 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Assault Rifle");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Soldier");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +60 - Assault Rifle Mastery (16 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Aimed Shot");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Adventurer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +20 - Eagle Eye (24 NCU)");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Agent");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +130 - Take the Shot (47 NCU)");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +15 - Sniper's Bliss (14 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Burst");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Fixer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +7 - Minor Suppressor (1 NCU)");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Soldier");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +110 - Riot Control (39 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Brawl");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Martial Artist");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +45 - Dirty Fighter (11 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Martial Arts");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Martial Artist");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +12 - Lesser Controlled Rage (1 NCU)");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +60 - Martial Arts Mastery (16 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("MG/SMG");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Fixer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +8 - Minor Suppressor (1 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Perception");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Adventurer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +120 - Lupus Oculus (15 NCU)");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +80 - Eagle Eye (24 NCU)");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Fixer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +130 - Karma Harvest (47 NCU)");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +107 - Blood Makes Noise (36 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Pistol");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Bureaucrat");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +20 - Gunslinger (5 NCU)\n    (Does not stack with Pistol Mastery)");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Soldier");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +40 - Pistol Mastery (8 NCU)");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Engineer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +120 - Extreme Prejudice (43 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Riposte");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Martial Artist");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +50 - Return Attack (45 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Rifle");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Agent");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +120 - Unexpected Attack (42 NCU)");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +50 - Sniper's Bliss (14 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Sneak Attack");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Agent");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +30 - Unexpected Attack (42 NCU)");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Fixer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +64 - Backpain (18 NCU)");

		skillBuffsWindow.AppendLineBreak(3);
		skillBuffsWindow.AppendHighlight("Treatment");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Adventurer");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +60 - Robust Treatment (+42 NCU)");
		skillBuffsWindow.AppendLineBreak(2);
		skillBuffsWindow.AppendNormal("  Doctor");
		skillBuffsWindow.AppendLineBreak();
		skillBuffsWindow.AppendNormal("  * +80 - Superior First Aid (37 NCU)");

		bot.SendReply(e, " Skill Buffs »» ", skillBuffsWindow);
	}

    }
}
