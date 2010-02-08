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

    #region PocketBoss XML
    [XmlRoot("data")]
    public class VhPocketBoss_Symbs
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("revision")]
        public string Revision;
        [XmlAttribute("updated")]
        public string Updated;
        [XmlElement("entry")]
        public VhPocketBoss_Symbs_Entry[] Entries;
    }
    public class VhPocketBoss_Symbs_Entry
    {
        [XmlAttribute("ql")]
        public int QL;
        [XmlAttribute("slot")]
        public string Slot;
        [XmlAttribute("unit")]
        public string Unit;
        [XmlAttribute("pb")]
        public string PB;
        [XmlAttribute("loc")]
        public string Loc;
        [XmlAttribute("genLoc")]
        public string GenLoc;
        [XmlAttribute("mob")]
        public string Mob;
        [XmlAttribute("mobLevel")]
        public string MobLevel;
        [XmlAttribute("symbID")]
        public int SymbID;
        [XmlAttribute("symbName")]
        public string SymbName;
    }
    #endregion

    public class VhPocketBoss : PluginBase
    {
        private readonly string _urlSymbs = @"http://items.vhabot.net/symbiants.xml";
        private readonly string _dataPath = "data";

        public VhPocketBoss()
        {
            this.Name = "Pocket Bosses";
            this.InternalName = "VhPocketBoss";
            this.Author = "Tsuyoi";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;
            this.Description = "Gives information on Pocket Bosses and Symbiant Drop Locations.\nThis plugin is a port from the BeBot 'PB' module.";
            this.Commands = new Command[]
            {
                new Command("symb", true, UserLevel.Guest),
                new Command("symbiant", "symb"),
                new Command("pb", true, UserLevel.Guest),
                new Command("pocket", "pb"),
                new Command("pocketboss", "pb")
            };
        }
        public override void OnLoad(BotShell bot)
        {
            this.Download("symbs.xml", this._urlSymbs);
        }

        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "symb":
                    this.OnSymbCommand(bot, e);
                    break;
                case "pb":
                    this.OnPBCommand(bot, e);
                    break;
            }
        }

        public void OnSymbCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: symb [unit] [slot]");
                return;
            }
            else
            {
                string unit = e.Args[0].ToLower();
                string slot = e.Args[1].ToLower();
                if (e.Args.Length == 3)
                {
                    slot = e.Args[1].ToLower() + " " + e.Args[2].ToLower();
                }
                switch (slot)
                {
                    case "right arm":
                        slot = "rarm";
                        break;
                    case "right-arm":
                        slot = "rarm";
                        break;
                    case "right wrist":
                        slot = "rwrist";
                        break;
                    case "right-wrist":
                        slot = "rwrist";
                        break;
                    case "right hand":
                        slot = "rhand";
                        break;
                    case "right-hand":
                        slot = "rhand";
                        break;
                    case "left arm":
                        slot = "larm";
                        break;
                    case "left-arm":
                        slot = "larm";
                        break;
                    case "left wrist":
                        slot = "lwrist";
                        break;
                    case "left-wrist":
                        slot = "lwrist";
                        break;
                    case "left hand":
                        slot = "lhand";
                        break;
                    case "left-hand":
                        slot = "lhand";
                        break;
                    case "brain":
                        slot = "head";
                        break;
                    case "ocular":
                        slot = "eye";
                        break;
                }
                int matches = 0;
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Symbiant Results");
                foreach (VhPocketBoss_Symbs_Entry Entry in this.GetSymbs().Entries)
                {
                    if ((Entry.Unit.IndexOf(unit, 0) >= 0) && (Entry.Slot.IndexOf(slot,0) >= 0))
                    {
                        matches++;
                        window.AppendItem(Entry.SymbName, Entry.SymbID, Entry.SymbID, Entry.QL);
                        window.AppendLineBreak();
                        window.AppendNormal("Dropped by: ");
                        window.AppendBotCommand(Entry.PB, "pb " + Entry.PB);
                        window.AppendLineBreak(2);
                    }
                }
                if (matches > 0)
                {
                    bot.SendReply(e, "Found " + HTML.CreateColorString(bot.ColorHeaderHex, matches.ToString()) + " Symbiants »» ", window);
                    return;
                }
                else
                {
                    bot.SendReply(e, "Could not find any Pocketbosses for " + HTML.CreateColorString(bot.ColorHeaderHex, unit) + " " + HTML.CreateColorString(bot.ColorHeaderHex, slot) + ".");
                }
            }
        }

        public void OnPBCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: pb [pb]");
                return;
            }
            else
            {
                string pb = e.Args[0];
                if (e.Args.Length == 2)
                {
                    pb = e.Args[0] + " " + e.Args[1];
                }
                if (e.Args.Length == 3)
                {
                    pb = e.Args[0] + " " + e.Args[1] + " " + e.Args[2];
                }
                if (e.Args.Length == 4)
                {
                    pb = e.Args[0] + " " + e.Args[1] + " " + e.Args[2] + " " + e.Args[3];
                }
                pb = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pb);
                pb = pb.Replace("Of", "of");
                pb = pb.Replace("Xark The", "Xark the");
                foreach (VhPocketBoss_Symbs_Entry Entry in this.GetSymbs().Entries)
                {
                    if (Entry.PB.IndexOf(pb,0) >= 0)
                    {
                        RichTextWindow window = new RichTextWindow(bot);
                        window.AppendTitle("Remains of " + Entry.PB);
                        window.AppendHighlight("Location: ");
                        window.AppendNormal(Entry.Loc);
                        window.AppendLineBreak();
                        window.AppendHighlight("Found On: ");
                        window.AppendNormal(Entry.Mob);
                        window.AppendLineBreak();
                        window.AppendHighlight("Mob Level: ");
                        window.AppendNormal(Entry.MobLevel);
                        window.AppendLineBreak();
                        window.AppendHighlight("General Location: ");
                        window.AppendNormal(Entry.GenLoc);
                        window.AppendLineBreak(2);
                        window.AppendHeader("Results");

                        foreach (VhPocketBoss_Symbs_Entry subEntry in this.GetSymbs().Entries)
                        {
                            if (Entry.PB == subEntry.PB)
                            {
                                window.AppendItem(subEntry.SymbName, subEntry.SymbID, subEntry.SymbID, subEntry.QL);
                                window.AppendLineBreak();
                            }
                        }

                        bot.SendReply(e, "Remains of " + HTML.CreateColorString(bot.ColorHeaderHex, Entry.PB) + " »» ", window);
                        return;
                    }
                }
                bot.SendReply(e, "Could not find the Pocketboss \"" + HTML.CreateColorString(bot.ColorHeaderHex, pb) + "\"");
                return;
            }
        }

        public VhPocketBoss_Symbs GetSymbs() { return (VhPocketBoss_Symbs)this.ParseXml(typeof(VhPocketBoss_Symbs), "symbs.xml"); }

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

        public bool Download(string file, string url)
        {
            file = this._dataPath + Path.DirectorySeparatorChar + file;
            try
            {
                if (!Directory.Exists(this._dataPath))
                {
                    Directory.CreateDirectory(this._dataPath);
                }
                using (WebClient Client = new WebClient())
                {
                    Client.DownloadFile(url, file);
                    if (File.Exists(file)) return true;
                }
            }
            catch { }
            return false;
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "symb":
                    return "Displays information about where this symbaint drops.\n" +
                        "Usage: /tell " + bot.Character + " symb [unit] [slot]";
                case "pb":
                    return "Displays information about this Pocket Boss.\n" +
                        "Usage: /tell " + bot.Character + " pb [pb]";
            }
            return null;
        }
    }
}