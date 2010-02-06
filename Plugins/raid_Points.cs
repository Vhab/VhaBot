using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidPoints: PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private BotShell _bot;

        public RaidPoints()
        {
            this.Name = "Raid :: Points";
            this.InternalName = "RaidPoints";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Manages adding and removing points";
            this.Commands = new Command[] {
                new Command("points", true, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
                new Command("points top20", true, UserLevel.Disabled, UserLevel.Member, UserLevel.Disabled),
                new Command("points add", true, UserLevel.Leader),
                new Command("points remove", true, UserLevel.Leader),
                new Command("points reward", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "points":
                    this.OnPointsCommand(bot, e);
                    break;
                case "points add":
                    this.OnPointsAddCommand(bot, e);
                    break;
                case "points remove":
                    this.OnPointsRemoveCommand(bot, e);
                    break;
                case "points reward":
                    this.OnPointsRewardCommand(bot, e);
                    break;
                case "points top20":
                    this.OnPointsTopCommand(bot, e);
                    break;
            }
        }

        private void OnPointsCommand(BotShell bot, CommandArgs e)
        {
            string raider;
            bool other = false;
            if (e.Args.Length > 0 && (bot.Users.GetUser(e.Sender) > bot.Commands.GetRight(e.Command, e.Type) || bot.Users.GetUser(e.Sender) == UserLevel.SuperAdmin))
                other = true;
            if (other)
                raider = bot.Users.GetMain(e.Args[0]);
            else
                raider = bot.Users.GetMain(e.Sender);

            double points = this._core.GetPoints(raider);
            if (points < this._core.MinimumPoints)
            {
                if (other)
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " doesn't have any points");
                else
                    bot.SendReply(e, "You don't have any points");
                return;
            }
            if (other)
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, raider) + " has " + HTML.CreateColorString(bot.ColorHeaderHex, points.ToString()) + " points");
            else
                if (e.Type == CommandType.Tell)
                    bot.SendPrivateMessage(e.Sender, bot.ColorHighlight + "You have " + HTML.CreateColorString(bot.ColorHeaderHex, points.ToString()) + " points", AoLib.Net.PacketQueue.Priority.Low, true);
                else
                    bot.SendReply(e, "You have " + HTML.CreateColorString(bot.ColorHeaderHex, points.ToString()) + " points");
        }

        private void OnPointsAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: points add [username] [amount]");
                return;
            }
            string raider = Format.UppercaseFirst(bot.Users.GetMain(e.Args[0]));
            double amount = 0;
            try { amount = Convert.ToDouble(e.Args[1].Replace(".", ",")); }
            catch { }
            if (amount < 0.1)
            {
                bot.SendReply(e, "You need to add at least 0,1 point");
                return;
            }
            double points = this._core.GetPoints(raider);
            if (points < this._core.MinimumPoints && bot.Users.GetUser(raider) < UserLevel.Member)
            {
                bot.SendReply(e, "Unable to add points to " + HTML.CreateColorString(bot.ColorHeaderHex, raider));
                return;
            }
            this._core.AddPoints(raider, amount);
            bot.SendReply(e, "You have added " + HTML.CreateColorString(bot.ColorHeaderHex, amount.ToString()) + " points. " + HTML.CreateColorString(bot.ColorHeaderHex, raider) + " now has " + HTML.CreateColorString(bot.ColorHeaderHex, this._core.GetPoints(raider).ToString()) + " points");
            this._core.Log(raider, e.Sender, this.InternalName, "points", e.Sender + " has added " + amount + " points to " + raider + " (Total Points: " + this._core.GetPoints(raider) + ")");
        }

        private void OnPointsRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: points remove [username] [amount]");
                return;
            }
            string raider = Format.UppercaseFirst(bot.Users.GetMain(e.Args[0]));
            double amount = 0;
            try { amount = Convert.ToDouble(e.Args[1].Replace(".", ",")); }
            catch { }
            if (amount < 0.1)
            {
                bot.SendReply(e, "You need to remove at least 0,1 point");
                return;
            }
            double points = this._core.GetPoints(raider);
            if (points < this._core.MinimumPoints)
            {
                bot.SendReply(e, "Unable to remove points from " + HTML.CreateColorString(bot.ColorHeaderHex, raider));
                return;
            }
            this._core.RemovePoints(raider, amount);
            bot.SendReply(e, "You have removed " + HTML.CreateColorString(bot.ColorHeaderHex, amount.ToString()) + " points. " + HTML.CreateColorString(bot.ColorHeaderHex, raider) + " now has " + HTML.CreateColorString(bot.ColorHeaderHex, this._core.GetPoints(raider).ToString()) + " points");
            this._core.Log(raider, e.Sender, this.InternalName, "points", e.Sender + " has removed " + amount + " points from " + raider + " (Total Points: " + this._core.GetPoints(raider) + ")");
        }

        private void OnPointsRewardCommand(BotShell bot, CommandArgs e)
        {
            if (!this._core.Running)
            {
                bot.SendReply(e, "There is currently no raid active");
                return;
            }
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: points reward [amount]");
                return;
            }
            double amount = 0;
            try { amount = Convert.ToDouble(e.Args[0].Replace(".", ",")); }
            catch { }
            if (amount < 0.1)
            {
                bot.SendReply(e, "You need to reward at least 0,1 point");
                return;
            }
            if (e.Args.Length >= 2)
            {
                RaidCore.Raider[] raiders = this._core.GetRaiders();
                Dictionary<string, double> points = new Dictionary<string, double>();
                switch (e.Args[1])
                {
                    case "activity":
                        foreach (RaidCore.Raider raider in raiders)
                        {
                            if (raider.Activity < 1)
                                continue;
                            double reward = (float)raider.Activity / ((float)this._core.TimeRunning.TotalSeconds - (float)this._core.TimePaused.TotalSeconds) * amount;
                            if (reward > amount) reward = amount;
                            points.Add(raider.Character, Math.Round(reward, 1));
                        }
                        break;
                    case "active":
                        foreach (RaidCore.Raider raider in raiders)
                        {
                            if (raider.OnRaid)
                                points.Add(raider.Character, amount);
                        }
                        break;
                    case "all":
                        foreach (RaidCore.Raider raider in raiders)
                        {
                            points.Add(raider.Character, amount);
                        }
                        break;
                    default:
                        bot.SendReply(e, "Invalid reward mode specified");
                        return;
                }
                double total = 0;
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Rewarded Points");
                foreach (KeyValuePair<string, double> kvp in points)
                {
                    window.AppendHighlight(kvp.Value.ToString("###0.0") + " ");
                    window.AppendNormal(kvp.Key);
                    window.AppendLineBreak();
                    this._core.AddPoints(kvp.Key, kvp.Value);
                    total += kvp.Value;
                    this._core.Log(kvp.Key, e.Sender, this.InternalName, "points", e.Sender + " has rewarded " + kvp.Value + " points to " + kvp.Key + " (Total Points: " + this._core.GetPoints(kvp.Key) + ")");
                }
                switch (e.Args[1])
                {
                    case "activity":
                        bot.SendReply(e, "You have rewarded a total of " + HTML.CreateColorString(bot.ColorHeaderHex, total.ToString()) + " points to " + HTML.CreateColorString(bot.ColorHeaderHex, points.Count.ToString()) + " raiders based on their active time on the raid »» ", window);
                        bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has rewarded " + HTML.CreateColorString(bot.ColorHeaderHex, amount.ToString()) + " points to all raiders based on their active time on the raid");
                        this._core.Log(e.Sender, e.Sender, this.InternalName, "points", e.Sender + " has rewarded " + amount + " points to " + points.Count + " raiders based on their active time on the raid (Total Points: " + total + ")");
                        return;
                    case "active":
                        bot.SendReply(e, "You have rewarded a total of " + HTML.CreateColorString(bot.ColorHeaderHex, total.ToString()) + " points to " + HTML.CreateColorString(bot.ColorHeaderHex, points.Count.ToString()) + " active raiders on the current raid »» ", window);
                        bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has rewarded " + HTML.CreateColorString(bot.ColorHeaderHex, amount.ToString()) + " points to all active raiders on the current raid");
                        this._core.Log(e.Sender, e.Sender, this.InternalName, "points", e.Sender + " has rewarded " + amount + " points to " + points.Count + " raiders active on the current raid (Total Points: " + total + ")");
                        return;
                    case "all":
                        bot.SendReply(e, "You have rewarded a total of " + HTML.CreateColorString(bot.ColorHeaderHex, total.ToString()) + " points to " + HTML.CreateColorString(bot.ColorHeaderHex, points.Count.ToString()) + " raiders that participated in this raid »» ", window);
                        bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has rewarded " + HTML.CreateColorString(bot.ColorHeaderHex, amount.ToString()) + " points to all raiders that participated on this raid");
                        this._core.Log(e.Sender, e.Sender, this.InternalName, "points", e.Sender + " has rewarded " + amount + " points to " + points.Count + " raiders that have participated on this raid (Total Points: " + total + ")");
                        return;
                }
            }
            else
            {
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle("Reward Points");
                window.AppendNormal("You are about to reward ");
                window.AppendHighlight(amount.ToString());
                window.AppendNormal(" points.");
                window.AppendLineBreak();
                window.AppendNormal("Please select one of the following modes for rewarding points.");
                window.AppendLineBreak(2);

                window.AppendHighlight("Activity Based");
                window.AppendLineBreak();
                window.AppendNormal("This method will reward points to all raiders relative to the time they spent on the raid while it was unpaused.");
                window.AppendLineBreak();
                window.AppendNormal("For example: If someone joined the raid for 30 minutes on a 60 minutes raid and the amount of points to be rewarded was 6, that raider would receive 3 points. Even if he wasn't at the raid at the moment the points were rewarded.");
                window.AppendLineBreak();
                window.AppendNormal("[");
                window.AppendBotCommand("Reward Points", "points reward " + amount.ToString() + " activity");
                window.AppendNormal("]");
                window.AppendLineBreak(2);

                window.AppendHighlight("All Active Raiders");
                window.AppendLineBreak();
                window.AppendNormal("This method will reward points to all raiders that are currently active on this raid");
                window.AppendLineBreak();
                window.AppendNormal("For example: If someone joined the raid for 30 minutes on a 60 minutes raid and the amount of points to be rewarded was 6, that raider would receive 6 points if he was on the raid at the moment these points were rewarded.");
                window.AppendLineBreak();
                window.AppendNormal("[");
                window.AppendBotCommand("Reward Points", "points reward " + amount.ToString() + " active");
                window.AppendNormal("]");
                window.AppendLineBreak(2);

                window.AppendHighlight("All Raiders");
                window.AppendLineBreak();
                window.AppendNormal("This method will reward points to all raiders that attended this raid");
                window.AppendLineBreak();
                window.AppendNormal("For example: If someone joined the raid for 30 minutes on a 60 minutes raid and the amount of points to be rewarded was 6, that raider would receive 6 points. Even if he wasn't at the raid at the moment the points were rewarded.");
                window.AppendLineBreak();
                window.AppendNormal("[");
                window.AppendBotCommand("Reward Points", "points reward " + amount.ToString() + " all");
                window.AppendNormal("]");
                window.AppendLineBreak(2);

                bot.SendReply(e, "Reward Points »» ", window);
            }
        }

        private void OnPointsTopCommand(BotShell bot, CommandArgs e)
        {
            Dictionary<string, double> points = this._core.GetAllPoints();
            int i = 1;
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Top 20 Raid Points");
            foreach (KeyValuePair<string, double> kvp in points)
            {
                if (i > 20)
                    break;
                window.AppendHighlight(Math.Floor(kvp.Value) + " ");
                window.AppendNormal(Format.UppercaseFirst(kvp.Key));
                window.AppendLineBreak();
                i++;
            }
            bot.SendReply(e, "Top 20 Raid Points »» ", window);
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "points":
                    return "Allows you to check how many raid points you have. Raid points are used to bid on loot during aunctions. Using 'points' will show you how many points you have, while using 'points [username]' will show you how many points [username] currently has.\n" +
                        "Usage: /tell " + bot.Character + " points [[username]]";
                case "points add":
                    return "Allows raid leaders to manually add [amount] raid points to [username]'s account. The raid leader optionally can choose to tell the raiders the [reason] for this points addition.\n" +
                        "Usage: /tell " + bot.Character + " points add [username] [[reason]]";
                case "points remove":
                    return "Allows raid leaders to manually remove [amount] raid points to [username]'s account. The raid leader optionally can choose to tell the raiders the [reason] for this points removal.\nIf the players' amount of raidpoints drops down to or below zero, he/she can no longer bid on loot during aunctions, but he/she can still join raffles.\n" +
                        "Usage: /tell " + bot.Character + " points remove [username] [[reason]]";
                case "points top20":
                    return "Displays the top 20 raiders with the most points\n" +
                        "Usage: /tell " + bot.Character + " points top20";
                case "points reward":
                    return "Allows raid leaders to reward points to all raiders that currently are or have been on the current raid. There are various options available for rewarding points, these options will be displayed when the command is issued.\n" +
                        "Usage: /tell " + bot.Character + " points reward [points]";
            }
            return null;
        }
    }
}
