using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class Tasks : PluginBase
    {
        private Config _database;
        public Tasks()
        {
            this.Name = "Tasks Manager";
            this.InternalName = "vhTasks";
            this.Author = "Vhab";
            this.DefaultState = PluginState.Installed;
            this.Version = 100;
            this.Description = "Provides a tasks management system";
            this.Commands = new Command[] {
                new Command("tasks", true, UserLevel.Member),
                new Command("tasks add", true, UserLevel.Leader),
                new Command("tasks remove", true, UserLevel.Leader),
                new Command("tasks clear", true, UserLevel.Leader)
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._database = new Config(bot.ID, this.InternalName);
            this._database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS tasks (username VARCHAR(14) UNIQUE, task VARCHAR(255))");
        }
        public override void OnUnload(BotShell bot) { }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "tasks":
                    this.OnTasksCommand(bot, e);
                    break;
                case "tasks add":
                    this.OnTasksAddCommand(bot, e);
                    break;
                case "tasks remove":
                    this.OnTasksRemoveCommand(bot, e);
                    break;
                case "tasks clear":
                    this.OnTasksClearCommand(bot, e);
                    break;
            }
        }

        private void OnTasksCommand(BotShell bot, CommandArgs e)
        {
            string message = "Tasks";
            RichTextWindow window = new RichTextWindow(bot);
            window.AppendTitle("Tasks");
            bool found = false;
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT username, task FROM tasks ORDER BY username";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    found = true;
                    window.AppendHighlight(reader.GetString(0) + ": ");
                    window.AppendNormal(reader.GetString(1).Trim()+" [");
                    window.AppendBotCommand("Remove", "tasks remove " + reader.GetString(0));
                    window.AppendNormal("]");
                    window.AppendLineBreak();
                    if (e.Sender == reader.GetString(0))
                        message = "You have been assigned the following task: " + HTML.CreateColorString(bot.ColorHeaderHex, reader.GetString(1));
                }
                reader.Close();
            }
            if (!found)
                bot.SendReply(e, "No tasks assigned");
            else
                bot.SendReply(e, message + " »» ", window);
        }

        private void OnTasksAddCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: tasks add [username] [task]");
                return;
            }
            string username = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(username) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            if (bot.FriendList.IsOnline(username) != OnlineState.Online)
            {
                bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " isn't online right now");
                return;
            }
            string task = e.Words[1];
            this._database.ExecuteNonQuery("DELETE FROM tasks WHERE username = '" + username + "'");
            this._database.ExecuteNonQuery(string.Format("INSERT INTO tasks (username, task) VALUES ('{0}', '{1}')", username, Config.EscapeString(task)));
            bot.SendReply(e, "You have assigned a task to " + HTML.CreateColorString(bot.ColorHeaderHex, username));
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has assigned " + HTML.CreateColorString(bot.ColorHeaderHex, username) + " the following task: " + HTML.CreateColorString(bot.ColorHeaderHex, task));
            bot.SendPrivateMessage(username, bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has assigned you the following task: " + HTML.CreateColorString(bot.ColorHeaderHex, task));
        }

        private void OnTasksRemoveCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: tasks remove [username]");
                return;
            }
            string username = Format.UppercaseFirst(e.Args[0]);
            if (bot.GetUserID(username) < 100)
            {
                bot.SendReply(e, "No such user: " + HTML.CreateColorString(bot.ColorHeaderHex, username));
                return;
            }
            using (IDbCommand command = this._database.Connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM tasks WHERE username = '" + username + "'";
                IDataReader reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    reader.Close();
                    bot.SendReply(e, HTML.CreateColorString(bot.ColorHeaderHex, username) + " doesn't have any tasks assigned");
                    return;
                }
                reader.Close();
            }
            this._database.ExecuteNonQuery("DELETE FROM tasks WHERE username = '" + username + "'");
            bot.SendReply(e, "You removed " + HTML.CreateColorString(bot.ColorHeaderHex, username + "'s") + " assigned task");
            bot.SendPrivateChannelMessage(bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has removed " + HTML.CreateColorString(bot.ColorHeaderHex, username + "'s") + " assigned task");
        }

        private void OnTasksClearCommand(BotShell bot, CommandArgs e)
        {
            this._database.ExecuteNonQuery("DELETE FROM tasks");
            bot.SendReply(e, "All tasks have been cleared");
            bot.SendPrivateChannelMessage(bot.ColorHighlight + "All tasks have been cleared");
        }

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command)
            {
                case "tasks":
                    return "Displays the assigned tasks.\n" +
                        "Usage: /tell " + bot.Character + " tasks";
                case "tasks add":
                    return "Allows you to assign a task to [username].\n" +
                        "Usage: /tell " + bot.Character + " tasks add [username]";
                case "tasks remove":
                    return "Allows you to remove [username]'s tasks.\n" +
                        "Usage: /tell " + bot.Character + " tasks remove [username]";
                case "tasks clear":
                    return "Clears all assigned tasks.\n" +
                        "Usage: /tell " + bot.Character + " tasks clear";
            }
            return null;
        }
    }
}
