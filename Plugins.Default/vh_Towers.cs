using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using System.Data;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Net;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{
    #region LCA XML
    [XmlRoot("data")]
    public class VhTowers_LCAs
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("revision")]
        public string Revision;
        [XmlAttribute("updated")]
        public string Updated;
        [XmlElement("entry")]
        public VhTowers_LCAs_Entry[] Entries;
    }
    public class VhTowers_LCAs_Entry
    {
        [XmlAttribute("number")]
        public int Number;
        [XmlAttribute("min")]
        public int Min;
        [XmlAttribute("max")]
        public int Max;
        [XmlAttribute("zone")]
        public string Zone;
        [XmlAttribute("x")]
        public int X;
        [XmlAttribute("y")]
        public int Y;
        [XmlAttribute("name")]
        public string Name;
    }
    #endregion

    public class VhTowers : PluginBase
    {
        private Config _database;
        private bool _sendgc = true;
        private bool _sendpg = true;
        private readonly string _dataPath = "data";

        public VhTowers()
        {
            this.Name = "Tower-Wars Tracker";
            this.InternalName = "VhTowers";
            this.Author = "Tsuyoi";
            this.DefaultState = PluginState.Installed;
            this.Description = "Tracks and displays Tower-Wars notifications to Guildchat and PrivateGroup channels.";
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("battle", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest),
                new Command("victory", true, UserLevel.Guest, UserLevel.Member, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Events.ChannelMessageEvent += new ChannelMessageHandler(OnChannelMessageEvent);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(ConfigurationChangedEvent);
            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS towerhistory (history_id integer PRIMARY KEY AUTOINCREMENT, victory INT NOT NULL, time INT NOT NULL, atkrSide VARCHAR(10), atkrOrg VARCHAR(50), atkrName VARCHAR(20), defSide VARCHAR(10) NOT NULL, defOrg VARCHAR(50) NOT NULL, zone VARCHAR(50) NOT NULL, xCoord INT, yCoord INT, LCA INT)");
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendgc", "Send notifications to the organization channel", this._sendgc);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "sendpg", "Send notifications to the private channel", this._sendpg);
            this.LoadConfiguration(bot);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ChannelMessageEvent -= new ChannelMessageHandler(OnChannelMessageEvent);
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(ConfigurationChangedEvent);
        }

        private void ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section != this.InternalName) return;
            this.LoadConfiguration(bot);
        }

        private void LoadConfiguration(BotShell bot)
        {
            this._sendgc = bot.Configuration.GetBoolean(this.InternalName, "sendgc", this._sendgc);
            this._sendpg = bot.Configuration.GetBoolean(this.InternalName, "sendpg", this._sendpg);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "battle":
                    this.OnBattleCommand(bot, e);
                    break;
                case "victory":
                    this.OnVictoryCommand(bot, e);
                    break;
            }
        }

        private void OnChannelMessageEvent(BotShell bot, ChannelMessageArgs e)
        {
            if (e.Channel == "All Towers" || e.Channel == "Tower Battle Outcome")
            {
                if (e.IsDescrambled)
                {
                    if (e.Descrambled.CategoryID == 506)
                    {
                        switch (e.Descrambled.EntryID)
                        {
                            case 12753364: // The %s organization %s just entered a state of war! %s attacked the %s organization %s's tower in %s at location (%d, %d).
                                string atkrSide = e.Descrambled.Arguments[0].Message;
                                string atkrOrg = e.Descrambled.Arguments[1].Message;
                                string atkrName = e.Descrambled.Arguments[2].Message;
                                string defSide  = e.Descrambled.Arguments[3].Message;
                                string defOrg = e.Descrambled.Arguments[4].Message;
                                string zone = e.Descrambled.Arguments[5].Message;
                                int x_coord   = e.Descrambled.Arguments[6].Integer;
                                int y_coord   = e.Descrambled.Arguments[7].Integer;
                                int LCA = this.grabLCA(zone, x_coord, y_coord);

                                WhoisResult whois = XML.GetWhois(atkrName.ToLower(), bot.Dimension);
                                int atkrLvl = whois.Stats.Level;
                                int atkrAiLvl = whois.Stats.DefenderLevel;
                                string atkrProf = whois.Stats.Profession;
                                string message = HTML.CreateColorString(bot.ColorHeaderHex, atkrOrg) + " " + HTML.CreateColorString(bot.ColorHeaderHex, "(") +
                                    this.sideColor(atkrSide, atkrSide) + HTML.CreateColorString(bot.ColorHeaderHex, ")") + " has attacked " +
                                    HTML.CreateColorString(bot.ColorHeaderHex, defOrg) + " " + HTML.CreateColorString(bot.ColorHeaderHex, "(") +
                                    this.sideColor(defSide, defSide) + HTML.CreateColorString(bot.ColorHeaderHex, ")") + " in " + zone + " at " +
                                    HTML.CreateColorString(bot.ColorHeaderHex, "(x" + LCA.ToString() + " " + x_coord.ToString() + "x" + y_coord.ToString() +
                                    ")") + ". Attacker: " + HTML.CreateColorString(bot.ColorHeaderHex, atkrName) +
                                    " " + HTML.CreateColorString(bot.ColorHeaderHex, "(") + this.sideColor(atkrLvl + "/" + atkrAiLvl + " " + atkrProf, atkrSide) +
                                    HTML.CreateColorString(bot.ColorHeaderHex, ")");
                                this.sendMessage(bot, message);
                                try
                                {
                                    using (IDbCommand command = this._database.Connection.CreateCommand())
                                    {
                                        command.CommandText = "INSERT INTO [towerhistory] (victory, time, atkrSide, atkrOrg, atkrName, defSide, defOrg, zone, xCoord, yCoord, LCA) VALUES (0," +
                                            TimeStamp.Now + ", '" + atkrSide + "', '" + atkrOrg + "', '" + atkrName + "', '" + defSide + "', '" + defOrg + "', '" + zone + "', " + x_coord + ", " +
                                            y_coord + ", " + LCA + ")";
                                        IDataReader reader = command.ExecuteReader();
                                    }
                                }
                                catch
                                {
                                    this.sendMessage(bot, "Error during tower history archiving!");
                                }
                                break;
                            case 147506468: // Notum Wars Update: The %s organization %s lost their base in %s.
                                string lostSide = e.Descrambled.Arguments[0].Message;
                                string lostOrg = e.Descrambled.Arguments[1].Message;
                                string lostZone = e.Descrambled.Arguments[2].Message;
                                try
                                {
                                    using (IDbCommand command = this._database.Connection.CreateCommand())
                                    {
                                        command.CommandText = "INSERT INTO [towerhistory] (victory, time, defSide, defOrg, zone) VALUES (1, " +
                                            TimeStamp.Now + ", '" + lostSide + "', '" + lostOrg + "', '" + lostZone + "')";
                                        IDataReader reader = command.ExecuteReader();
                                    }
                                }
                                catch
                                {
                                    this.sendMessage(bot, "Error during tower history archiving!");
                                }
                                break;
                        }
                    }
                }
                else
                {
                    //%s just attacked the %s organization %s's tower in %s at location (%d, %d).
                    Regex soloAttack = new Regex(@"(.+)\sjust\sattacked\sthe\s([a-zA-Z]+)\sorganization\s(.+)'s\stower\sin\s(.+)\sat\slocation\s\(([0-9]+),\s([0-9]+)\)");
                    if (soloAttack.IsMatch(e.Message))
                    {
                        Match m = soloAttack.Match(e.Message);
                        string atkrName = m.Groups[1].Value;
                        string defSide = m.Groups[2].Value;
                        string defOrg = m.Groups[3].Value;
                        string zone = m.Groups[4].Value;
                        int x_coord;
                        int y_coord;
                        int.TryParse(m.Groups[5].Value, out x_coord);
                        int.TryParse(m.Groups[6].Value, out y_coord);
                        int LCA = this.grabLCA(zone, x_coord, y_coord);

                        WhoisResult whois = XML.GetWhois(atkrName.ToLower(), bot.Dimension);
                        int atkrLvl = whois.Stats.Level;
                        int atkrAiLvl = whois.Stats.DefenderLevel;
                        string atkrProf = whois.Stats.Profession;
                        string atkrSide = whois.Stats.Faction;
                        string message = HTML.CreateColorString(bot.ColorHeaderHex, atkrName) + " " + HTML.CreateColorString(bot.ColorHeaderHex, "(") +
                            this.sideColor(atkrLvl + "/" + atkrAiLvl + " " + atkrProf, atkrSide) + HTML.CreateColorString(bot.ColorHeaderHex, ")") +
                            " has attacked " + HTML.CreateColorString(bot.ColorHeaderHex, defOrg) + " " + HTML.CreateColorString(bot.ColorHeaderHex, "(") +
                            this.sideColor(defSide, defSide) + HTML.CreateColorString(bot.ColorHeaderHex, ")") + " in " + zone + " at " +
                            HTML.CreateColorString(bot.ColorHeaderHex, "(x" + LCA.ToString() + " " + x_coord.ToString() + "x" + y_coord.ToString() + ")") +
                            HTML.CreateColorString(bot.ColorHeaderHex, ")") + ".";
                        this.sendMessage(bot, message);
                        try
                        {
                            using (IDbCommand command = this._database.Connection.CreateCommand())
                            {
                                command.CommandText = "INSERT INTO [towerhistory] (victory, time, atkrSide, atkrName, defSide, defOrg, zone, xCoord, yCoord, LCA) VALUES (0," +
                                    TimeStamp.Now + ", '" + atkrSide + "', '" + atkrName + "', '" + defSide + "', '" + defOrg + "', '" + zone + "', " + x_coord + ", " +
                                    y_coord + ", " + LCA + ")";
                                IDataReader reader = command.ExecuteReader();
                            }
                        }
                        catch
                        {
                            this.sendMessage(bot, "Error during tower history archiving!");
                        }
                    }

                    //The %s organization %s attacked the %s %s at %s. The attackers won!!
                    Regex Victory = new Regex(@"The\s(.*)\sorganization\s(.*)\sattacked\sthe\s([a-zA-Z]+)\s(.*)\sat\stheir\sbase\sin\s(.*)\.\sThe\sattackers\swon!!");
                    if (Victory.IsMatch(e.Message))
                    {
                        Match m = Victory.Match(e.Message);
                        string atkrSide = m.Groups[1].Value;
                        string atkrOrg = m.Groups[2].Value;
                        string defSide = m.Groups[3].Value;
                        string defOrg = m.Groups[4].Value;
                        string zone = m.Groups[5].Value;
                        try
                        {
                            using (IDbCommand command = this._database.Connection.CreateCommand())
                            {
                                command.CommandText = "INSERT INTO [towerhistory] (victory, time, atkrSide, atkrOrg, defSide, defOrg, zone) VALUES (1, " +
                                    TimeStamp.Now + ", '" + atkrSide + "', '" + atkrOrg + "', '" + defSide + "', '" + defOrg + "', '" + zone + "')";
                                IDataReader reader = command.ExecuteReader();
                            }
                        }
                        catch
                        {
                            this.sendMessage(bot, "Error during tower history archiving!");
                        }
                    }
                }
            }
        }

        public void OnBattleCommand(BotShell bot, CommandArgs e)
        {
            try
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    RichTextWindow window = new RichTextWindow(bot);
                    window.AppendTitle("Tower Battle History");
                    window.AppendLineBreak();
                    command.CommandText = "SELECT [time], [atkrSide] , [atkrOrg], [atkrName], [defSide], [defOrg], [zone], [xCoord], [yCoord], [LCA] FROM [towerhistory] WHERE victory = 0 ORDER BY [time] DESC LIMIT 10";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        window.AppendHeader(Format.DateTime(reader.GetInt64(0), FormatStyle.Compact) + " GMT");
                        if (!reader.IsDBNull(2))
                        {
                            window.AppendHighlight("Attacker: ");
                            window.AppendColorString(this.sideColorWindow(reader.GetString(3), reader.GetString(1)), reader.GetString(3));
                            window.AppendNormal(" - (");
                            window.AppendColorString(this.sideColorWindow(reader.GetString(2), reader.GetString(1)), reader.GetString(2));
                            window.AppendNormal(")");
                            window.AppendLineBreak();
                        }
                        else
                        {
                            WhoisResult whois = XML.GetWhois(reader.GetString(3), bot.Dimension);
                            window.AppendHighlight("Attacker: ");
                            window.AppendColorString(this.sideColorWindow(reader.GetString(3),whois.Stats.Faction), reader.GetString(3));
                            window.AppendNormal(" - (Unguilded)");
                            window.AppendLineBreak();
                        }
                        window.AppendHighlight("Defender: ");
                        window.AppendColorString(this.sideColorWindow(reader.GetString(5), reader.GetString(4)), reader.GetString(5));
                        window.AppendLineBreak();
                        window.AppendHighlight("Location: ");
                        window.AppendNormal(reader.GetString(6) + " (x");
                        window.AppendColorString("FF0000", reader.GetInt64(9).ToString());
                        window.AppendNormal(" " + reader.GetInt64(7) + "x" + reader.GetInt64(8) + ")");
                        window.AppendLineBreak(2);
                    }
                    bot.SendReply(e, " Tower Battle Results »» ", window);
                    return;
                }
            }
            catch
            {
                bot.SendReply(e, "Error retrieving battle history!");
            }
        }

        public void OnVictoryCommand(BotShell bot, CommandArgs e)
        {
            try
            {
                using (IDbCommand command = this._database.Connection.CreateCommand())
                {
                    RichTextWindow window = new RichTextWindow(bot);
                    window.AppendTitle("Tower Victory History");
                    window.AppendLineBreak();
                    command.CommandText = "SELECT [time], [atkrSide], [atkrOrg], [defSide], [defOrg], [zone] FROM [towerhistory] WHERE victory = 1 ORDER BY [time] DESC LIMIT 10";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        window.AppendHeader(Format.DateTime(reader.GetInt64(0), FormatStyle.Compact) + " GMT");
                        if (!reader.IsDBNull(1))
                        {
                            window.AppendHighlight("Attacker: ");
                            window.AppendColorString(this.sideColorWindow(reader.GetString(2), reader.GetString(1)), reader.GetString(2));
                            window.AppendLineBreak();
                            window.AppendHighlight("Defender: ");
                            window.AppendColorString(this.sideColorWindow(reader.GetString(4), reader.GetString(3)), reader.GetString(4));
                            window.AppendLineBreak();
                            window.AppendHighlight("Location: ");
                            window.AppendNormal(reader.GetString(5));
                            window.AppendLineBreak(2);
                        }
                        else
                        {
                            window.AppendColorString(this.sideColor(reader.GetString(4), reader.GetString(3)), reader.GetString(4));
                            window.AppendNormal(" lost their base in " + reader.GetString(5) + ".");
                            window.AppendLineBreak(2);
                        }
                    }
                    bot.SendReply(e, " Tower Victory Results »» ", window);
                    return;
                }
            }
            catch
            {
                bot.SendReply(e, "Error retrieving battle victory history!");
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "battle":
                    return "Displays recent tower battle messages from [All Towers].\n" +
                        "Usage: /tell " + bot.Character + " battle";
                case "victory":
                    return "Displays recent victory messages from [Tower Battle Outcome].\n" +
                        "Usage: /tell " + bot.Character + " victory";
            }
            return null;
        }

        #region Other Functions
        private void sendMessage(BotShell bot, string message)
        {
            if (this._sendpg) { bot.SendPrivateChannelMessage(bot.ColorHighlight + message); }
            if (this._sendgc) { bot.SendOrganizationMessage(bot.ColorHighlight + message); }
        }

        private string sideColor(string orgName, string orgSide)
        {
            orgSide = orgSide.ToLower();
            switch (orgSide)
            {
                case "clan":
                    return HTML.CreateColorString(RichTextWindow.ColorOrange, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(orgName));
                case "neutral":
                    return HTML.CreateColorString("FFFFFF", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(orgName));
                case "omni":
                    return HTML.CreateColorString(RichTextWindow.ColorBlue, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(orgName));
                default:
                    return HTML.CreateColorString("FFFFFF", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(orgName));
            }
        }

        private string sideColorWindow(string orgName, string orgSide)
        {
            orgSide = orgSide.ToLower();
            switch (orgSide)
            {
                case "clan":
                    return RichTextWindow.ColorOrange;
                case "neutral":
                    return "FFFFFF";
                case "omni":
                    return RichTextWindow.ColorBlue;
                default:
                    return "FFFFFF";
            }
        }

        public VhTowers_LCAs GetLCAs() { return (VhTowers_LCAs)this.ParseXml(typeof(VhTowers_LCAs), "lca.xml"); }

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

        private int grabLCA(string zone, int x, int y)
        {
            int limit = 290;
            foreach (VhTowers_LCAs_Entry Entry in this.GetLCAs().Entries)
            {
                if (Entry.Zone == zone)
                {
                    if (Entry.X >= (x - limit) && Entry.X <= (x + limit))
                    {
                        if (Entry.Y >= (y - limit) && Entry.Y <= (y + limit))
                        {
                            return Entry.Number;
                        }
                    }
                }
            }
            return 0;
        }
        #endregion
    }
}
