using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class BreedStats
    {
        private string[] name;
        private int minStr;
        private int minAgil;
        private int minSta;
        private int minInt;
        private int minSen;
        private int minPsy;
        private int maxStr;
        private int maxAgil;
        private int maxSta;
        private int maxInt;
        private int maxSen;
        private int maxPsy;
        private string colorStr;
        private string colorAgil;
        private string colorSta;
        private string colorInt;
        private string colorSen;
        private string colorPsy;
        private int nanoCap;
        private int hpPerBD;

        public BreedStats(string[] Name,
            int MinStr, int MaxStr, string ColorStr,
            int MinAgil, int MaxAgil, string ColorAgil,
            int MinSta, int MaxSta, string ColorSta,
            int MinInt, int MaxInt, string ColorInt,
            int MinSen, int MaxSen, string ColorSen,
            int MinPsy, int MaxPsy, string ColorPsy,
            int NanoCap, int HPPerBD)
        {
            name = Name;
            minStr = MinStr;
            maxStr = MaxStr;
            colorStr = ColorStr;
            minAgil = MinAgil;
            maxAgil = MaxAgil;
            colorAgil = ColorAgil;
            minSta = MinSta;
            maxSta = MaxSta;
            colorSta = ColorSta;
            minInt = MinInt;
            maxInt = MaxInt;
            colorInt = ColorInt;
            minSen = MinSen;
            maxSen = MaxSen;
            colorSen = ColorSen;
            minPsy = MinPsy;
            maxPsy = MaxPsy;
            colorPsy = ColorPsy;
            nanoCap = NanoCap;
            hpPerBD = HPPerBD;
        }

        public string[] Name { get { return name; } }
        public string MinStr { get { return minStr.ToString(); } }
        public string MaxStr { get { return maxStr.ToString(); } }
        public string ColorStr { get { return colorStr; } }

        public string MinAgil { get { return minAgil.ToString(); } }
        public string MaxAgil { get { return maxAgil.ToString(); } }
        public string ColorAgil { get { return colorAgil; } }

        public string MinSta { get { return minSta.ToString(); } }
        public string MaxSta { get { return maxSta.ToString(); } }
        public string ColorSta { get { return colorSta; } }

        public string MinInt { get { return minInt.ToString(); } }
        public string MaxInt { get { return maxInt.ToString(); } }
        public string ColorInt { get { return colorInt; } }

        public string MinSen { get { return minSen.ToString(); } }
        public string MaxSen { get { return maxSen.ToString(); } }
        public string ColorSen { get { return colorSen; } }

        public string MinPsy { get { return minPsy.ToString(); } }
        public string MaxPsy { get { return maxPsy.ToString(); } }
        public string ColorPsy { get { return colorPsy; } }

        public string NanoCap { get { return nanoCap.ToString(); } }
        public string HPPerBD { get { return hpPerBD.ToString(); } }
    }


    public class VhBreed : PluginBase
    {
        private BreedStats[] Breeds;

        public VhBreed()
        {
            this.Name = "Breed Caps";
            this.InternalName = "VhBreed";
            this.Author = "Kevma";
            this.Version = 100;
            this.Description = "Gives information on breed caps.";
            this.DefaultState = PluginState.Installed;
            this.Commands = new Command[] {
                new Command("breed", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            Breeds = new BreedStats[] {new BreedStats(new string[] { "Atrox", "trox" }, 512, 912, "Green", 480, 780, "Blue", 512, 912, "Green", 400, 600, "Dark Blue", 400, 600, "Dark Blue", 400, 600, "Dark Blue", 45, 4),
            new BreedStats(new string[] { "Solitus", "soli", "sol" }, 472, 772, "Blue", 480, 780, "Blue", 480, 780, "Blue", 480, 780, "Blue", 480, 780, "Blue", 480, 780, "Blue", 50, 3),
            new BreedStats(new string[] { "Opifex", "opi", "fex" }, 464, 764, "Blue", 544, 944, "Green", 480, 680, "Dark Blue", 464, 764, "Blue", 512, 912, "Green", 448, 748, "Blue", 50, 3),
            new BreedStats(new string[] { "Nanomage", "nano", "mage", "nm" }, 464, 664, "Dark Blue", 464, 664, "Blue", 448, 748, "Blue", 512, 912, "Green", 480, 780, "Dark Blue", 512, 912, "Dark Blue", 55, 2)};
        }

        public override void OnUnload(BotShell bot)
        {
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length == 0)
            {
                bot.SendReply(e, "Usage: breed [breed]");
                return;
            }
            else
            {
                foreach (BreedStats Breed in Breeds)
                {
                    foreach (string Name in Breed.Name)
                    {
                        if (Name.ToLower() == e.Args[0].ToLower())
                        {
                            /*
                             *  Strength     - ### / ###
                             *  Agility      - ### / ###
                             *  Stamina      - ### / ###
                             *  Intelligence - ### / ###
                             *  Sense        - ### / ###
                             *  Psychic      - ### / ###
                             *
                             *  NanoCost%    - ##%
                             *  HP / BodyDev - #
                             */
                            RichTextWindow window = new RichTextWindow(bot);
                            window.AppendTitle(Breed.Name[0]);
                            window.AppendLineBreak();
                            window.AppendHighlight("Breed Caps - (Normal Caps / Shadow Level Caps)");
                            window.AppendLineBreak(2);
                            window.AppendColorString(this.StatColor(Breed.ColorStr), "Strength");
                            window.AppendNormal(" - " + Breed.MinStr + " / " + Breed.MaxStr);
                            window.AppendLineBreak();
                            window.AppendColorString(this.StatColor(Breed.ColorAgil), "Agility");
                            window.AppendNormal(" - " + Breed.MinAgil + " / " + Breed.MaxAgil);
                            window.AppendLineBreak();
                            window.AppendColorString(this.StatColor(Breed.ColorSta), "Stamina");
                            window.AppendNormal(" - " + Breed.MinSta + " / " + Breed.MaxSta);
                            window.AppendLineBreak();
                            window.AppendColorString(this.StatColor(Breed.ColorInt), "Intelligence");
                            window.AppendNormal(" - " + Breed.MinInt + " / " + Breed.MaxInt);
                            window.AppendLineBreak();
                            window.AppendColorString(this.StatColor(Breed.ColorSen), "Sense");
                            window.AppendNormal(" - " + Breed.MinSen + " / " + Breed.MaxSen);
                            window.AppendLineBreak();
                            window.AppendColorString(this.StatColor(Breed.ColorPsy), "Psychic");
                            window.AppendNormal(" - " + Breed.MinPsy + " / " + Breed.MaxPsy);
                            window.AppendLineBreak(2);
                            window.AppendHighlight("NanoCost%");
                            window.AppendNormal(" - " + Breed.NanoCap + "%");
                            window.AppendLineBreak();
                            window.AppendHighlight("HP / BodyDev");
                            window.AppendNormal(" - " + Breed.HPPerBD);
                            bot.SendReply(e, Breed.Name[0] + " Stats »» ", window);
                            return;
                        }
                    }
                }
                bot.SendReply(e, "No results found for \"" + e.Words[0] + "\"!");
                return;
            }
        }

        public string StatColor(string Color)
        {
            switch (Color)
            {
                case "Green":
                    return "00FF00";
                case "Blue":
                    return "00CCFF";
                case "Dark Blue":
                    return "0066FF";
                default:
                    return "FFFFFF";
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "breed":
                    return "Lists the caps of all the breeds\n" +
                        "Usage: /tell " + bot.Character + " breed [breed]";
            }
            return null;
        }
    }
}
