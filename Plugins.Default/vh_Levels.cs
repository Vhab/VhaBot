using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.IO;
using System.Net;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{

    #region Levels XML
    [XmlRoot("data")]
    public class VhLevels_Levels
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("revision")]
        public string Revision;
        [XmlAttribute("updated")]
        public string Updated;
        [XmlElement("entry")]
        public VhLevels_Levels_Entry[] Entries;
    }

    public class VhLevels_Levels_Entry
    {
        [XmlAttribute("level")]
        public int Level;
        [XmlAttribute("teamMin")]
        public int TeamMin;
        [XmlAttribute("teamMax")]
        public int TeamMax;
        [XmlAttribute("pvpMin")]
        public int PvpMin;
        [XmlAttribute("pvpMax")]
        public int PvpMax;
        [XmlAttribute("xpsk")]
        public int Experience;
        [XmlAttribute("missions")]
        public string _missions;
        public List<string> Missions
        {
            get
            {
                if (this._missions == null) return null;
                return new List<string>(this._missions.Split(','));
            }
        }
    }
    #endregion
    #region Defender Ranks XML
    [XmlRoot("data")]
    public class VhLevels_DefenderRanks
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("revision")]
        public string Revision;
        [XmlAttribute("updated")]
        public string Updated;
        [XmlElement("entry")]
        public VhLevels_DefenderRanks_Entry[] Entries;
    }

    public class VhLevels_DefenderRanks_Entry
    {
        [XmlAttribute("id")]
        public int ID;
        [XmlAttribute("rank")]
        public string Rank;
        [XmlAttribute("xp")]
        public int Experience;
    }
    #endregion
    #region Research Levels XML
    [XmlRoot("data")]
    public class VhLevels_ResearchLevels
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("revision")]
        public string Revision;
        [XmlAttribute("updated")]
        public string Updated;
        [XmlElement("entry")]
        public VhLevels_ResearchLevels_Entry[] Entries;
    }

    public class VhLevels_ResearchLevels_Entry
    {
        [XmlAttribute("level")]
        public int Level;
        [XmlAttribute("research")]
        public int Research;
    }
    #endregion

    public class VhLevels : PluginBase
    {
        private readonly string _dataPath = "data";

        public VhLevels()
        {
            this.Name = "Levels and Experience";
            this.InternalName = "VhLevels";
            this.Author = "Naturalistic / Iriche / Vhab";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Description = "Levels and experience calculations and information.\n'Tokens' was originally developed for BeBot by Siocuffin (rk1).";
            this.Commands = new Command[]
            {
                new Command("xp", true, UserLevel.Guest),
                new Command("sk","xp"),
                new Command("level", true, UserLevel.Guest),
                new Command("l","level"),
                new Command("lvl","level"),
                new Command("mission", true, UserLevel.Guest),
                new Command("axp", true, UserLevel.Guest),
                new Command("research", true, UserLevel.Guest),
                new Command("rs","research"),
                new Command("tokens", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot) { }

        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "level":
                    this.OnLevelCommand(bot, e);
                    break;
                case "mission":
                    this.OnMissionCommand(bot, e);
                    break;
                case "xp":
                    this.OnXpCommand(bot, e);
                    break;
                case "axp":
                    this.OnAxpCommand(bot, e);
                    break;
                case "research":
                    this.OnResearchCommand(bot, e);
                    break;
                case "tokens":
                    this.OnTokensCommand(bot, e);
                    break;
            }
        }

        public void OnLevelCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: level [level]");
                return;
            }
            foreach (VhLevels_Levels_Entry entry in this.GetLevels().Entries)
            {
                if (entry.Level.ToString() == e.Args[0])
                {
                    string message = "";
                    message = "L " + entry.Level + ": ";
                    message += "Team " + entry.TeamMin + "-" + entry.TeamMax + " | ";
                    message += "PvP " + entry.PvpMin + "-" + entry.PvpMax + " | ";
                    message += entry.Experience;
                    if (entry.Level > 200)
                        message += " SK";
                    else
                        message += " XP";
                    message += " | ";
                    message += "Missions ";
                    message += string.Join(", ", entry.Missions.ToArray());
                    bot.SendReply(e, message);
                    return;
                }
            }
            bot.SendReply(e, "Invalid level specified");
        }

        public void OnMissionCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: mission [ql]");
                return;
            }
            List<string> levels = new List<string>();
            foreach (VhLevels_Levels_Entry entry in this.GetLevels().Entries)
            {
                if (entry.Missions.Contains(e.Args[0]))
                {
                    levels.Add(entry.Level.ToString());
                }
            }
            if (levels.Count < 1)
            {
                bot.SendReply(e, "Invalid QL specified");
                return;
            }
            bot.SendReply(e, "Players with the following level should be able to pull a " + HTML.CreateColorString(bot.ColorHeaderHex, "QL " + e.Args[0]) + " mission: " + bot.ColorHeader + string.Join(", ", levels.ToArray()));
        }

        public void OnXpCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: xp [level] [level]");
                return;
            }
            int from = -1;
            int to = -1;
            if (e.Args.Length == 1)
            {
                if (!int.TryParse(e.Args[0], out to) || to < 2 || to > 220)
                {
                    bot.SendReply(e, "Invalid level specified");
                    return;
                }
                from = to - 1;
            }
            else
            {
                if (!int.TryParse(e.Args[0], out from) || !int.TryParse(e.Args[1], out to))
                {
                    bot.SendReply(e, "Invalid level specified");
                    return;
                }
                if (to <= from)
                {
                    bot.SendReply(e, "Invalid level range specified");
                    return;
                }
                if (to < 2 || to > 220 || from < 1 || from > 219)
                {
                    bot.SendReply(e, "Invalid level specified");
                    return;
                }
            }
            int xp = 0;
            int sk = 0;
            foreach (VhLevels_Levels_Entry entry in this.GetLevels().Entries)
            {
                if (entry.Level >= from && entry.Level < to)
                {
                    if (entry.Level >= 200) sk += entry.Experience;
                    else xp += entry.Experience;
                }
            }
            string message = "From level " + HTML.CreateColorString(bot.ColorHeaderHex, from.ToString()) + " to " + HTML.CreateColorString(bot.ColorHeaderHex, to.ToString()) + " you need ";
            if (xp > 0) message += HTML.CreateColorString(bot.ColorHeaderHex, xp.ToString("0,0") + " xp");
            if (xp > 0 && sk > 0) message += " and ";
            if (sk > 0) message += HTML.CreateColorString(bot.ColorHeaderHex, sk.ToString("0,0") + " sk");
            bot.SendReply(e, message);
        }

        public void OnAxpCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: axp [rank] [rank]");
                return;
            }
            int from = -1;
            int to = -1;
            if (e.Args.Length == 1)
            {
                if (!int.TryParse(e.Args[0], out to) || to < 1 || to > 30)
                {
                    bot.SendReply(e, "Invalid rank specified");
                    return;
                }
                from = to - 1;
            }
            else
            {
                if (!int.TryParse(e.Args[0], out from) || !int.TryParse(e.Args[1], out to))
                {
                    bot.SendReply(e, "Invalid rank specified");
                    return;
                }
                if (to <= from)
                {
                    bot.SendReply(e, "Invalid rank range specified");
                    return;
                }
                if (to < 1 || to > 30 || from < 0 || from > 29)
                {
                    bot.SendReply(e, "Invalid rank specified");
                    return;
                }
            }
            int xp = 0;
            foreach (VhLevels_DefenderRanks_Entry entry in this.GetDefenderRanks().Entries)
            {
                if (entry.ID >= from && entry.ID < to)
                {
                    xp += entry.Experience;
                }
            }
            bot.SendReply(e, "From defender rank " + HTML.CreateColorString(bot.ColorHeaderHex, from.ToString()) + " to " + HTML.CreateColorString(bot.ColorHeaderHex, to.ToString()) + " you need " + HTML.CreateColorString(bot.ColorHeaderHex, xp.ToString("0,0")) + " alien experience");
        }

        public void OnResearchCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: research [level] [level]");
                return;
            }
            int from = -1;
            int to = -1;
            if (e.Args.Length == 1)
            {
                if (!int.TryParse(e.Args[0], out to) || to < 1 || to > 10)
                {
                    bot.SendReply(e, "Invalid level specified");
                    return;
                }
                from = to - 1;
            }
            else
            {
                if (!int.TryParse(e.Args[0], out from) || !int.TryParse(e.Args[1], out to))
                {
                    bot.SendReply(e, "Invalid level specified");
                    return;
                }
                if (to <= from)
                {
                    bot.SendReply(e, "Invalid level range specified");
                    return;
                }
                if (to < 1 || to > 10 || from < 0 || from > 9)
                {
                    bot.SendReply(e, "Invalid level specified");
                    return;
                }
            }
            int xp = 0;
            foreach (VhLevels_ResearchLevels_Entry entry in this.GetResearchLevels().Entries)
            {
                if (entry.Level >= from && entry.Level < to)
                {
                    xp += entry.Research;
                }
            }
            bot.SendReply(e, "From research level " + HTML.CreateColorString(bot.ColorHeaderHex, from.ToString()) + " to " + HTML.CreateColorString(bot.ColorHeaderHex, to.ToString()) + " you need " + HTML.CreateColorString(bot.ColorHeaderHex, xp.ToString("0,0")) + " research points");
        }

        public VhLevels_Levels GetLevels() { return (VhLevels_Levels)this.ParseXml(typeof(VhLevels_Levels), "levels.xml"); }
        public VhLevels_DefenderRanks GetDefenderRanks() { return (VhLevels_DefenderRanks)this.ParseXml(typeof(VhLevels_DefenderRanks), "dr.xml"); }
        public VhLevels_ResearchLevels GetResearchLevels() { return (VhLevels_ResearchLevels)this.ParseXml(typeof(VhLevels_ResearchLevels), "research.xml"); }

        private object ParseXml(Type type, string file)
        {
            try
            {
                using (FileStream stream = File.OpenRead(this._dataPath + Path.DirectorySeparatorChar + file))
                {
                    XmlSerializer serializer = new XmlSerializer(type);
                    return serializer.Deserialize(stream);
                }
            }
            catch { return null; }
        }

        public void OnTokensCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                // Tokens overview
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Tokens Overview");
                window.AppendHighlight("Level 1-14: ");
                window.AppendNormal("1 token");
                window.AppendLineBreak();
                window.AppendHighlight("Level 15-49: ");
                window.AppendNormal("2 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Level 50-74: ");
                window.AppendNormal("3 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Level 75-99: ");
                window.AppendNormal("4 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Level 100-124: ");
                window.AppendNormal("5 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Level 125-149: ");
                window.AppendNormal("6 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Level 150-174: ");
                window.AppendNormal("7 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Level 175-189: ");
                window.AppendNormal("8 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Level 190-220: ");
                window.AppendNormal("9 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("Veteran tokens (7 veteran points): ");
                window.AppendNormal("50 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("OFAB tokens (1.000 victory points): ");
                window.AppendNormal("10 tokens");
                window.AppendLineBreak();
                window.AppendHighlight("OFAB tokens (10.000 victory points): ");
                window.AppendNormal("100 tokens");
                window.AppendLineBreak();

                bot.SendReply(e, "Tokens »» ", window);
                return;
            }
            else
            {
                // Validate and parse input
                if (e.Args.Length < 2)
                {
                    bot.SendReply(e, "Correct Usage: tokens [[level]] [current] [goal]");
                    return;
                }
                int level = 220;
                int current = 0;
                int goal = 0;
                if (e.Args.Length == 2)
                {
                    if (e.SenderWhois != null && e.SenderWhois.Success)
                        level = e.SenderWhois.Stats.Level;
                    if (!int.TryParse(e.Args[0], out current) || !int.TryParse(e.Args[1], out goal))
                    {
                        bot.SendReply(e, "Correct Usage: tokens [current] [goal]");
                        return;
                    }
                }
                else
                {
                    if (!int.TryParse(e.Args[0], out level) || !int.TryParse(e.Args[1], out current) || !int.TryParse(e.Args[2], out goal))
                    {
                        bot.SendReply(e, "Correct Usage: tokens [level] [current] [goal]");
                        return;
                    }
                }
                if (level > 220) level = 220;
                if (level < 1) level = 1;
                if (current < 0) current = 0;
                if (goal < 0) goal = 0;
                if (current >= goal)
                {
                    bot.SendReply(e, "Congratulations, you've already reached your goal");
                    return;
                }

                // Determine how much tokens
                int tokens = 1;
                if (level > 189) tokens = 9;
                else if (level > 174) tokens = 8;
                else if (level > 149) tokens = 7;
                else if (level > 124) tokens = 6;
                else if (level > 99) tokens = 5;
                else if (level > 74) tokens = 4;
                else if (level > 49) tokens = 3;
                else if (level > 14) tokens = 2;

                // Calculate requirements
                double need = goal - current;
                double step1 = need / (double)tokens;
                double step2 = (double)step1 / 7;
                double bags = Math.Ceiling(4 * step2);
                step1 = Math.Ceiling(step1);
                double vpToken = Math.Ceiling(need / 10);
                double vp = vpToken * 1000;
                double vp2 = vpToken * 10;
                double vetToken = Math.Ceiling(need / 50);
                double vet = vetToken * 7;
                double vet2 = vetToken * 50;

                // Format output
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Tokens Calculator Results");
                window.AppendHighlight("Level: ");
                window.AppendNormal(level.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Current amount of tokens: ");
                window.AppendNormal(current.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Goal amount of tokens: ");
                window.AppendNormal(goal.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Tokens needed: ");
                window.AppendNormal(need.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Tokens per mission token: ");
                window.AppendNormal(tokens.ToString());
                window.AppendLineBreak(2);

                window.AppendHeader("Requirements");
                window.AppendHighlight("Token bags: ");
                window.AppendNormal(bags.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Mission tokens: ");
                window.AppendNormal(step1.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("Veteran tokens ("+vet+" veteran points): ");
                window.AppendNormal(vetToken.ToString());
                window.AppendLineBreak();
                window.AppendHighlight("OFAB tokens (" + vp + " victory points): ");
                window.AppendNormal(vpToken.ToString());
                window.AppendLineBreak();

                bot.SendReply(e, "Tokens »» ", window);
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "level":
                    return "Displays information about the given level.\n" +
                        "Usage: /tell " + bot.Character + " level [level]";
                case "mission":
                    return "Displays the levels that are able to pull a mission of a certain QL.\n" +
                        "Usage: /tell " + bot.Character + " mission [ql]";
                case "xp":
                    return "Displays the required experience between a given range of levels.\n" +
                        "Usage: /tell " + bot.Character + " xp [level] [level]";
                case "axp":
                    return "Displays the required alien experience between a given range of defender ranks.\n" +
                        "Usage: /tell " + bot.Character + " axp [rank] [rank]";
                case "research":
                    return "Displays the required research points between a given range of research levels.\n" +
                        "Usage: /tell " + bot.Character + " research [level] [level]";
                case "tokens":
                    return "Calculates the required amount of tokens when given a level, current tokens and a goal.\n" +
                        "Usage: /tell " + bot.Character + " tokens [[level]] [current] [goal]";
            }
            return null;
        }
    }
}
