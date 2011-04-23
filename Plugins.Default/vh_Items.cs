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
    public class Items : PluginBase
    {
        //private string Server = "items.vhabot.net";
        private string Server = "cidb.xyphos.com";
        private string UrlTemplate = "http://{0}/?bot=VhaBot&search={1}&ql={2}&max={3}&output=xml";
        private int Max = 100;
        private int NonPageSize = 3;

        public Items()
        {
            this.Name = "Central Items Database";
            this.InternalName = "vhItems";
            this.Author = "Vhab / MJE";
            this.DefaultState = PluginState.Installed;
            this.Version = 105;
            this.Commands = new Command[] {
                new Command("items", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.String, this.InternalName, "server", "Items database server [cidb.xyphos.com]", this.Server);
            this.Server = bot.Configuration.GetString(this.InternalName, "server", this.Server);
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }
        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(Events_ConfigurationChangedEvent);
        }

        private void Events_ConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section == this.InternalName)
                this.Server = bot.Configuration.GetString(this.InternalName, "server", this.Server);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "items":
                    if (e.Args.Length < 1)
                    {
                        bot.SendReply(e, "Correct Usage: items [search string]");
                        return;
                    }
                    int ql = 0;
                    try
                    {
                        ql = Convert.ToInt32(e.Args[0]);
                        if (ql <= 0 || ql >= 1000)
                        {
                            bot.SendReply(e, "Quality level has to be between 0 and 999");
                            return;
                        }
                    }
                    catch { }
                    if (ql != 0 && e.Words.Length == 1)
                    {
                        bot.SendReply(e, "Correct Usage: items [quality level] [search string]");
                        return;
                    }
                    string search = string.Empty;
                    if (ql != 0)
                        search = e.Words[1];
                    else
                        search = e.Words[0];
                    search = search.ToLower();

                    string url = string.Format(this.UrlTemplate, this.Server, HttpUtility.UrlEncode(search), ql, Max);
                    string xml = HTML.GetHtml(url, 60000);
                    if (xml == null || xml == string.Empty)
                    {
                        bot.SendReply(e, "Unable to query the central items database");
                        return;
                    }
                    if (xml.ToLower().StartsWith("<error>"))
                    {
                        if (xml.Length > 13)
                        {
                            bot.SendReply(e, "Error: " + xml.Substring(7, xml.Length - 13));
                            return;
                        }
                        else
                        {
                            bot.SendReply(e, "An unknown error has occured!");
                            return;
                        }
                    }

                    string result = string.Empty;
                    MemoryStream stream = null;
                    try
                    {
                        stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                        XmlSerializer serializer = new XmlSerializer(typeof(ItemsResults));
                        ItemsResults items = (ItemsResults)serializer.Deserialize(stream);
                        stream.Close();
                        if (items.Items == null || items.Items.Length == 0)
                            result = "No items were found";
                        else
                        {
                            RichTextWindow window = new RichTextWindow(bot);
                            if (items.Items.Length > this.NonPageSize)
                            {
                                window.AppendTitle("Central Items Database");
                                window.AppendHighlight("Server: ");
                                window.AppendNormal(this.Server);
                                window.AppendLineBreak();
                                window.AppendHighlight("Version: ");
                                window.AppendNormal(items.Version);
                                window.AppendLineBreak();
                                window.AppendHighlight("Search String: ");
                                window.AppendNormal(search);
                                window.AppendLineBreak();
                                window.AppendHighlight("Results: ");
                                window.AppendNormal(items.Items.Length.ToString() + " / " + items.Max);
                                window.AppendLineBreak(2);
                                window.AppendHeader("Search Results");
                            }
                            else
                            {
                                window.AppendLineBreak();
                            }
                            foreach (ItemsResults_Item item in items.Items)
                            {
                                if (items.Items.Length <= this.NonPageSize)
                                    window.AppendString("    ");
                                window.AppendHighlight(item.Name + " ");
                                window.AppendNormalStart();
                                window.AppendString("[");
                                window.AppendItem("QL " + item.LowQL, item.LowID, item.HighID, item.LowQL);
                                window.AppendString("] ");
                                if (ql != 0 && ql > item.LowQL && ql < item.HighQL)
                                {
                                    window.AppendString("[");
                                    window.AppendItem("QL " + ql, item.LowID, item.HighID, ql);
                                    window.AppendString("] ");
                                }
                                if (item.HighQL != item.LowQL)
                                {
                                    window.AppendString("[");
                                    window.AppendItem("QL " + item.HighQL, item.LowID, item.HighID, item.HighQL);
                                    window.AppendString("] ");
                                }
                                window.AppendColorEnd();
                                window.AppendLineBreak();
                                if (item.IconID > 0 && items.Items.Length > this.NonPageSize)
                                {
                                    window.AppendIcon(item.IconID);
                                    window.AppendLineBreak();
                                }
                            }
                            if (items.Items.Length > this.NonPageSize)
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, items.Items.Length.ToString()) + " Results »» ", window);
                            else
                                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, items.Items.Length.ToString()) + " Results »»" + window.Text.TrimEnd('\n'));
                            return;
                        }
                    }
                    catch
                    {
                        result = "Unable to query the central items database";
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
                case "items":
                    return "Allows you to search the vhabot central items database.\n"+
                        "To search the items database use: /tell " + bot.Character + " items [partial item name]\n" +
                        "To search for a specific quality level use: /tell " + bot.Character + " items [quality level] [partial item name]\n" +
                        "You don't need to specify the full name of the item, you can use multiple words in order to find an item.\n" +
                        "However, the different words are required to be in the right order to find a certain item.\n" +
                        "For example: 'comb merc' will work but 'merc comb' won't if you want to find Combined Mercenary's armor";
            }
            return null;
        }
    }

    [XmlRoot("items")]
    public class ItemsResults
    {
        [XmlElement("version")]
        public string Version;
        [XmlElement("results")]
        public string Results;
        [XmlElement("max")]
        public string Max;
        [XmlElement("item")]
        public ItemsResults_Item[] Items;
        [XmlElement("credits")]
        public string Credits;
    }

    public class ItemsResults_Item
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("lowid")]
        public string _lowid;
        [XmlAttribute("highid")]
        public string _highid;
        [XmlAttribute("lowql")]
        public string _lowql;
        [XmlAttribute("highql")]
        public string _highql;
        [XmlAttribute("icon")]
        public string _iconid;

        public int LowID { get { try { return Convert.ToInt32(this._lowid); } catch { return 0; } } }
        public int HighID { get { try { return Convert.ToInt32(this._highid); } catch { return 0; } } }
        public int LowQL { get { try { return Convert.ToInt32(this._lowql); } catch { return 0; } } }
        public int HighQL { get { try { return Convert.ToInt32(this._highql); } catch { return 0; } } }
        public int IconID { get { try { return Convert.ToInt32(this._iconid); } catch { return 0; } } }
    }
}
