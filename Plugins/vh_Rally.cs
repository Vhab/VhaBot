using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class Rally : PluginBase
    {
        private string _location = string.Empty;
        private int _x = 0;
        private int _y = 0;
        private string _note = string.Empty;
        private bool _set = false;

        public Rally()
        {
            this.Name = "Rally Manager";
            this.InternalName = "vhRally";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("rally", true, UserLevel.Member),
                new Command("rally set", true, UserLevel.Leader),
                new Command("rally clear", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot) { }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "rally":
                    if (!this._set)
                    {
                        bot.SendReply(e, "There's currently no rally set");
                        return;
                    }
                    bot.SendReply(e, "Rally »» " + HTML.CreateColorString(bot.ColorHeaderHex, this._location) + " [ " + HTML.CreateColorString(bot.ColorHeaderHex, this._x.ToString()) + " x " + HTML.CreateColorString(bot.ColorHeaderHex, this._y.ToString()) + " ] " + HTML.CreateColorString(bot.ColorHeaderHex, this._note));
                    break;
                case "rally set":
                    if (e.Args.Length < 3)
                    {
                        bot.SendReply(e, "Correct Usage: rally set [location] [x] [y] [note]");
                        return;
                    }
                    int x = 0;
                    try { x = Convert.ToInt32(e.Args[1]); }
                    catch
                    {
                        bot.SendReply(e, "Invalid X value");
                        return;
                    }
                    int y = 0;
                    try { y = Convert.ToInt32(e.Args[2]); }
                    catch
                    {
                        bot.SendReply(e, "Invalid Y value");
                        return;
                    }
                    this._location = e.Args[0];
                    this._x = x;
                    this._y = y;
                    this._note = string.Empty;
                    this._set = true;
                    if (e.Words.Length > 3)
                        this._note = e.Words[3];
                    bot.SendReply(e, "The rally has been set");
                    break;
                case "rally clear":
                    this._set = false;
                    this._note = string.Empty;
                    this._location = string.Empty;
                    this._x = 0;
                    this._y = 0;
                    bot.SendReply(e, "The rally has been cleared");
                    break;
            }
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "rally":
                    return "Displays the current rally. Please gather at this location if ordered.\n" +
                        "Usage: /tell " + bot.Character + " rally";
                case "rally set":
                    return "Allows you to set the current rally. After the rally has been set, it can be viewed using the 'rally' command.\n" +
                        "Usage: /tell " + bot.Character + " rally set [location] [x] [y] [note]\n" +
                        "Example: /tell " + bot.Character + " rally set EPF 1234 567 Gather at CT";
                case "rally clear":
                    return "Allows you to clear the current rally.\n" +
                        "Usage: /tell " + bot.Character + " rally clear";
            }
            return null;
        }
    }
}
