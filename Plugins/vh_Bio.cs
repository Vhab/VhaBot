/*
 * 247102 / 247103 = Pristine Kyr'Ozch Bio-Material (DNA soup)
 * 247104 / 247105 = Mutated Kyr'Ozch Bio-Material (DNA soup)
 * 247697 / 247698 = Kyr'Ozch Bio-Material - Type 76 (Weapon Upgrade)
 * 247699 / 247700 = ??? (Type 112)
 * 247701 / 247702 = ??? (Type 240)
 * 247703 / 247704 = ??? (Type 880)
 * 247705 / 247706 = ??? (Type 992)
 * 247707 / 247708 = Kyr'Ozch Bio-Material - Type 1 (Weapon Upgrade)
 * 247709 / 247710 = Kyr'Ozch Bio-Material - Type 2 (Weapon Upgrade)
 * 247711 / 247712 = ??? (Type 4)
 * 247713 / 247714 = ??? (Type 5)
 * 247715 / 247716 = ??? (Type 12)
 * 247717 / 247718 = ??? (Type 3)
 * 247719 / 247720 = ??? (Type 13)
 * 247764 / 254804 = Kyr'Ozch Viral Serum (Making high QL buildings)
 * 256035 = Useless Kyr'Ozch Bio-Material
 *
 * Type 1 = Fling Shot
 * Type 112 = Brawl, Dimach and Fast Attack
 * Type 12 = Burst and Full Auto
 * Type 13 = Burst, Fling Shot and Full Auto
 * Type 2 = Aimed Shot
 * Type 240 = Brawl, Dimach, Fast Attack and Sneak Attack
 * Type 3 = Fling Shot and Aimed Shot
 * Type 4 = Burst
 * Type 5 = Fling Shot and Burst
 * Type 76 = Brawl and Fast Attack
 * Type 880 = Dimach, Fast Attack, Parry and Riposte
 * Type 992 = Dimach, Fast Attack, Sneak Attack, Parry and Riposte
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class VhBio : PluginBase
    {
        private Dictionary<int, VhBioClump> _clumps = new Dictionary<int, VhBioClump>();

        public VhBio()
        {
            this.Name = "Bio-Material Identifier";
            this.InternalName = "VhBio";
            this.Version = 100;
            this.Author = "Iriche";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("bio", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._clumps.Add(247103, new VhBioClump(0, "Pristine Kyr'Ozch Bio-Material", "Used for making alien armor"));
            this._clumps.Add(247105, new VhBioClump(0, "Mutated Kyr'Ozch Bio-Material", "Used for making alien armor"));
            this._clumps.Add(254804, new VhBioClump(0, "Kyr'Ozch Viral Serum", "Used for making high QL buildings"));

            VhBioClump clump = new VhBioClump(1, "Kyr'Ozch Bio-Material - Type 1", "Fling shot");
            clump.Weapons.Add(new VhBioWeapon("Grenade Gun", 1, 99, 247437, 247438));
            clump.Weapons.Add(new VhBioWeapon("Grenade Gun", 100, 199, 247439, 247440));
            clump.Weapons.Add(new VhBioWeapon("Grenade Gun", 200, 299, 247441, 247442));
            clump.Weapons.Add(new VhBioWeapon("Grenade Gun", 300, 300, 247443, 247443));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 1, 99, 254619, 254620));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 100, 199, 254621, 254622));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 200, 299, 254623, 254624));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 300, 300, 254625, 254625));
            clump.Weapons.Add(new VhBioWeapon("Shotgun", 1, 99, 247437, 247438));
            clump.Weapons.Add(new VhBioWeapon("Shotgun", 200, 199, 254539, 254540));
            clump.Weapons.Add(new VhBioWeapon("Shotgun", 200, 299, 254541, 254542));
            clump.Weapons.Add(new VhBioWeapon("Shotgun", 300, 300, 254543, 254543));
            this._clumps.Add(247708, clump);

            clump = new VhBioClump(2, "Kyr'Ozch Bio-Material - Type 2", "Aimed Shot");
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 1, 99, 254605, 254606));
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 100, 199, 254607, 254608));
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 200, 299, 254609, 254610));
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 300, 300, 254611, 254611));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 1, 99, 254472, 254473));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 100, 199, 254474, 254475));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 200, 299, 254476, 254477));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 300, 300, 254478, 254478));
            this._clumps.Add(247710, clump);

            clump = new VhBioClump(3, "Kyr'Ozch Bio-Material - Type 3", "Fling Shot and Aimed Shot");
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 1, 99, 254598, 254599));
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 100, 199, 254600, 254601));
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 200, 299, 254602, 254603));
            clump.Weapons.Add(new VhBioWeapon("Crossbow", 300, 300, 254604, 254604));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 1, 99, 254556, 254557));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 100, 199, 254558, 254559));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 200, 299, 254560, 254561));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 300, 300, 254562, 254562));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 1, 99, 254479, 254480));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 100, 199, 254481, 254482));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 200, 299, 254483, 254484));
            clump.Weapons.Add(new VhBioWeapon("Rifle", 300, 300, 254485, 254485));
            this._clumps.Add(247718, clump);

            clump = new VhBioClump(4, "Kyr'Ozch Bio-Material - Type 4", "Burst");
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 1, 99, 254654, 254655));
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 100, 199, 254656, 254657));
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 200, 299, 254658, 254659));
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 300, 300, 54660, 254660));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 1, 99, 54626, 254627));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 100, 199, 254628, 254629));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 200, 299, 254630, 254631));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 300, 300, 254632, 254632));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 1, 99, 254521, 254522));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 100, 199, 254523, 254524));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 200, 299, 254525, 254526));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 300, 300, 254527, 254527));
            this._clumps.Add(247712, clump);

            clump = new VhBioClump(5, "Kyr'Ozch Bio-Material - Type 5", "Fling Shot and Burst");
            clump.Weapons.Add(new VhBioWeapon("Carbine", 1, 99, 254493, 254494));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 100, 199, 254495, 254496));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 200, 299, 254497, 254498));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 300, 300, 254499, 254499));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 1, 99, 254549, 254550));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 100, 199, 254551, 254552));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 200, 299, 254553, 254554));
            clump.Weapons.Add(new VhBioWeapon("Energy Carbine", 300, 300, 254555, 254555));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 1, 99, 254640, 254641));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 100, 199, 254642, 254643));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 200, 299, 254644, 254645));
            clump.Weapons.Add(new VhBioWeapon("Pistol", 300, 300, 254646, 254646));
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 1, 99, 254661, 254662));
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 100, 199, 254663, 254664));
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 200, 299, 254665, 254666));
            clump.Weapons.Add(new VhBioWeapon("Machine Pistol", 300, 300, 254667, 254667));
            clump.Weapons.Add(new VhBioWeapon("Submachine gun", 1, 99, 254528, 254529));
            clump.Weapons.Add(new VhBioWeapon("Submachine gun", 100, 199, 254530, 254531));
            clump.Weapons.Add(new VhBioWeapon("Submachine gun", 200, 299, 254532, 254533));
            clump.Weapons.Add(new VhBioWeapon("Submachine gun", 300, 300, 254534, 254534));
            this._clumps.Add(247714, clump);

            clump = new VhBioClump(12, "Kyr'Ozch Bio-Material - Type 12", "Burst and Full Auto");
            clump.Weapons.Add(new VhBioWeapon("Carbine", 1, 99, 254500, 254501));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 100, 199, 254502, 254503));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 200, 299, 254504, 254505));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 300, 300, 254506, 254506));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 1, 99, 254535, 254536));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 100, 199, 254537, 254538));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 200, 299, 254539, 254540));
            clump.Weapons.Add(new VhBioWeapon("Submachine Gun", 300, 300, 254541, 254541));
            this._clumps.Add(247716, clump);

            clump = new VhBioClump(13, "Kyr'Ozch Bio-Material - Type 13", "Burst, Fling Shot and Full Auto");
            clump.Weapons.Add(new VhBioWeapon("Carbine", 1, 99, 254507, 254508));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 100, 199, 254509, 254510));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 200, 299, 254511, 254512));
            clump.Weapons.Add(new VhBioWeapon("Carbine", 300, 300, 254513, 254513));
            this._clumps.Add(247720, clump);

            clump = new VhBioClump(76, "Kyr'Ozch Bio-Material - Type 76", "Brawl and Fast Attack");
            clump.Weapons.Add(new VhBioWeapon("Energy Sword", 1, 99, 254745, 254746));
            clump.Weapons.Add(new VhBioWeapon("Energy Sword", 100, 199, 254747, 254748));
            clump.Weapons.Add(new VhBioWeapon("Energy Sword", 200, 299, 254749, 254750));
            clump.Weapons.Add(new VhBioWeapon("Energy Sword", 300, 300, 254751, 254751));
            clump.Weapons.Add(new VhBioWeapon("Sledgehammer", 1, 99, 254731, 254732));
            clump.Weapons.Add(new VhBioWeapon("Sledgehammer", 100, 199, 254733, 254734));
            clump.Weapons.Add(new VhBioWeapon("Sledgehammer", 200, 299, 254735, 254736));
            clump.Weapons.Add(new VhBioWeapon("Sledgehammer", 300, 300, 254737, 254737));
            this._clumps.Add(247698, clump);

            clump = new VhBioClump(112, "Kyr'Ozch Bio-Material - Type 112", "Brawl, Dimach and Fast Attack");
            clump.Weapons.Add(new VhBioWeapon("Energy Hammer", 1, 99, 254703, 254704));
            clump.Weapons.Add(new VhBioWeapon("Energy Hammer", 100, 199, 254705, 254706));
            clump.Weapons.Add(new VhBioWeapon("Energy Hammer", 200, 299, 254707, 254708));
            clump.Weapons.Add(new VhBioWeapon("Energy Hammer", 300, 300, 254709, 254709));
            clump.Weapons.Add(new VhBioWeapon("Hammer", 1, 99, 254675, 254676));
            clump.Weapons.Add(new VhBioWeapon("Hammer", 100, 199, 254677, 254678));
            clump.Weapons.Add(new VhBioWeapon("Hammer", 200, 299, 254679, 254680));
            clump.Weapons.Add(new VhBioWeapon("Hammer", 300, 300, 254681, 254681));
            clump.Weapons.Add(new VhBioWeapon("Spear", 1, 99, 254780, 254781));
            clump.Weapons.Add(new VhBioWeapon("Spear", 100, 199, 254782, 254783));
            clump.Weapons.Add(new VhBioWeapon("Spear", 200, 299, 254784, 254785));
            clump.Weapons.Add(new VhBioWeapon("Spear", 300, 300, 254786, 254786));
            clump.Weapons.Add(new VhBioWeapon("Sword", 1, 99, 254759, 254760));
            clump.Weapons.Add(new VhBioWeapon("Sword", 100, 199, 254761, 254762));
            clump.Weapons.Add(new VhBioWeapon("Sword", 200, 299, 254763, 254764));
            clump.Weapons.Add(new VhBioWeapon("Sword", 300, 300, 254765, 254765));
            this._clumps.Add(247700, clump);

            clump = new VhBioClump(240, "Kyr'Ozch Bio-Material - Type 240", "Brawl, Dimach, Fast Attack and Sneak Attack");
            clump.Weapons.Add(new VhBioWeapon("Axe", 1, 99, 254689, 254690));
            clump.Weapons.Add(new VhBioWeapon("Axe", 100, 199, 254691, 254692));
            clump.Weapons.Add(new VhBioWeapon("Axe", 200, 299, 254693, 254694));
            clump.Weapons.Add(new VhBioWeapon("Axe", 300, 300, 254695, 254695));
            this._clumps.Add(247702, clump);

            clump = new VhBioClump(880, "Kyr'Ozch Bio-Material - Type 880", "Dimach, Fast Attack, Parry and Riposte");
            clump.Weapons.Add(new VhBioWeapon("Sword", 1, 99, 254766, 254767));
            clump.Weapons.Add(new VhBioWeapon("Sword", 100, 199, 254768, 254769));
            clump.Weapons.Add(new VhBioWeapon("Sword", 200, 299, 254770, 254771));
            clump.Weapons.Add(new VhBioWeapon("Sword", 300, 300, 254772, 254772));
            this._clumps.Add(247704, clump);

            clump = new VhBioClump(992, "Kyr'Ozch Bio-Material - Type 992", "Dimach, Fast Attack, Sneak Attack, Parry and Riposte");
            clump.Weapons.Add(new VhBioWeapon("Energy Rapier", 1, 99, 254717, 254718));
            clump.Weapons.Add(new VhBioWeapon("Energy Rapier", 100, 199, 254719, 254720));
            clump.Weapons.Add(new VhBioWeapon("Energy Rapier", 200, 299, 254721, 254722));
            clump.Weapons.Add(new VhBioWeapon("Energy Rapier", 300, 300, 254723, 254723));
            this._clumps.Add(247706, clump);
        }

        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Items.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: bio [bio material]");
                return;
            }
            foreach (AoItem match in e.Items)
            {
                if (this._clumps.ContainsKey(match.HighID))
                {
                    string name = "QL " + match.QL + " " + this._clumps[match.HighID].Clump;
                    if (this._clumps[match.HighID].Weapons.Count > 0)
                    {
                        double hql = Math.Floor((match.QL * 1.1));
                        if (hql > 300) hql = 300;
                        RichTextWindow window = new RichTextWindow(bot);
                        window.AppendTitle("Kyr'Ozch Weapons");
                        window.AppendHighlight("The following weapons can be created using your " + name + ":");
                        window.AppendLineBreak();
                        foreach (VhBioWeapon weapon in this._clumps[match.HighID].Weapons)
                        {
                            if (weapon.HighQL >= hql && weapon.LowQL <= hql)
                            {
                                window.AppendNormal("- ");
                                window.AppendItem("Kyr'Ozch " + weapon.Name + " - Type " + this._clumps[match.HighID].Type, weapon.LowID, weapon.HighID, Convert.ToInt32(hql));
                                window.AppendLineBreak();
                            }
                        }
                        bot.SendReply(e, name + " (" + this._clumps[match.HighID].Description + ")" + " »» ", window);
                    }
                    else
                    {
                        bot.SendReply(e, name + " (" + this._clumps[match.HighID].Description + ")");
                    }
                }
                else if (match.Name == "Solid Clump of Kyr'Ozch Bio-Material")
                {
                    bot.SendReply(e, "Unable to identify your " + HTML.CreateColorString(bot.ColorHeaderHex, match.Name));
                }
                else
                {
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, match.Name) + " doesn't look like a " + HTML.CreateColorString(bot.ColorHeaderHex, "Solid Clump of Kyr'Ozch Bio-Material"));
                }
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "bio":
                    return "Identifies the given Kyr'Ozch Bio-Material and displays the possible weapons that can be created.\n" +
                        "Usage: /tell " + bot.Character + " bio [bio material]";
            }
            return null;
        }
    }

    public class VhBioClump
    {
        public readonly List<VhBioWeapon> Weapons = new List<VhBioWeapon>();
        public readonly int Type;
        public readonly string Clump;
        public readonly string Description;

        public VhBioClump(int type, string clump, string description)
        {
            this.Type = type;
            this.Clump = clump;
            this.Description = description;
        }
    }

    public class VhBioWeapon
    {
        public readonly string Name;
        public readonly int LowQL;
        public readonly int HighQL;
        public readonly int LowID;
        public readonly int HighID;

        public VhBioWeapon(string name, int lowql, int highql, int lowid, int highid)
        {
            this.Name = name;
            this.LowQL = lowql;
            this.HighQL = highql;
            this.LowID = lowid;
            this.HighID = highid;
        }
    }
}