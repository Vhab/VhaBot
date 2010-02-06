using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Net;
using AoLib.Utils;

namespace VhaBot
{
    public class RichTextWindow : TextWindow
    {
        public string ColorDetail;
        public string ColorHeader;
        public string ColorHighlight;
        public string ColorNormal;

        public static readonly string ColorGreen = "00DD00";
        public static readonly string ColorRed = "DD0000";
        public static readonly string ColorBlue = "00BBFF";
        public static readonly string ColorOrange = "DD6600";

        private BotShell Bot;

        public RichTextWindow(BotShell bot)
        {
            this.ColorDetail = bot.ColorHeaderDetailHex;
            this.ColorHeader= bot.ColorHeaderHex;
            this.ColorHighlight = bot.ColorHighlightHex;
            this.ColorNormal = bot.ColorNormalHex;
            this.Bot = bot;
        }

        public void AppendTitle() { this.AppendTitle(null); }
        public void AppendTitle(string title)
        {
            this.AppendColorStart(this.ColorHeader);
            this.AppendColorString(this.ColorDetail, ":::::::::::");
            this.AppendString(" VhaBot Client Terminal ");
            this.AppendColorString(this.ColorDetail, ":::::::::::");
            this.AppendLineBreak();
            this.AppendColorString(this.ColorDetail, "« ");
            this.AppendCommand("About", "/tell " + this.Bot.Character + " version", true);
            this.AppendColorString(this.ColorDetail, " »     « ");
            this.AppendCommand("Help", "/tell " + this.Bot.Character + " help", true);
            this.AppendColorString(this.ColorDetail, " »     « ");
            this.AppendCommand("Close Terminal", "/close InfoView", true);
            this.AppendColorString(this.ColorDetail, " »");
            
            this.AppendLineBreak();
            this.AppendColorEnd();
            this.AppendColorString(this.ColorDetail, "¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯");
            this.AppendLineBreak();
            if (title != null)
                this.AppendHeader(title);
        }

        public void AppendHeader(string text)
        {
            this.AppendColorString(this.ColorHeader, text);
            this.AppendLineBreak();
        }

        public void AppendHighlightStart()
        {
            this.AppendColorStart(this.ColorHighlight);
        }

        public void AppendHighlight(string text)
        {
            this.AppendColorString(this.ColorHighlight, text);
        }

        public void AppendNormalStart()
        {
            this.AppendColorStart(this.ColorNormal);
        }

        public void AppendNormal(string text)
        {
            this.AppendColorString(this.ColorNormal, text);
        }

        public void AppendBotCommand(string name, string command) { this.AppendBotCommand(name, command, false); }
        public void AppendBotCommand(string name, string command, bool disableStyle)
        {
            this.AppendCommand(name, "/tell " + this.Bot.Character + " " + command, disableStyle);
        }

        public void AppendBotCommandStart(string command) { this.AppendBotCommandStart(command, false); }
        public void AppendBotCommandStart(string command, bool disableStyle)
        {
            this.AppendCommandStart("/tell " + this.Bot.Character + " " + command, disableStyle);
        }

        public void AppendProgressBar(double value, double valueMax, int size) { this.AppendProgressBar(value, valueMax, size, RichTextWindow.ColorGreen); }
        public void AppendProgressBar(double value, double valueMax, int size, string color)
        {
            int active = size;
            int inactive = 0;
            if (valueMax != 0 && value <= valueMax)
            {
                active = (int)((value / valueMax) * size);
                if (active < 0)
                    active = 0;
                inactive = size - active;
            }
            
            this.AppendColorStart(color);
            for (int i = 0; i < active; i++)
                this.AppendRawString("l");
            this.AppendColorEnd();

            this.AppendColorStart(this.ColorNormal);
            for (int i = 0; i < inactive; i++)
                this.AppendRawString("l");
            this.AppendColorEnd();
        }

        public void AppendMultiBox(string command, string selected, params string[] values)
        {
            if (values == null || values.Length == 0)
                return;

            this.AppendString("[");
            int i = 0;
            foreach (string value in values)
            {
                this.AppendBotCommand(value, command.Trim() + " " + value, (value == selected));
                i++;
                if (i < values.Length)
                    this.AppendString(" ");
            }
            this.AppendString("]");
        }

        public override string[] ToStrings() { return base.ToStrings(this.Bot.MaxWindowSizeOrganization); }
        public override string[] ToStrings(string title) { return base.ToStrings(title, this.Bot.MaxWindowSizeOrganization); }
        public override string[] ToStrings(string title, bool disableStyle) { return base.ToStrings(title, this.Bot.MaxWindowSizeOrganization, disableStyle); }
    }
}