using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class VhInspect : PluginBase
    {
        public VhInspect()
        {
            this.Name = "Item Inspector";
            this.InternalName = "VhInspect";
            this.Version = 100;
            this.Author = "Neksus";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("inspect", true, UserLevel.Guest)
            };
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Items.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: inspect [item]");
                return;
            }
            foreach (AoItem item in e.Items)
            {
                string result = string.Empty;
                switch (item.HighID)
                {
                    case 205842:
                        result = "Funny Arrow";
                        break;
                    case 205843:
                        result = "Monster Sunglasses";
                        break;
                    case 205844:
                        result = "Karlsson Propellor Cap";
                        break;
                    case 216286:
                        result = "Funk Flamingo Sunglasses or Disco Duck Sunglasses or Electric Boogie Sunglasses or Gurgling River Sprite";
                        break;
                    case 245658:
                        result = "Blackpack";
                        break;
                    case 245596:
                        result = "Doctor's Pill Pack";
                        break;
                    case 245594:
                        result = "Syndicate Shades";
                        break;
                    case 269800:
                        result = "Galactic Jewel of the Infinite Moebius (HP/Nano Resist/Max Nano) ";
                        break;
                    case 269811:
                        result = "Galactic Jewel of the Bruised Brawler (HP/+add Melee dam)";
                        break;
                    case 269812:
                        result = "Galactic Jewel of the Searing Desert (HP/+add Fire dam)";
                        break;
                    case 269813:
                        result = "Galactic Jewel of the Frozen Tundra (HP/+add Cold dam)";
                        break;
                    case 269814:
                        result = "Galactic Jewel of the Jagged Landscape (HP/+add Projectile dam)";
                        break;
                    case 269815:
                        result = "Galactic Jewel of the Silent Killer (HP/+add Poison dam)";
                        break;
                    case 269816:
                        result = "Galactic Jewel of the Frail Juggernaut (HP low jewel)";
                        break;
                    case 269817:
                        result = "Galactic Jewel of the Icy Tundra (HP/+add Cold dam, low jewel)";
                        break;
                    case 269818:
                        result = "Galactic Jewel of the Craggy Landscape (HP/+add Projectile dam, low jewel)";
                        break;
                    case 269819:
                        result = "Galactic Jewel of the Scarlet Sky (HP/+add Radiation dam, low jewel)";
                        break;
                    case 270000:
                        result = "Robe of City Lights";
                        break;
                }
                if (result != string.Empty) bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, item.Name) + " contains " + HTML.CreateColorString(bot.ColorHeaderHex, result));
                else bot.SendReply(e, "Unable to identify " + HTML.CreateColorString(bot.ColorHeaderHex, item.Name));
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "inspect":
                    return "Inspects 'Christmas Gift', 'Light Perennium Container', 'Expensive Gift from Earth' and 'Frozen Crystal Compound'.\n" +
                        "Usage: /tell " + bot.Character + " inspect [item]";
            }
            return null;
        }
    }
}