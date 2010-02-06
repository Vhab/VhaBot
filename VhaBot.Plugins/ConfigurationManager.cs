using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{
    public class ConfigurationManager : PluginBase
    {
        private Dictionary<string, string> Colors;
        public ConfigurationManager()
        {
            this.Name = "Configuration Manager";
            this.InternalName = "VhConfigurationManager";
            this.Author = "Vhab";
            this.Description = "Provides a UI for viewing and changing various dynamic settings";
            this.DefaultState = PluginState.Core;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("configuration", true, UserLevel.SuperAdmin),
                new Command("configuration reset", true, UserLevel.SuperAdmin),
                new Command("configuration set", true, UserLevel.SuperAdmin),
                new Command("configuration color", false, UserLevel.SuperAdmin),
                new Command("config", "configuration")
            };
        }

        public override void OnLoad(BotShell bot)
        {
            /*this.Colors = new string[][] {
                new string[] {"FFFFFF","CCCCCC","999999","666666","333333","000000","FFCC00","FF9900","FF6600","FF3300","000000","000000","000000","000000","000000","000000"},
                new string[] {"99CC00","000000","000000","000000","000000","CC9900","FFCC33","FFCC66","FF9966","FF6633","CC3300","000000","000000","000000","000000","CC0033"},
                new string[] {"CCFF00","CCFF33","333300","666600","999900","CCCC00","FFFF00","CC9933","CC6633","330000","660000","990000","CC0000","FF0000","FF3366","FF0033"},
                new string[] {"99FF00","CCFF66","99CC33","666633","999933","CCCC33","FFFF33","996600","993300","663333","993333","CC3333","FF3333","CC3366","FF6699","FF0066"},
                new string[] {"66FF00","99FF66","66CC33","669900","999966","CCCC66","FFFF66","996633","663300","996666","CC6666","FF6666","990033","CC3399","FF66CC","FF0099"},
                new string[] {"33FF00","66FF33","339900","66CC00","99FF33","CCCC99","FFFF99","CC9966","CC6600","CC9999","FF9999","FF3399","CC0066","990066","FF33CC","FF00CC"},
                new string[] {"00CC00","33CC00","336600","669933","99CC66","CCFF99","FFFFCC","FFCC99","FF9933","FFCCCC","FF99CC","CC6699","993366","660033","CC0099","330033"},
                new string[] {"33CC33","66CC66","00FF00","33FF33","66FF66","99FF99","CCFFCC","000000","000000","000000","CC99CC","996699","993399","990099","663366","660066"},
                new string[] {"006600","336633","009900","339933","669966","99CC99","000000","000000","000000","FFCCFF","FF99FF","FF66FF","FF33FF","FF00FF","CC66CC","CC33CC"},
                new string[] {"003300","00CC33","006633","339966","66CC99","99FFCC","CCFFFF","3399FF","99CCFF","CCCCFF","CC99FF","9966CC","663399","330066","9900CC","CC00CC"},
                new string[] {"00FF33","33FF66","009933","00CC66","33FF99","99FFFF","99CCCC","0066CC","6699CC","9999FF","9999CC","9933FF","6600CC","660099","CC33FF","CC00FF"},
                new string[] {"00FF66","66FF99","33CC66","009966","66FFFF","66CCCC","669999","003366","336699","6666FF","6666CC","666699","330099","9933CC","CC66FF","9900FF"},
                new string[] {"00FF99","66FFCC","33CC99","33FFFF","33CCCC","339999","336666","006699","003399","3333FF","3333CC","333399","333366","6633CC","9966FF","6600FF"},
                new string[] {"00FFCC","33FFCC","00FFFF","00CCCC","009999","006666","003333","3399CC","3366CC","0000FF","0000CC","000099","000066","000033","6633FF","3300FF"},
                new string[] {"00CC99","000000","000000","000000","000000","0099CC","33CCFF","66CCFF","6699FF","3366FF","0033CC","000000","000000","000000","000000","3300CC"},
                new string[] {"000000","000000","000000","000000","000000","000000","00CCFF","0099FF","0066FF","0033FF","000000","000000","000000","000000","000000","000000"},
            };*/
            this.Colors = new Dictionary<string, string>();
            this.Colors.Add("Red Colors", "");
            this.Colors.Add("IndianRed", "CD5C5C");
            this.Colors.Add("LightCoral", "F08080");
            this.Colors.Add("Salmon", "FA8072");
            this.Colors.Add("DarkSalmon", "E9967A");
            this.Colors.Add("LightSalmon", "FFA07A");
            this.Colors.Add("Red", "FF0000");
            this.Colors.Add("Crimson", "DC143C");
            this.Colors.Add("FireBrick", "B22222");
            this.Colors.Add("DarkRed", "8B0000");

            this.Colors.Add("Pink Colors", "");
            this.Colors.Add("Pink", "FFC0CB");
            this.Colors.Add("LightPink", "FFB6C1");
            this.Colors.Add("PaleVioletRed", "DB7093");
            this.Colors.Add("HotPink", "FF69B4");
            this.Colors.Add("DeepPink", "FF1493");
            this.Colors.Add("MediumVioletRed", "C71585");

            this.Colors.Add("Orange Colors", "");
            this.Colors.Add("Orange", "FFA500");
            this.Colors.Add("DarkOrange", "FF8C00");
            this.Colors.Add("Coral", "FF7F50");
            this.Colors.Add("Tomato", "FF6347");
            this.Colors.Add("OrangeRed", "FF4500");

            this.Colors.Add("Yellow Colors", "");
            this.Colors.Add("LightYellow", "FFFFE0");
            this.Colors.Add("LemonChiffon", "FFFACD");
            this.Colors.Add("LightGoldenrodYellow", "FAFAD2");
            this.Colors.Add("PapayaWhip", "FFEFD5");
            this.Colors.Add("Moccasin", "FFE4B5");
            this.Colors.Add("PeachPuff", "FFDAB9");
            this.Colors.Add("PaleGoldenrod", "EEE8AA");
            this.Colors.Add("Khaki", "F0E68C");
            this.Colors.Add("Yellow", "FFFF00");
            this.Colors.Add("Gold", "FFD700");
            this.Colors.Add("DarkKhaki", "BDB76B");

            this.Colors.Add("Green Colors", "");
            this.Colors.Add("GreenYellow", "ADFF2F");
            this.Colors.Add("Chartreuse", "7FFF00");
            this.Colors.Add("LawnGreen", "7CFC00");
            this.Colors.Add("Lime", "00FF00");
            this.Colors.Add("PaleGreen", "98FB98");
            this.Colors.Add("LightGreen", "90EE90");
            this.Colors.Add("MediumSpringGreen", "00FA9A");
            this.Colors.Add("SpringGreen", "00FF7F");
            this.Colors.Add("YellowGreen", "9ACD32");
            this.Colors.Add("LimeGreen", "32CD32");
            this.Colors.Add("MediumSeaGreen", "3CB371");
            this.Colors.Add("SeaGreen", "2E8B57");
            this.Colors.Add("ForestGreen", "228B22");
            this.Colors.Add("Green", "008000");
            this.Colors.Add("OliveDrab", "6B8E23");
            this.Colors.Add("Olive", "808000");
            this.Colors.Add("DarkOliveGreen", "556B2F");
            this.Colors.Add("DarkGreen", "006400");
            this.Colors.Add("MediumAquamarine", "66CDAA");
            this.Colors.Add("DarkSeaGreen", "8FBC8F");
            this.Colors.Add("LightSeaGreen", "20B2AA");
            this.Colors.Add("DarkCyan", "008B8B");
            this.Colors.Add("Teal", "008080");

            this.Colors.Add("Blue Colors", "");
            this.Colors.Add("Cyan", "00FFFF");
            this.Colors.Add("LightCyan", "E0FFFF");
            this.Colors.Add("PaleTurquoise", "AFEEEE");
            this.Colors.Add("Aqua", "00FFFF");
            this.Colors.Add("Aquamarine", "7FFFD4");
            this.Colors.Add("Turquoise", "40E0D0");
            this.Colors.Add("MediumTurquoise", "48D1CC");
            this.Colors.Add("DarkTurquoise", "00CED1");
            this.Colors.Add("PowderBlue", "B0E0E6");
            this.Colors.Add("LightSteelBlue", "B0C4DE");
            this.Colors.Add("LightBlue", "ADD8E6");
            this.Colors.Add("SkyBlue", "87CEEB");
            this.Colors.Add("LightSkyBlue", "87CEFA");
            this.Colors.Add("DeepSkyBlue", "00BFFF");
            this.Colors.Add("CornflowerBlue", "6495ED");
            this.Colors.Add("SteelBlue", "4682B4");
            this.Colors.Add("CadetBlue", "5F9EA0");
            this.Colors.Add("MediumSlateBlue", "7B68EE");
            this.Colors.Add("DodgerBlue", "1E90FF");
            this.Colors.Add("RoyalBlue", "4169E1");
            this.Colors.Add("Blue", "0000FF");
            this.Colors.Add("MediumBlue", "0000CD");
            this.Colors.Add("DarkBlue", "00008B");
            this.Colors.Add("Navy", "000080");
            this.Colors.Add("MidnightBlue", "191970");

            this.Colors.Add("Purple Colors", "");
            this.Colors.Add("Lavender", "E6E6FA");
            this.Colors.Add("Thistle", "D8BFD8");
            this.Colors.Add("Plum", "DDA0DD");
            this.Colors.Add("Violet", "EE82EE");
            this.Colors.Add("Fuchsia", "FF00FF");
            this.Colors.Add("Magenta", "FF00FF");
            this.Colors.Add("Orchid", "DA70D6");
            this.Colors.Add("MediumOrchid", "BA55D3");
            this.Colors.Add("MediumPurple", "9370DB");
            this.Colors.Add("SlateBlue", "6A5ACD");
            this.Colors.Add("BlueViolet", "8A2BE2");
            this.Colors.Add("DarkViolet", "9400D3");
            this.Colors.Add("DarkOrchid", "9932CC");
            this.Colors.Add("DarkMagenta", "8B008B");
            this.Colors.Add("Purple", "800080");
            this.Colors.Add("DarkSlateBlue", "483D8B");
            this.Colors.Add("Indigo", "4B0082");

            this.Colors.Add("Brown Colors", "");
            this.Colors.Add("Cornsilk", "FFF8DC");
            this.Colors.Add("BlanchedAlmond", "FFEBCD");
            this.Colors.Add("Bisque", "FFE4C4");
            this.Colors.Add("NavajoWhite", "FFDEAD");
            this.Colors.Add("Wheat", "F5DEB3");
            this.Colors.Add("BurlyWood", "DEB887");
            this.Colors.Add("Tan", "D2B48C");
            this.Colors.Add("RosyBrown", "BC8F8F");
            this.Colors.Add("SandyBrown", "F4A460");
            this.Colors.Add("Goldenrod", "DAA520");
            this.Colors.Add("DarkGoldenrod", "B8860B");
            this.Colors.Add("Peru", "CD853F");
            this.Colors.Add("Chocolate", "D2691E");
            this.Colors.Add("SaddleBrown", "8B4513");
            this.Colors.Add("Sienna", "A0522D");
            this.Colors.Add("Brown", "A52A2A");
            this.Colors.Add("Maroon", "800000");

            this.Colors.Add("White Colors", "");
            this.Colors.Add("White", "FFFFFF");
            this.Colors.Add("Snow", "FFFAFA");
            this.Colors.Add("Honeydew", "F0FFF0");
            this.Colors.Add("MintCream", "F5FFFA");
            this.Colors.Add("Azure", "F0FFFF");
            this.Colors.Add("AliceBlue", "F0F8FF");
            this.Colors.Add("GhostWhite", "F8F8FF");
            this.Colors.Add("WhiteSmoke", "F5F5F5");
            this.Colors.Add("Seashell", "FFF5EE");
            this.Colors.Add("Beige", "F5F5DC");
            this.Colors.Add("OldLace", "FDF5E6");
            this.Colors.Add("FloralWhite", "FFFAF0");
            this.Colors.Add("Ivory", "FFFFF0");
            this.Colors.Add("AntiqueWhite", "FAEBD7");
            this.Colors.Add("Linen", "FAF0E6");
            this.Colors.Add("LavenderBlush", "FFF0F5");
            this.Colors.Add("MistyRose", "FFE4E1");

            this.Colors.Add("Grey Colors", "");
            this.Colors.Add("Gainsboro", "DCDCDC");
            this.Colors.Add("LightGrey", "D3D3D3");
            this.Colors.Add("Silver", "C0C0C0");
            this.Colors.Add("DarkGray", "A9A9A9");
            this.Colors.Add("Gray", "808080");
            this.Colors.Add("DimGray", "696969");
            this.Colors.Add("LightSlateGray", "778899");
            this.Colors.Add("SlateGray", "708090");
            this.Colors.Add("DarkSlateGray", "2F4F4F");
            this.Colors.Add("Black", "000000");
        }

        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "configuration":
                    if (e.Args.Length == 0)
                        this.OnConfigurationCommand(bot, e);
                    else
                        this.OnConfigurationDisplayCommand(bot, e);
                    break;
                case "configuration set":
                    this.OnConfigurationSetCommand(bot, e);
                    break;
                case "configuration color":
                    this.OnConfigurationColorCommand(bot, e);
                    break;
                case "configuration reset":
                    this.OnConfigurationResetCommand(bot, e);
                    break;
            }
        }

        private void OnConfigurationCommand(BotShell bot, CommandArgs e)
        {
            string[] plugins = bot.Configuration.ListRegisteredPlugins();
            if (plugins == null || plugins.Length == 0)
            {
                bot.SendReply(e, "No configuration entries registered");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Configuration");
            foreach (string plugin in plugins)
            {
                PluginLoader loader = bot.Plugins.GetLoader(plugin);
                window.AppendNormalStart();
                window.AppendString("[");
                window.AppendBotCommand("Configure", "configuration " + plugin.ToLower());
                window.AppendString("] ");
                window.AppendColorEnd();

                window.AppendHighlight(loader.Name);
                window.AppendNormal(" (" + bot.Configuration.List(plugin).Length + " settings)");
                window.AppendLineBreak();
            }
            bot.SendReply(e, "Configuration »» ", window);
        }

        private void OnConfigurationDisplayCommand(BotShell bot, CommandArgs e)
        {
            string internalName = e.Args[0].ToLower();
            if (!bot.Plugins.Exists(internalName))
            {
                bot.SendReply(e, "No such plugin!");
                return;
            }
            ConfigurationEntry[] entires = bot.Configuration.List(internalName);
            if (entires.Length < 1)
            {
                bot.SendReply(e, "This plugin has no settings to configure");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            PluginLoader loader = bot.Plugins.GetLoader(internalName);
            window.AppendTitle("Configuration");
            foreach (ConfigurationEntry entry in entires)
            {
                window.AppendHighlight(entry.Name + ": ");
                window.AppendNormalStart();
                string command = "configuration set " + internalName + " " + entry.Key.ToLower();
                switch (entry.Type)
                {
                    case ConfigType.String:
                        string value1 = bot.Configuration.GetString(entry.Section, entry.Key, (string)entry.DefaultValue);
                        if (entry.Values != null && entry.Values.Length > 0)
                            window.AppendMultiBox(command, value1, entry.StringValues);
                        else
                        {
                            window.AppendString(value1 + " [");
                            window.AppendCommand("Edit", "/text /tell " + bot.Character + " " + command + " [text]");
                            window.AppendString("]");
                        }
                        break;
                    case ConfigType.Integer:
                        string value2 = bot.Configuration.GetInteger(entry.Section, entry.Key, (int)entry.DefaultValue).ToString();
                        if (entry.Values != null && entry.Values.Length > 0)
                            window.AppendMultiBox(command, value2, entry.StringValues);
                        else
                        {
                            window.AppendString(value2 + " [");
                            window.AppendCommand("Edit", "/text /tell " + bot.Character + " " + command + " [number]");
                            window.AppendString("]");
                        }
                        break;
                    case ConfigType.Boolean:
                        string value3 = "Off";
                        if (bot.Configuration.GetBoolean(entry.Section, entry.Key, (bool)entry.DefaultValue))
                            value3 = "On";
                        window.AppendMultiBox(command, value3, "On", "Off");
                        break;
                    case ConfigType.Username:
                        string value4 = bot.Configuration.GetUsername(entry.Section, entry.Key, (string)entry.DefaultValue);
                        window.AppendString(value4 + " [");
                        window.AppendCommand("Edit", "/text /tell " + bot.Character + " " + command + " [username]");
                        window.AppendString("]");
                        break;
                    case ConfigType.Date:
                        DateTime value5 = bot.Configuration.GetDate(entry.Section, entry.Key, (DateTime)entry.DefaultValue);
                        window.AppendString(value5.ToString("dd/MM/yyyy") + " [");
                        window.AppendCommand("Edit", "/text /tell " + bot.Character + " " + command + " [dd]/[mm]/[yyyy]");
                        window.AppendString("]");
                        break;
                    case ConfigType.Time:
                        TimeSpan value6 = bot.Configuration.GetTime(entry.Section, entry.Key, (TimeSpan)entry.DefaultValue);
                        window.AppendString(string.Format("{0:00}:{1:00}:{2:00}", Math.Floor(value6.TotalHours), value6.Minutes, value6.Seconds) + " [");
                        window.AppendCommand("Edit", "/text /tell " + bot.Character + " " + command + " [hh]:[mm]:[ss]");
                        window.AppendString("]");
                        break;
                    case ConfigType.Dimension:
                        string value7 = bot.Configuration.GetDimension(entry.Section, entry.Key, (Server)entry.DefaultValue).ToString();
                        window.AppendMultiBox(command, value7, Server.Atlantean.ToString(), Server.Rimor.ToString(), Server.DieNeueWelt.ToString(), Server.Test.ToString());
                        break;
                    case ConfigType.Color:
                        string value8 = bot.Configuration.GetColor(entry.Section, entry.Key, (string)entry.DefaultValue);
                        window.AppendColorString(value8, value8);
                        window.AppendString(" [");
                        window.AppendBotCommand("Edit", "configuration color " + internalName + " " + entry.Key.ToLower());
                        window.AppendString("]");
                        break;
                    case ConfigType.Password:
                        string value9 = bot.Configuration.GetPassword(entry.Section, entry.Key, (string)entry.DefaultValue);
                        for (int i = 0; i < value9.Length; i++)
                            window.AppendString("*");
                        window.AppendString(" [");
                        window.AppendCommand("Edit", "/text /tell " + bot.Character + " " + command + " [password]");
                        window.AppendString("]");
                        break;
                    case ConfigType.Custom:
                        string value10 = bot.Configuration.GetCustom(entry.Section, entry.Key);
                        if (value10 != null)
                            window.AppendRawString(value10);
                        break;
                }
                window.AppendString(" [");
                window.AppendBotCommand("Default", "configuration reset " + internalName + " " + entry.Key.ToLower());
                window.AppendString("]");
                window.AppendColorEnd();
                window.AppendLineBreak();
            }
            bot.SendReply(e, "Configuration »» " + loader.Name + " »» ", window);
        }

        private void OnConfigurationSetCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 3)
            {
                bot.SendReply(e, "Usage: configuration set [plugin] [key] [value]");
                return;
            }
            if (!bot.Configuration.IsRegistered(e.Args[0], e.Args[1]))
            {
                bot.SendReply(e, "No such configuration entry");
                return;
            }
            string section = e.Args[0].ToLower();
            string key = e.Args[1].ToLower();
            string value = e.Words[2];
            ConfigType type = bot.Configuration.GetType(section, key);
            bot.Configuration.Exists(section, key);
            object objValue = null;
            bool error = false;
            switch (type)
            {
                case ConfigType.Boolean:
                    if (value.ToLower() == "on")
                    {
                        objValue = true;
                        break;
                    }
                    if (value.ToLower() == "off")
                    {
                        objValue = false;
                        break;
                    }
                    error = true;
                    break;
                case ConfigType.Color:
                    if (!Regex.Match(value, "^[#]?[0-9a-f]{6}$", RegexOptions.IgnoreCase).Success)
                        error = true;
                    objValue = value;
                    break;
                case ConfigType.Date:
                    Match dateMatch = Regex.Match(value, "^([0-2][0-9]|3[0-1])/([0]?[0-9]|1[0-2])/([0-9]{4})$", RegexOptions.IgnoreCase);
                    if (!dateMatch.Success)
                    {
                        error = true;
                        break;
                    }
                    try
                    {
                        int day = Convert.ToInt32(dateMatch.Groups[1].Value);
                        int month = Convert.ToInt32(dateMatch.Groups[2].Value);
                        int year = Convert.ToInt32(dateMatch.Groups[3].Value);
                        objValue = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                    }
                    catch { error = true; }
                    break;
                case ConfigType.Dimension:
                    try { objValue = (Server)Enum.Parse(typeof(Server), Format.UppercaseFirst(value)); }
                    catch { error = true; }
                    break;
                case ConfigType.Integer:
                    try { objValue = Convert.ToInt32(value); }
                    catch { error = true; }
                    break;
                case ConfigType.String:
                case ConfigType.Password:
                    objValue = value;
                    break;
                case ConfigType.Time:
                    Match timeMatch = Regex.Match(value, "^([0-9]+):([0-5][0-9]|60):([0-5][0-9]|60)$", RegexOptions.IgnoreCase);
                    if (!timeMatch.Success)
                    {
                        error = true;
                        break;
                    }
                    try
                    {
                        int hours = Convert.ToInt32(timeMatch.Groups[1].Value);
                        int minutes = Convert.ToInt32(timeMatch.Groups[2].Value);
                        int seconds = Convert.ToInt32(timeMatch.Groups[3].Value);
                        objValue = new TimeSpan(hours, minutes, seconds);
                    }
                    catch { error = true; }
                    break;
                case ConfigType.Username:
                    if (bot.GetUserID(value) < 10)
                        error = true;
                    else
                        objValue = Format.UppercaseFirst(value);
                    break;
            }
            if (error)
            {
                bot.SendReply(e, "Invalid Value: " + HTML.CreateColorString(bot.ColorHeaderHex, value));
                return;
            }
            if (bot.Configuration.Set(type, section, key, objValue))
                bot.SendReply(e, "Configuration entry " + HTML.CreateColorString(bot.ColorHeaderHex, section + "::" + key) + " has been set to: " + HTML.CreateColorString(bot.ColorHeaderHex, value));
            else
                bot.SendReply(e, "Unknown error while storing settings!");
        }

        private void OnConfigurationColorCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Usage: configuration color [plugin] [key]");
                return;
            }
            if (!bot.Configuration.IsRegistered(e.Args[0], e.Args[1]))
            {
                bot.SendReply(e, "No such configuration entry");
                return;
            }
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle();
            bool first = true;
            string section = e.Args[0].ToLower();
            string key = e.Args[1].ToLower();
            string command = "configuration set " + section + " " + key + " ";
            foreach (KeyValuePair<string, string> color in this.Colors)
            {
                if (color.Value != string.Empty)
                {
                    window.AppendString("  ");
                    window.AppendBotCommandStart(command + color.Value, true);
                    window.AppendColorString(color.Value, color.Key);
                    window.AppendCommandEnd();
                    window.AppendLineBreak();
                }
                else
                {
                    if (!first)
                        window.AppendLineBreak();
                    window.AppendHeader(color.Key);
                    first = false;
                }
            }
            window.AppendLineBreak();
            window.AppendHeader("Other");
            window.AppendHighlight("To select a different color not listed above use: ");
            window.AppendLineBreak();
            window.AppendNormal("/tell " + bot.Character + " " + command + "<color hex>");
            bot.SendReply(e, "Color Selection »» ", window);
        }

        private void OnConfigurationResetCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Usage: configuration reset [plugin] [key]");
                return;
            }
            if (!bot.Configuration.IsRegistered(e.Args[0], e.Args[1]))
            {
                bot.SendReply(e, "No such configuration entry");
                return;
            }
            string section = e.Args[0].ToLower();
            string key = e.Args[1].ToLower();
            ConfigurationEntry entry = bot.Configuration.GetRegistered(section, key);
            bot.Configuration.Set(entry.Type, section, key, entry.DefaultValue);
            bot.Configuration.Delete(section, key);
            bot.SendReply(e, "Configuration entry " + HTML.CreateColorString(bot.ColorHeaderHex, section + "::" + key) + " has been reset to it's default value");
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "configuration":
                    return "Displays the central configuration interface.\nThis interface displays all settings that have been registered by plugins.\n" +
                        "Usage: /tell " + bot.Character + " configuration [[plugin]]";
                case "configuration set":
                    return "Allows you to set a configuration entry.\nIt's recommended to approach configuration entries using the central interface by issueing the 'configuration' command.\n" +
                        "Usage: /tell " + bot.Character + " configuration set [plugin] [key] [value]";
                case "configuration reset":
                    return "Allows you to reset a configuration entry to it's default value.\n" +
                        "Usage: /tell " + bot.Character + " configuration reset [plugin] [key]";
            }
            return null;
        }
    }
}