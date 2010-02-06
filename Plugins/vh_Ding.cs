using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Data;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{
    public class VhDing : PluginBase
    {
        private BotShell _bot;

        public VhDing()
        {
            this.Name = "Org Congratulatory Function";
            this.InternalName = "VhDing";
            this.Author = "Llie";
            this.DefaultState = PluginState.Installed;
            this.Description = "Responds to ding messages";
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("ding", true, UserLevel.Guest)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "ding":
                    this.OnDingCommand(bot, e);
                    break;
            }
        }

        public void OnDingCommand(BotShell bot, CommandArgs e)
        {
            string[] bogusreplies = new string[] {
                "Umm... ya... Congratulations on whatever level you just dinged, (sender)" ,
                "Yay!, (sender) just dinged... something.",
                "dong!"
            };
            string[] replies = new string[] {
                "Congratulations on (level),(sender)",
                "Woot! (level)! You go, (sender)."
            };
            int[] titlebreaks= new int[] { 0, 5, 15, 50, 100, 150, 190, 205 };
            int newlevel = 0;
            int newtitle = 0;
            if (e.Words.Length > 0 )
            {
                if ( ! int.TryParse( e.Args[0], out newlevel ) )
                    newlevel = 0;
                newtitle = InIntList( titlebreaks, newlevel );
            }
            if ( (e.Words.Length < 1) || (newlevel < 1) || (newlevel > 220) )
                SendEverywhere( PickRandom( bogusreplies, e.Sender, Convert.ToString(newlevel) ) );
            else if ( newtitle > 0 )
                SendEverywhere("Woot! Congratulations on Title Level " + Convert.ToString(newtitle) + ", " + e.Sender );
            else
                SendEverywhere( PickRandom( replies, e.Sender, Convert.ToString(newlevel) ) );
        }

        public void SendEverywhere( string output )
        {
            _bot.SendPrivateChannelMessage( output );
            if ( _bot.InOrganization )
                _bot.SendOrganizationMessage( output );
            return;
        }

        public int InIntList(int[] list, int testnum )
        {
            int retval = -1;
            for (int iter = 0; iter < list.Length; iter++ )
                if ( list[iter] == testnum )
                    retval = iter;
            return retval;
        }

        public string PickRandom(string[] selection, string sender, string level)
        {
            Random random = new Random();
            string message = selection[random.Next(selection.Length)];
            message = message.Replace("(sender)", sender);
            message = message.Replace("(level)", level);
            return message;
        }
    }
}
