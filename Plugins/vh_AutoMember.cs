using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot.Plugins
{
    public class VhAutoMember : PluginBase
    {
        private bool _enabled = false;
        private bool _guestlist = false;
        private List<string> _factions;
        private List<int> _levels;
        private List<int> _defenderRanks;
        private List<string> _professions;
        private List<string> _breeds;
        private List<string> _genders;

        public VhAutoMember()
        {
            this.Name = "Automated Membership";
            this.InternalName = "VhAutoMember";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Commands = new Command[] {
                new Command("automember", true, UserLevel.Admin)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "enabled", "Automatically add new members", this._enabled);
            bot.Configuration.Register(ConfigType.Boolean, this.InternalName, "guestlist", "Add new members to the guestlist", this._guestlist);
            bot.Configuration.Register(ConfigType.String, this.InternalName, "factions", "Factions (example: clan;neutral)", "");
            bot.Configuration.Register(ConfigType.String, this.InternalName, "levels", "Levels (example: 1-50;190-220)", "");
            bot.Configuration.Register(ConfigType.String, this.InternalName, "defenderranks", "Defender Ranks (example: 20-30)", "");
            bot.Configuration.Register(ConfigType.String, this.InternalName, "professions", "Professions (example: soldier;enforcer)", "");
            bot.Configuration.Register(ConfigType.String, this.InternalName, "breeds", "Breeds (example: atrox;solitus)", "");
            bot.Configuration.Register(ConfigType.String, this.InternalName, "genders", "Genders (example: male;neuter)", "");
            bot.Events.ConfigurationChangedEvent += new ConfigurationChangedHandler(OnConfigurationChangedEvent);
            bot.Events.PrivateMessageEvent += new PrivateMessageHandler(OnPrivateMessageEvent);
            this.LoadConfiguration(bot);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Events.ConfigurationChangedEvent -= new ConfigurationChangedHandler(OnConfigurationChangedEvent);
            bot.Events.PrivateMessageEvent -= new PrivateMessageHandler(OnPrivateMessageEvent);
        }

        public void OnConfigurationChangedEvent(BotShell bot, ConfigurationChangedArgs e)
        {
            if (e.Section != this.InternalName) return;
            this.LoadConfiguration(bot);
        }

        private void LoadConfiguration(BotShell bot)
        {
            this._enabled = bot.Configuration.GetBoolean(this.InternalName, "enabled", this._enabled);
            this._guestlist = bot.Configuration.GetBoolean(this.InternalName, "guestlist", this._guestlist);
            this._factions = this.SplitValues(bot.Configuration.GetString(this.InternalName, "factions", ""));
            this._professions = this.SplitValues(bot.Configuration.GetString(this.InternalName, "professions", ""));
            this._breeds = this.SplitValues(bot.Configuration.GetString(this.InternalName, "breeds", ""));
            this._genders = this.SplitValues(bot.Configuration.GetString(this.InternalName, "genders", ""));
            this._levels = this.SplitRanges(bot.Configuration.GetString(this.InternalName, "levels", ""));
            this._defenderRanks = this.SplitRanges(bot.Configuration.GetString(this.InternalName, "defenderranks", ""));
        }

        private List<string> SplitValues(string input)
        {
            List<string> values = new List<string>();
            string[] splitted = input.Split(';');
            foreach (string split in splitted) if (!string.IsNullOrEmpty(split)) values.Add(Format.UppercaseFirst(split));
            return values;
        }

        private List<int> SplitRanges(string input)
        {
            List<int> values = new List<int>();
            string[] splitted = input.Split(';');
            foreach (string split in splitted)
            {
                if (string.IsNullOrEmpty(split)) continue;
                string[] ranges = split.Split('-');
                if (ranges.Length > 1)
                {
                    if (ranges.Length != 2) continue;
                    int minLevel, maxLevel;
                    if (Int32.TryParse(ranges[0], out minLevel) && Int32.TryParse(ranges[1], out maxLevel))
                        for (int i = minLevel; i <= maxLevel; i++)
                            if (!values.Contains(i)) values.Add(i);
                }
                else
                {
                    int level;
                    if (Int32.TryParse(split, out level) && !values.Contains(level)) values.Add(level);
                }
            }
            return values;
        }

        private string[] ToStringArray(List<int> array) { return this.ToStringArray(array.ToArray()); }
        private string[] ToStringArray(int[] array)
        {
            List<string> values = new List<string>();
            foreach (int value in array) values.Add(value.ToString());
            return values.ToArray();
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "automember":
                    this.OnAutomemberCommand(bot, e);
                    break;
            }
        }

        public void OnAutomemberCommand(BotShell bot, CommandArgs e)
        {
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Automated Membership");

            window.AppendHighlight("Enabled: ");
            if (this._enabled)
                window.AppendColorString(RichTextWindow.ColorGreen, "Yes");
            else
                window.AppendColorString(RichTextWindow.ColorRed, "No");
            window.AppendLineBreak();

            window.AppendHighlight("Add to guestlist: ");
            if (this._guestlist)
                window.AppendColorString(RichTextWindow.ColorGreen, "Yes");
            else
                window.AppendColorString(RichTextWindow.ColorRed, "No");
            window.AppendLineBreak();

            if (this._factions.Count > 0)
            {
                window.AppendHighlight("Factions: ");
                window.AppendNormal(string.Join(", ", this._factions.ToArray()));
                window.AppendLineBreak();
            }

            if (this._levels.Count > 0)
            {
                window.AppendHighlight("Levels: ");
                window.AppendNormal(string.Join(", ", this.ToStringArray(this._levels)));
                window.AppendLineBreak();
            }

            if (this._defenderRanks.Count > 0)
            {
                window.AppendHighlight("Defender Ranks: ");
                window.AppendNormal(string.Join(", ", this.ToStringArray(this._defenderRanks)));
                window.AppendLineBreak();
            }

            if (this._professions.Count > 0)
            {
                window.AppendHighlight("Professions: ");
                window.AppendNormal(string.Join(", ", this._professions.ToArray()));
                window.AppendLineBreak();
            }

            if (this._breeds.Count > 0)
            {
                window.AppendHighlight("Breeds: ");
                window.AppendNormal(string.Join(", ", this._breeds.ToArray()));
                window.AppendLineBreak();
            }

            if (this._genders.Count > 0)
            {
                window.AppendHighlight("Genders: ");
                window.AppendNormal(string.Join(", ", this._genders.ToArray()));
                window.AppendLineBreak();
            }

            bot.SendReply(e, "Automated Membership »» ", window);
        }

        private void OnPrivateMessageEvent(BotShell bot, PrivateMessageArgs e)
        {
            if (!this._enabled) return;
            if (bot.Users.Authorized(e.Sender, UserLevel.Member)) return;
            WhoisResult whois = e.SenderWhois;
            if (whois == null || !whois.Success) return;

            if (this._factions.Count > 0 && !this._factions.Contains(whois.Stats.Faction)) return;
            if (this._levels.Count > 0 && !this._levels.Contains(whois.Stats.Level)) return;
            if (this._defenderRanks.Count > 0 && !this._defenderRanks.Contains(whois.Stats.DefenderLevel)) return;
            if (this._professions.Count > 0 && !this._professions.Contains(whois.Stats.Profession)) return;
            if (this._breeds.Count > 0 && !this._breeds.Contains(whois.Stats.Breed)) return;
            if (this._genders.Count > 0 && !this._genders.Contains(whois.Stats.Gender)) return;

            bot.Users.AddUser(e.Sender, UserLevel.Member);
            if (this._guestlist) bot.FriendList.Add("guestlist", e.Sender);
            bot.SendPrivateMessage(e.SenderID, bot.ColorHighlight + "You have been automatically added as member of this bot");
        }
    }
}
