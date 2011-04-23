using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class UniqueMobs : PluginBase
    {
        private string Server = "unimob.dastof.com";
        private string UrlTemplate = "http://{0}/query.php?search={1}";
        private int NonPageSize = 3;

        public UniqueMobs()
        {
            this.Name = "Unique Mobs Database Lookup";
            this.InternalName = "VhUniqueMobs";
            this.Author = "Vhab, Llie";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            //this.Dependencies = new string[] { "vhItems" };
            this.Commands = new Command[] {
                new Command("umob", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "umob":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: umob [search string]");
                        return;
                    }
                    string search = string.Empty;
                    search = e.Words[0];
                    search = search.ToLower();

                    string url = string.Format(this.UrlTemplate, this.Server, HttpUtility.UrlEncode(search));
                    string xml = HTML.GetHtml(url, 20000);
                    if (xml == null || xml == string.Empty)
                    {
                        bot.SendReply(e, "Unable to query the unique mobs database");
                        return;
                    }
                    // if (xml.ToLower().StartsWith("<error>"))
                    // {
                    //     if (xml.Length > 13)
                    //     {
                    //         bot.SendReply(e, "Error: " + xml.Substring(7, xml.Length - 13));
                    //         return;
                    //     }
                    //     else
                    //     {
                    //         bot.SendReply(e, "An unknown error has occured!");
                    //         return;
                    //     }
                    // }

                    string result = string.Empty;
                    MemoryStream stream = null;
                    try
                    {
                        stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                        XmlSerializer serializer = new XmlSerializer(typeof(UniquesResults));
                        UniquesResults search_results = (UniquesResults)serializer.Deserialize(stream);
                        stream.Close();
                        if (search_results.Mobs == null || search_results.Mobs.Length == 0)
                            result = "No mobs were found";
                        else
                        {
                            RichTextWindow window = new RichTextWindow(bot);
                            if (search_results.Mobs.Length > this.NonPageSize)
                            {
                                window.AppendTitle("Unique Mobs Database");
                                window.AppendHighlight("Server: ");
                                window.AppendNormal(this.Server);
                                window.AppendLineBreak();
                                window.AppendHighlight("Version: ");
                                window.AppendNormal(search_results.Version);
                                window.AppendLineBreak();
                                window.AppendHighlight("Search String: ");
                                window.AppendNormal(search);
                                window.AppendLineBreak();
                                window.AppendHighlight("Results: ");
                                window.AppendNormal(search_results.Mobs.Length.ToString() + " / " + search_results.Max);
                                window.AppendLineBreak(2);
                                window.AppendHeader("Search Results");
                            }
                            else
                            {
                                window.AppendLineBreak();
                            }
                            foreach (UniqueMob mob in search_results.Mobs)
                            {
                                if (search_results.Mobs.Length <= this.NonPageSize)
                                    window.AppendString("    ");
                                window.AppendHighlight(mob.name + " ");
                                window.AppendNormalStart();
                                window.AppendLineBreak();
                                window.AppendString("    Level: " + mob.level);
                                window.AppendLineBreak();
                                window.AppendString("    Location: " + mob.location + " (" + mob.coords + ")");
                                window.AppendLineBreak();
                                window.AppendString("    Drops: ");
                                window.AppendLineBreak();
                                foreach (UItemsResults_Item item in mob.Items)
                                {
                                    window.AppendHighlight(item.Name + " ");
                                    window.AppendNormalStart();
                                    window.AppendString("[");
                                    window.AppendItem("QL " + item._ql, item.LowID, item.HighID, item.QL);
                                    window.AppendString("] (" + item._droprate + "%)");
                                    window.AppendColorEnd();
                                    window.AppendLineBreak();
                                }
                                window.AppendLineBreak(2);
                                //window.AppendLineBreak();
                            }
                            if (search_results.Mobs.Length > this.NonPageSize)
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, search_results.Mobs.Length.ToString()) + " Results »» ", window);
                            else
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, search_results.Mobs.Length.ToString()) + " Results »»" + window.Text.TrimEnd('\n'));
                            return;
                        }
                    }
                    catch
                    {
                        result = "Unable to query the unique mobs database";
                    }
                    finally
                    {
                        if (stream != null)
                            stream.Close();
                    }
                    bot.SendReply(e, result);
                    break;
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "umob":
                    return "Allows you to search the vhabot central items database.\n" +
                        "To search the items database use: /tell " + bot.Character + " items [partial item name]\n" +
                        "To search for a specific quality level use: /tell " + bot.Character + " items [quality level] [partial item name]\n" +
                        "You don't need to specify the full name of the item, you can use multiple words in order to find an item.\n" +
                        "However, the different words are required to be in the right order to find a certain item.\n" +
                        "For example: 'comb merc' will work but 'merc comb' won't if you want to find Combined Mercenary's armor";
            }
            return null;
        }
    }

    [XmlRoot("uniques")]
    public class UniquesResults
    {
        [XmlElement("version")]
        public string Version;
        [XmlElement("results")]
        public string Results;
        [XmlElement("max")]
        public string Max;
        [XmlElement("mob")]
        public UniqueMob[] Mobs;
        [XmlElement("credits")]
        public string Credits;
    }

    [XmlRoot("mob")]
    public class UniqueMob
    {
        [XmlElement("name")]
        public string name;
        [XmlElement("level")]
        public string level;
        [XmlElement("spawn_time")]
        public string spawn_time;
        [XmlElement("location")]
        public string location;
        [XmlElement("coords")]
        public string coords;
        [XmlElement("pf")]
        public string pf;
        [XmlElement("item")]
        public UItemsResults_Item[] Items;
        [XmlElement("info")]
        public string info;
    }

    public class UItemsResults_Item
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("lowid")]
        public string _lowid;
        [XmlAttribute("highid")]
        public string _highid;
        [XmlAttribute("ql")]
        public string _ql;
        [XmlAttribute("droprate")]
        public string _droprate;

        public int LowID { get { try { return Convert.ToInt32(this._lowid); } catch { return 0; } } }
        public int HighID { get { try { return Convert.ToInt32(this._highid); } catch { return 0; } } }
        public int QL { get { try { return Convert.ToInt32(this._ql); } catch { return 0; } } }

    }
}
