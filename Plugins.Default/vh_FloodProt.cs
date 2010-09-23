using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using AoLib.Utils;
/*
 * Todo:
 * 3. autoban
 * 4. stats
 */

namespace VhaBot.Plugins
{
    public class VhFloodProtection : PluginBase
    {
        Dictionary<uint, Flooders> Flooder = new Dictionary<uint, Flooders>();

        public VhFloodProtection()
        {
            this.Name = "Flood Protection";
            this.InternalName = "vhFloodProtection";
            this.Version = 100;
            this.Author = "Iriche";
            this.DefaultState = PluginState.Installed;
            this.Description = "Tired of people flooding the private group? Well kick them out, or warn them. This plugin handles those things automatically";
        }

        public override void OnInstall(BotShell bot)
        {

        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "flooddelay", "Flood delay in seconds", 5, 0, 1, 2, 3, 4, 5, 7, 10, 15, 20, 30, 45, 60);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "floodwarning", "How many lines per X seconds for warning", 5, 0, 1, 2, 3, 4, 5, 7, 10, 15, 20, 30, 45, 60);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "floodkick", "How many lines per X seconds for kick", 7, 0, 1, 2, 3, 4, 5, 7, 10, 15, 20, 30, 45, 60);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "repeatwarning", "How many of same line for warning", 3, 0, 2, 3, 4, 5, 7, 10, 15, 20, 30, 45, 60);
            bot.Configuration.Register(ConfigType.Integer, this.InternalName, "repeatkick", "How many of same lines for kick", 4, 0, 3, 4, 5, 7, 10, 15, 20, 30, 45, 60);

            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "limmune", "Leader Immune", true);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "aimmune", "Admin Immune", true);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "saimmune", "Super-Admin Immune", true);

            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "inccommands", "Include Commands", true);


            bot.Events.UserJoinChannelEvent += new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.PrivateChannelMessageEvent += new PrivateChannelMessageHandler(Events_PrivateChannelMessageEvent);
        }

    public void Events_UserJoinChannelEvent(BotShell bot, UserJoinChannelArgs e)
        {
            if (Flooder.ContainsKey(e.SenderID))
                Flooder.Remove(e.SenderID);
        }

        public override void OnUnload(BotShell bot)
        {
            Flooder.Clear();
            bot.Events.UserJoinChannelEvent -= new UserJoinChannelHandler(Events_UserJoinChannelEvent);
            bot.Events.PrivateChannelMessageEvent -= new PrivateChannelMessageHandler(Events_PrivateChannelMessageEvent);
        }

        private void Events_PrivateChannelMessageEvent(BotShell bot, PrivateChannelMessageArgs e)
        {
            if (e.Self == true)
                return;

            if (bot.Configuration.GetBoolean(this.InternalName, "inccommands", true) == false)
            {
                if (e.Command == true)
                    return;
            }
            if (bot.Configuration.GetBoolean(this.InternalName, "limmune", true) == true)
            {
                if (bot.Users.GetUser(e.Sender) == UserLevel.Leader)
                    return;
            }
            if (bot.Configuration.GetBoolean(this.InternalName, "aimmune", true) == true)
            {
                if (bot.Users.GetUser(e.Sender) == UserLevel.Admin)
                    return;
            }
            if (bot.Configuration.GetBoolean(this.InternalName, "saimmune", true) == true)
            {
                if (bot.Users.GetUser(e.Sender) == UserLevel.SuperAdmin)
                    return;
            }
            if (Flooder.ContainsKey(e.SenderID))
            {
                Flooder[e.SenderID].FloodCount += 1;
                Flooder[e.SenderID].LastSaid = TimeStamp.Now;

                // Handles the Pure FloodPart
                long TimeDiff = Flooder[e.SenderID].LastSaid - Flooder[e.SenderID].FirstSaid;
                if (TimeDiff <= bot.Configuration.GetInteger(this.InternalName, "flooddelay", 5) && bot.Configuration.GetInteger(this.InternalName, "flooddelay", 5) != 0)
                {
                    if (bot.Configuration.GetInteger(this.InternalName, "floodkick", 7) != 0 && Flooder[e.SenderID].Kick == false && Flooder[e.SenderID].FloodCount >= bot.Configuration.GetInteger(this.InternalName, "floodkick", 7))
                    {
                        Flooder[e.SenderID].Kick = true;
                        if (bot.PrivateChannel.IsOn(e.SenderID))
                        {
                            bot.PrivateChannel.Kick(e.Sender);
                            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has been removed from the private channel for flooding");
                            bot.SendPrivateMessage(e.Sender, bot.ColorHighlight + "You have been removed from the private channel for flooding!", false);
                        }
                    }
                    else if (bot.Configuration.GetInteger(this.InternalName, "floodwarning", 5) != 0 && Flooder[e.SenderID].FloodCount >= bot.Configuration.GetInteger(this.InternalName, "floodwarning", 5) && Flooder[e.SenderID].Warning == false && Flooder[e.SenderID].Kick == false)
                    {
                        Flooder[e.SenderID].Warning = true;
                        bot.SendPrivateMessage(e.Sender, bot.ColorHighlight + "Warning! You're close to being kicked for flooding", false);
                    }
                }
                else
                {
                    Flooder[e.SenderID].FloodCount = 0;
                    Flooder[e.SenderID].FirstSaid = TimeStamp.Now;
                    Flooder[e.SenderID].Warning = false;
                }
                // End of flood part

                // Handles the Repeation part
                if (e.Message.ToLower() == Flooder[e.SenderID].LastLine.ToLower())
                {
                    Flooder[e.SenderID].RepeatCount += 1;
                    if (bot.Configuration.GetInteger(this.InternalName, "repeatkick", 4) != 0 && Flooder[e.SenderID].RepeatCount >= bot.Configuration.GetInteger(this.InternalName, "repeatkick", 4) && Flooder[e.SenderID].Kick == false)
                    {
                        Flooder[e.SenderID].Kick = true;
                        if (bot.PrivateChannel.IsOn(e.SenderID))
                        {
                            bot.PrivateChannel.Kick(e.Sender);
                            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has been removed from the private channel for repeating");
                            bot.SendPrivateMessage(e.Sender, bot.ColorHighlight + "You have been removed from the private channel for repeating!", false);
                        }
                    }
                    else if (bot.Configuration.GetInteger(this.InternalName, "repeatwarning", 3) != 0 && Flooder[e.SenderID].RepeatCount >= bot.Configuration.GetInteger(this.InternalName, "repeatwarning", 3) && Flooder[e.SenderID].Warning == false && Flooder[e.SenderID].Kick == false)
                    {
                        Flooder[e.SenderID].Warning = true;
                        bot.SendPrivateMessage(e.Sender, bot.ColorHighlight + "Warning! You're close to being kicked for repeating", false);
                    }
                }
                else
                {
                    Flooder[e.SenderID].RepeatCount = 1;
                }
                Flooder[e.SenderID].LastLine = e.Message;
                // End of repeat part
            }
            else
                Flooder.Add(e.SenderID, new Flooders(e.Sender, TimeStamp.Now, TimeStamp.Now, 1, false, false, e.Message, 1));
        }

        public class Flooders
        {
            public string Name, LastLine;
            public long FirstSaid, LastSaid;
            public int FloodCount, RepeatCount;
            public bool Warning, Kick;

            public Flooders(string Name, long FirstSaid, long LastSaid, int FloodCount, bool Warning, bool Kick, string LastLine, int RepeatCount)
            {
                this.Name = Name;
                this.FirstSaid = FirstSaid;
                this.LastSaid = LastSaid;
                this.LastLine = LastLine;
                this.RepeatCount = RepeatCount;
                this.FloodCount = FloodCount;
                this.Warning = Warning;
                this.Kick = Kick;
            }
        }
    }
}