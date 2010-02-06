using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Permissions;
using System.Reflection;
using System.CodeDom.Compiler;
using AoLib.Utils;

namespace VhaBot.Plugins
{
    public class CommonTools : PluginBase
    {
        private Random _random;
        private SortedDictionary<int, Verify> _results;

        public CommonTools()
        {
            this.Name = "Common Tools";
            this.InternalName = "vhCommon";
            this.Author = "Vhab / Iriche";
            this.Version = 100;
            this.DefaultState = PluginState.Installed;

            this.Commands = new Command[] {
                new Command("time", true, UserLevel.Guest),
                new Command("date", true, UserLevel.Guest),
                new Command("oe", true, UserLevel.Guest),
                new Command("coords", true, UserLevel.Guest),
                new Command("calc", true, UserLevel.Guest),
                new Command("roll", true, UserLevel.Guest),
                new Command("flip", true, UserLevel.Guest),
                new Command("verify", true, UserLevel.Guest),
                new Command("compass", true, UserLevel.Guest),
                new Command("random", true, UserLevel.Guest),
                new Command("lootorder", "random"),
                new Command("init", true, UserLevel.Guest),
                new Command("aggdef", "init")
            };
        }

        public override void OnLoad(BotShell bot)
        {          
            this._regex = new Regex(@"(([^0-9])(\.\d+))", RegexOptions.Compiled);

            this._random = new Random();
            this._results = new SortedDictionary<int, Verify>();
            bot.Timers.Minute += new EventHandler(Timers_Minute);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Timers.Minute -= new EventHandler(Timers_Minute);
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            switch (e.Command)
            {
                case "time":
                    this.OnTimeCommand(bot, e);
                    break;
                case "date":
                    DateTimeFormatInfo dtfi = new CultureInfo("en-US", false).DateTimeFormat;
                    bot.SendReply(e, "The Current Date is " + HTML.CreateColorString(bot.ColorHeaderHex, DateTime.Now.ToUniversalTime().ToString("dddd, MMMM d, ", dtfi) + (DateTime.Now.ToUniversalTime().Year + 27474)));
                    break;
                case "oe":
                    double val = 0;
                    if (!Double.TryParse(e.Args[0], out val))
                    {
                        bot.SendReply(e, "Correct Usage: oe [number]");
                        return;
                    }
                    double oe1 = Math.Ceiling(val * 1.25);
                    double oe2 = Math.Ceiling(val * 0.80);

                    string msg = "With a skill of {0}, you will be over equipped above {1} requirement. With a requirement of {0}, you can have {2} without being over equipped.";
                    msg = string.Format(msg, HTML.CreateColorString(bot.ColorHeaderHex, val.ToString()), HTML.CreateColorString(bot.ColorHeaderHex, oe1.ToString()), HTML.CreateColorString(bot.ColorHeaderHex, oe2.ToString()));
                    bot.SendReply(e, msg);
                    break;
                case "coords":
                    this.OnCoordsCommand(bot, e);
                    break;
                case "calc":
                    this.OnCalcCommand(bot, e);
                    break;
                case "roll":
                    this.OnRollCommand(bot, e);
                    break;
                case "flip":
                    this.OnFlipCommand(bot, e);
                    break;
                case "verify":
                    this.OnVerifyCommand(bot, e);
                    break;
                case "compass":
                    bot.SendReply(e, String.Format("{0}N{1} nne {2}NE{1} ene {0}E{1} ese {2}SE{1} sse {0}S{1} ssw {2}SW{1} wsw {0}W{1} wnw {2}NW{1} nnw {0}N{1}", HTML.CreateColorStart(bot.ColorHeaderHex), HTML.CreateColorEnd(), HTML.CreateColorStart(bot.ColorNormalHex)));
                    break;
                case "init":
                    this.OnInitCommand(bot, e);
                    break;
                case "random":
                    this.OnRandomCommand(bot, e);
                    break;
            }
        }

        private void OnRandomCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: random [name] [name] [[name]]...");
                return;
            }

            string[] looters = e.Args;
            string[] lootOrder = new string[e.Args.Length];
            string output = "";

            for (int iter1 = 0; iter1 < e.Args.Length; iter1++)
            {
                lootOrder[iter1] = this._random.Next(0, 10000000).ToString();
            }
            Array.Sort(lootOrder, looters);

            for (int iter2 = 0; iter2 < e.Args.Length; iter2++)
            {
                output = output + " " + (iter2 + 1).ToString() + ":" + looters[iter2];
            }
            lock (this._results)
            {
                int verify = this.GetNextVerifyNumber();
                string reply = "Random order: " + HTML.CreateColorString(bot.ColorHeaderHex, output);
                this._results.Add(verify, new Verify(e.Sender, reply));
                bot.SendReply(e, reply + ". To verify this use: " + HTML.CreateColorString(bot.ColorHeaderHex, "/tell " + bot.Character + " verify " + verify));
            }
        }

        private void OnTimeCommand(BotShell bot, CommandArgs e)
        {
            DateTime now = DateTime.Now.ToUniversalTime();
            RichTextWindow window = new RichTextWindow(bot);
            string spacer = HTML.CreateColorString("#000000", "i");
            window.AppendTitle("US Pacific");
            window.AppendHighlight("Winter Offset");
            window.AppendLineBreak();
            window.AppendNormalStart();
            window.AppendRawString("  " + Format.Time(now.AddHours(-3.5), FormatStyle.Compact) + " NST (GMT-3:30)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-4), FormatStyle.Compact) + " AST (GMT-4)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-5), FormatStyle.Compact) + " EST (GMT-5)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-6), FormatStyle.Compact) + " CST (GMT-6)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-7), FormatStyle.Compact) + " MST (GMT-7)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-8), FormatStyle.Compact) + " PST (GMT-8)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-9), FormatStyle.Compact) + " AKST (GMT-9)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-10), FormatStyle.Compact) + " HAST (GMT-10)");
            window.AppendLineBreak();

            window.AppendHighlight("Summer Offset");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-2.5), FormatStyle.Compact) + " NDT (GMT-2:30)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-3), FormatStyle.Compact) + " ADT (GMT-3)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-4), FormatStyle.Compact) + " EDT (GMT-4)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-5), FormatStyle.Compact) + " CDT (GMT-5)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-6), FormatStyle.Compact) + " MDT (GMT-6)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-7), FormatStyle.Compact) + " PDT (GMT-7)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-8), FormatStyle.Compact) + " AKDT (GMT-8)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(-9), FormatStyle.Compact) + " HADT (GMT-9)");
            window.AppendLineBreak(2);

            window.AppendHeader("Europe");
            window.AppendHighlight("Winter Offset");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(0), FormatStyle.Compact) + " WET (GMT+0)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(1), FormatStyle.Compact) + " CET (GMT+1)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(2), FormatStyle.Compact) + " EET (GMT+2)");
            window.AppendLineBreak();
            window.AppendHighlight("Summer Offset");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(1), FormatStyle.Compact) + " WEST (GMT+1)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(2), FormatStyle.Compact) + " CEST (GMT+2)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(3), FormatStyle.Compact) + " EEST (GMT+3)");
            window.AppendLineBreak(2);

            window.AppendHeader("Australia");
            window.AppendHighlight("Winter Offset");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(8), FormatStyle.Compact) + " AWST (GMT+8)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(9.5), FormatStyle.Compact) + " ACST (GMT+9:30)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(10), FormatStyle.Compact) + " AEST (GMT+10)");
            window.AppendLineBreak();
            window.AppendHighlight("Summer Offset");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(9), FormatStyle.Compact) + " AWDT (GMT+9)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(10.5), FormatStyle.Compact) + " ACDT (GMT+10:30)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(11), FormatStyle.Compact) + " AEDT (GMT+11)");
            window.AppendLineBreak(2);

            window.AppendHeader("Asian Pacific / Middle East");
            window.AppendHighlight("Default Offset");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(8), FormatStyle.Compact) + " HON (GMT+8)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(7), FormatStyle.Compact) + " BAN (GMT+7)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(7), FormatStyle.Compact) + " JAK (GMT+7)");
            window.AppendLineBreak();
            window.AppendRawString("  " + Format.Time(now.AddHours(3), FormatStyle.Compact) + " KUW (GMT+3)");
            window.AppendColorEnd();


            bot.SendReply(e, "The Current Time is " + HTML.CreateColorString(bot.ColorHeaderHex, Format.Time(DateTime.Now.ToUniversalTime(), FormatStyle.Large) + " GMT") + " »» ", window, "More Information");
        }

        private void OnCoordsCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 4)
            {
                bot.SendReply(e, "Correct Usage: coords [from x] [from y] [to x] [to y]");
                return;
            }

            string whereto = "";
            double bearing = 0.0;
            double distance = 0.0;
            double x1, x2, y1, y2, delta_x, delta_y, eta, degrees, radians;
            string replymessage = "";

            try
            {
                x1 = 0.000000001 + Convert.ToDouble(e.Args[0]);
                x2 = 0.000000001 + Convert.ToDouble(e.Args[2]);
                y1 = 0.000000001 + Convert.ToDouble(e.Args[1]);
                y2 = 0.000000001 + Convert.ToDouble(e.Args[3]);
                x1 = x1 - 0.000000001;
                x2 = x2 - 0.000000001;
                y1 = y1 - 0.000000001;
                y2 = y2 - 0.000000001;
                delta_y = Math.Abs(y2 - y1);
                delta_x = Math.Abs(x2 - x1);

                if (x1 < 0 || x2 < 0 || y1 < 0 || y2 < 0)
                {
                    bot.SendReply(e, "You cant use negative values! Correct Usage: coords [from x] [from y] [to x] [to y]");
                    return;
                }
                if (!((x2 - x1) == 0 && (y2 - y1) == 0))
                {
                    distance = Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
                    if ((x2 - x1) != 0)
                    {
                        radians = Math.Atan2(delta_y, delta_x);
                        degrees = radians / Math.PI * 180.0;
                        bearing = Math.Abs(degrees);
                    }
                    else
                    {
                        if ((y2 - y1) > 0) { bearing = 0.0; }
                        if ((y2 - y1) < 0) { bearing = 180.0; }
                    }
                    if (((x2 - x1) > 0.0) && ((y2 - y1) == 0.0)) { bearing = 90.0; }
                    if (((x2 - x1) < 0.0) && ((y2 - y1) == 0.0)) { bearing = 270.0; }
                    if (((x2 - x1) > 0.0) && ((y2 - y1) > 0.0)) { bearing = 90.0 - bearing; }
                    if (((x2 - x1) < 0.0) && ((y2 - y1) > 0.0)) { bearing = 270.0 + bearing; }
                    if (((x2 - x1) < 0.0) && ((y2 - y1) < 0.0)) { bearing = 270.0 - bearing; }
                    if (((x2 - x1) > 0.0) && ((y2 - y1) < 0.0)) { bearing = 90.0 + bearing; }
                }
                if (bearing >= 354.375 || bearing < 5.625) { whereto = "North"; }
                if (bearing >= 5.625 && bearing < 16.875) { whereto = "between North and NNE"; }
                if (bearing >= 16.875 && bearing < 28.125) { whereto = "NNE"; }
                if (bearing >= 28.125 && bearing < 39.375) { whereto = "between NNE and NE"; }
                if (bearing >= 39.375 && bearing < 50.625) { whereto = "NE"; }
                if (bearing >= 50.625 && bearing < 61.875) { whereto = "between NE and ENE"; }
                if (bearing >= 61.875 && bearing < 73.125) { whereto = "ENE"; }
                if (bearing >= 73.125 && bearing < 84.375) { whereto = "between ENE and East"; }
                if (bearing >= 84.375 && bearing < 95.625) { whereto = "East"; }
                if (bearing >= 95.625 && bearing < 106.875) { whereto = "between East and ESE"; }
                if (bearing >= 106.875 && bearing < 118.125) { whereto = "ESE"; }
                if (bearing >= 118.125 && bearing < 129.375) { whereto = "between ESE and SE"; }
                if (bearing >= 129.375 && bearing < 140.625) { whereto = "SE"; }
                if (bearing >= 140.625 && bearing < 151.875) { whereto = "between SE and SSE"; }
                if (bearing >= 151.875 && bearing < 163.125) { whereto = "SSE"; }
                if (bearing >= 163.125 && bearing < 174.375) { whereto = "between SSE and South"; }
                if (bearing >= 174.375 && bearing < 185.625) { whereto = "South"; }
                if (bearing >= 185.625 && bearing < 196.875) { whereto = "between South and SSW"; }
                if (bearing >= 196.875 && bearing < 208.125) { whereto = "SSW"; }
                if (bearing >= 208.125 && bearing < 219.375) { whereto = "between SSW and SW"; }
                if (bearing >= 219.375 && bearing < 230.625) { whereto = "SW"; }
                if (bearing >= 230.625 && bearing < 241.875) { whereto = "between SW and WSW"; }
                if (bearing >= 241.875 && bearing < 253.125) { whereto = "WSW"; }
                if (bearing >= 253.125 && bearing < 264.375) { whereto = "between WSW and West"; }
                if (bearing >= 264.375 && bearing < 275.625) { whereto = "West"; }
                if (bearing >= 275.625 && bearing < 286.875) { whereto = "between West and WNW"; }
                if (bearing >= 286.875 && bearing < 298.125) { whereto = "WNW"; }
                if (bearing >= 298.125 && bearing < 309.375) { whereto = "between WNW and NW"; }
                if (bearing >= 309.375 && bearing < 320.625) { whereto = "NW"; }
                if (bearing >= 320.625 && bearing < 331.875) { whereto = "between NW and NNW"; }
                if (bearing >= 331.875 && bearing < 343.125) { whereto = "NNW"; }
                if (bearing >= 343.125 && bearing < 354.375) { whereto = "between NNW and North"; }
                eta = distance / 14.0;
                replymessage = String.Format("{0} {1} to {2} {3} go {4}°, {5}, {6} meters, {7} seconds by Yalm", x1, y1, x2, y2, bearing, whereto, Math.Round(distance), Math.Round(eta));
            }
            catch
            {
                bot.SendReply(e, "Correct Usage: coords [from x] [from y] [to x] [to y]");
                return;
            }
            bot.SendReply(e, replymessage);
        }

        #region OnCalcCommand
        private CodeDomProvider _compiler;
        private CompilerParameters _parameters;
        private Regex _regex;
        private void OnCalcCommand(BotShell bot, CommandArgs e)
        {
            if (e.Words.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: calc [formula]");
                return;
            }

            // Compiler for the calc plugin
            _compiler = new Microsoft.CSharp.CSharpCodeProvider();
            _parameters = new CompilerParameters();
            _parameters.GenerateExecutable = false;
            _parameters.GenerateInMemory = true;
            _parameters.IncludeDebugInformation = false;
            _parameters.TempFiles.AddExtension(".dll", false);


            // Create formula
            string formula = e.Words[0].ToLower();
            formula = formula.Replace(" ", "");
            formula = formula.Replace(";", "");
            formula = formula.Replace("/", "/(float)");
            formula = formula.Replace("round(", "Math.Round(");
            formula = formula.Replace("floor(", "Math.Floor(");
            formula = formula.Replace("ceiling(", "Math.Ceiling(");
            formula = formula.Replace("pi", "Math.PI");
            formula = formula.Replace("sin(", "Math.Sin(");
            formula = formula.Replace("cos(", "Math.Cos(");
            formula = formula.Replace("tan(", "Math.Tan(");
            formula = formula.Replace("asin(", "Math.Asin(");
            formula = formula.Replace("acos(", "Math.Acos(");
            formula = formula.Replace("atan(", "Math.Atan(");
            formula = formula.Replace("exp(", "Math.Exp(");
            formula = formula.Replace("pow(", "Math.Pow(");
            formula = formula.Replace("abs(", "Math.Abs(");
            formula = formula.Replace("log(", "Math.Log(");
            formula = formula.Replace("log10(", "Math.Log10(");
            formula = formula.Replace("sqrt(", "Math.Sqrt(");
            formula = formula.Replace("x", "*");
            formula = formula.Replace("X", "*");
            formula = this._regex.Replace(formula, @"$2 0$3");

            // The code
            string open =
                "using System;" +
                "using System.Security.Permissions;" +
                "public class Solver { " +
                "[EnvironmentPermission(SecurityAction.Deny)]" +
                "[FileIOPermission(SecurityAction.Deny)]" +
                "[FileDialogPermission(SecurityAction.Deny)]" +
                "[IsolatedStorageFilePermission(SecurityAction.Deny)]" +
                "[ReflectionPermission(SecurityAction.Deny)]" +
                "[RegistryPermission(SecurityAction.Deny)]" +
                "[UIPermission(SecurityAction.Deny)]" +
                "public static string Solve() { return Convert.ToString((float)";
            string close = ");}}";
            string Source = open + formula + close;
            
            // Calculate
            CompilerResults results = _compiler.CompileAssemblyFromSource(_parameters, Source);
            if (results.Errors.Count == 0)
            {
                Assembly assembly = results.CompiledAssembly;
                try
                {
                    Type t = assembly.GetType("Solver", true, true);
                    MethodInfo mi = t.GetMethod("Solve");
                    string result = (string)mi.Invoke(null, null);
                    if (result != null && result != string.Empty)
                    {
                        bot.SendReply(e, e.Words[0].ToLower() + " = " + HTML.CreateColorString(bot.ColorHeaderHex, result));                      
                    }
                }
                catch { }
            }
            else
            {
                bot.SendReply(e, "Error in formula: " + HTML.CreateColorString(bot.ColorHeaderHex, e.Words[0].ToLower()));
            }
        }
        #endregion

        private void OnInitCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 2)
            {
                bot.SendReply(e, "Correct Usage: init [attack speed] [recharge speed] [[init]] [[target aggdef value]]");
                return;
            }

            // Parse required arguments
            double attackSpeed = 0;
            double rechargeSpeed = 0;
            if (Double.TryParse(e.Args[0].Replace('.', ','), out attackSpeed) == false || Double.TryParse(e.Args[1].Replace('.', ','), out rechargeSpeed) == false)
            {
                bot.SendReply(e, "Correct Usage: init [attack speed] [recharge speed] [[init]] [[target aggdef value]]");
                return;
            }
            if (attackSpeed <= 0)
            {
                bot.SendReply(e, "Invalid value given for attack speed");
                return;
            }
            if (rechargeSpeed <= 0)
            {
                bot.SendReply(e, "Invalid value given for recharge speed");
                return;
            }

            // Parse optional arguments
            double init = 0;
            if (e.Args.Length > 2) Double.TryParse(e.Args[2].Replace(",", ""), out init);
            double targetAggdef = 0;
            if (e.Args.Length > 3) Double.TryParse(e.Args[3].Replace(",", ""), out targetAggdef);
            if (init < 0) init = 0;
            if (targetAggdef < 0) targetAggdef = 0;

            // Max. Beneficial Init. Calculations
            double maxAttackInit, maxRechargeInit, maxNanoInit, maxWeaponInit;

            maxAttackInit = (double)((attackSpeed + (87.5 - targetAggdef) / 50 - 1) * 600);
            if (maxAttackInit > 1200)
                maxAttackInit = (double)((attackSpeed + (87.5 - targetAggdef) / 50 - 3) * 1200 + maxAttackInit);

            maxRechargeInit = (double)((rechargeSpeed + (87.5 - targetAggdef) / 50 - 1) * 300);
            if (maxRechargeInit > 1200)
                maxRechargeInit = (double)((rechargeSpeed + (87.5 - targetAggdef) / 50 - 5) * 600 + maxRechargeInit);

            if (maxAttackInit > maxRechargeInit)
                maxWeaponInit = maxAttackInit;
            else
                maxWeaponInit = maxRechargeInit;

            maxNanoInit = (double)((attackSpeed + (62.5 - targetAggdef) / 50) * 200);
            if (maxNanoInit > 1200)
                maxNanoInit = (double)((attackSpeed + (62.5 - targetAggdef) / 50 - 6) * 400 + maxNanoInit);

            // Agg-Def Calculations
            double bestAttackAggdef, bestRechargeAggdef, bestNanoAggdef, currentAttackRate, currentRechargeRate, currentNanoRate;

            if (init > 1200)
            {
                bestAttackAggdef = Math.Round((((attackSpeed + 0.75) - (2 + (init - 1200) / 1800)) / 2) * 100, 0);
                bestRechargeAggdef = Math.Round((((rechargeSpeed + 0.75) - (4 + (init - 1200) / 900)) / 2) * 100, 0);
                bestNanoAggdef = Math.Round((((attackSpeed + 1.25) - (6 + (init - 1200) / 600)) / 2) * 100, 0);
                currentAttackRate = Math.Round(attackSpeed + (87.5 - targetAggdef) / 50 - 2 - (init - 1200) / 1800, 2);
                currentRechargeRate = Math.Round(attackSpeed + (87.5 - targetAggdef) / 50 - 4 - (init - 1200) / 900, 2);
                currentNanoRate = Math.Round(attackSpeed + (62.5 - targetAggdef) / 50 - 6 - (init - 1200) / 600, 2);
            }
            else
            {
                bestAttackAggdef = Math.Round((((attackSpeed + 0.75) - init / 600) / 2) * 100, 0);
                bestRechargeAggdef = Math.Round((((rechargeSpeed + 0.75) - init / 300) / 2) * 100, 0);
                bestNanoAggdef = Math.Round((((attackSpeed + 1.25) - init / 200) / 2) * 100, 0);
                currentAttackRate = Math.Round(attackSpeed + (87.5 - targetAggdef) / 50 - (init) / 600, 2);
                currentRechargeRate = Math.Round(attackSpeed + (87.5 - targetAggdef) / 50 - (init) / 300, 2);
                currentNanoRate = Math.Round(attackSpeed + (62.5 - targetAggdef) / 50 - (init) / 200, 2);
            }

            // Some cleaning
            if (currentAttackRate < 1)
                currentAttackRate = 1;
            if (currentRechargeRate < 1)
                currentRechargeRate = 1;
            if (currentNanoRate < 0)
                currentNanoRate = 0;

            double bestAggDef;
            if (bestAttackAggdef > bestRechargeAggdef)
                bestAggDef = bestAttackAggdef;
            else
                bestAggDef = bestRechargeAggdef;

            if (bestAggDef < 0)
                bestAggDef = 0;
            else if (bestAggDef > 100)
                bestAggDef = 100;

            if (bestNanoAggdef < 0)
                bestNanoAggdef = 0;
            else if (bestNanoAggdef > 100)
                bestNanoAggdef = 100;

            // Format output
            string output = string.Format("Calculations for {0}{2}{1}/{0}{3}{1} with {0}{4}{1} inits at {0}{5}{1}% on the slider:\n" +
                "Weapon: {0}{6}{1}/{0}{7}{1}. {0}{8}{1} Init for 1/1 at {9}%. Best: {0}{10}%{1}\n" +
                "Nano: {0}{11}{1}/{0}{12}{1}. {0}{13}{1} Init for 0/{14} at {15}%. Best: {0}{16}%{1}",
                HTML.CreateColorStart(bot.ColorHeaderHex),
                HTML.CreateColorEnd(),
                attackSpeed, rechargeSpeed, init, targetAggdef,
                currentAttackRate, currentRechargeRate, maxWeaponInit, targetAggdef, bestAggDef,
                currentNanoRate, rechargeSpeed, maxNanoInit, rechargeSpeed, targetAggdef, bestNanoAggdef
                );
            bot.SendReply(e, output);
        }

        private void OnRollCommand(BotShell bot, CommandArgs e)
        {
            int low = 1;
            int high = 6;
            try
            {
                if (e.Args.Length == 1)
                {
                    high = Convert.ToInt32(e.Args[0]);
                }
                else if (e.Args.Length > 1)
                {
                    low = Convert.ToInt32(e.Args[0]);
                    high = Convert.ToInt32(e.Args[1]);
                }
            }
            catch { }
            if (high == int.MaxValue)
                high--;
            if (low > high)
            {
                int tmp = low;
                low = high;
                high = tmp;
            }
            int result = this._random.Next(low, high + 1);
            lock (this._results)
            {
                int verify = this.GetNextVerifyNumber();
                string reply = "From " + HTML.CreateColorString(bot.ColorHeaderHex, low.ToString()) + " to " + HTML.CreateColorString(bot.ColorHeaderHex, high.ToString()) +
                    ", I rolled " + HTML.CreateColorString(bot.ColorHeaderHex, result.ToString());
                this._results.Add(verify, new Verify(e.Sender, reply));
                bot.SendReply(e, reply + ". To verify this use: " + HTML.CreateColorString(bot.ColorHeaderHex, "/tell " + bot.Character + " verify " + verify));
            }
        }

        private void OnFlipCommand(BotShell bot, CommandArgs e)
        {
            int random = this._random.Next(0, 2);
            string result = "tails";
            if (random > 0)
            {
                result = "heads";
            }
            lock (this._results)
            {
                int verify = this.GetNextVerifyNumber();
                string reply = "I flipped a coin and it landed " + HTML.CreateColorString(bot.ColorHeaderHex, result);
                this._results.Add(verify, new Verify(e.Sender, reply));
                bot.SendReply(e, reply + ". To verify this use: " + HTML.CreateColorString(bot.ColorHeaderHex, "/tell " + bot.Character + " verify " + verify));
            }
        }

        private int GetNextVerifyNumber()
        {
            int verify = 0;
            lock (this._results)
            {
                if (this._results.Count > 0)
                {
                    verify = new List<int>(this._results.Keys)[this._results.Keys.Count - 1] + 1;
                }
            }
            return verify;
        }

        #region OnVerifyCommand
        public class Verify
        {
            public readonly DateTime Time;
            public readonly string User;
            public readonly string Result;

            public Verify(string user, string result)
            {
                this.Time = DateTime.Now;
                this.User = user;
                this.Result = result;
            }
        }

        private void Timers_Minute(object sender, EventArgs e)
        {
            // Clear old verify messages after 15 minutes
            List<int> remove = new List<int>();
            lock (this._results)
            {
                foreach (KeyValuePair<int, Verify> kvp in this._results)
                {
                    if (((TimeSpan)(DateTime.Now - kvp.Value.Time)).Minutes >= 15)
                    {
                        remove.Add(kvp.Key);
                    }
                }
                foreach (int id in remove)
                {
                    this._results.Remove(id);
                }
            }
        }

        private void OnVerifyCommand(BotShell bot, CommandArgs e)
        {
            if (e.Args.Length < 1)
            {
                bot.SendReply(e, "Correct Usage: verify [id]");
                return;
            }
            int id = 0;
            try
            {
                id = Convert.ToInt32(e.Args[0]);
            }
            catch
            {
                bot.SendReply(e, "Invalid ID");
                return;
            }
            lock (this._results)
            {
                // Get verification result
                if (!this._results.ContainsKey(id))
                {
                    bot.SendReply(e, "Unable to locate " + HTML.CreateColorString(bot.ColorHeaderHex, id.ToString()) + " in my records");
                    return;
                }
                Verify verify = this._results[id];
                TimeSpan ago = DateTime.Now - verify.Time;
                string agoString = ago.Seconds + " seconds";
                if (ago.Minutes > 0)
                    agoString = ago.Minutes + " minutes and " + agoString;
                string reply = HTML.CreateColorString(bot.ColorHeaderHex, agoString) + " ago, I told " + HTML.CreateColorString(bot.ColorHeaderHex, verify.User) + ": " + verify.Result;

                // Find more results
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle(verify.User + "'s History");
                int results = 0;
                foreach (Verify value in this._results.Values)
                {
                    if (value.User != verify.User) continue;
                    results++;
                    window.AppendHighlight(Format.Time(value.Time, FormatStyle.Medium) + ": ");
                    window.AppendNormalStart();
                    window.AppendRawString(value.Result);
                    window.AppendColorEnd();
                    window.AppendLineBreak();
                }

                // Send reply
                if (results > 1) bot.SendReply(e, reply + " »» ", window, "More Information");
                else bot.SendReply(e, reply);
            }
        }
        #endregion

        public override string OnHelp(BotShell bot, string command)
        {
            switch (command.ToLower())
            {
                case "time":
                    return "This command displays the current time in various timezones.\n" +
                        "Usage: /tell " + bot.Character + " time";
                case "date":
                    return "This command displays the current date in Anarchy Online.\n" +
                        "Usage: /tell " + bot.Character + " date";
                case "oe":
                    return "This will take a skill value you have and tell you how high of a requirement you can wear without being overquiped.\n" +
                        "Usage: /tell " + bot.Character + " oe [skill]";
                case "coords":
                    return "This will tell you which direction you need to go to reach a certain place.\n" +
                        "If you press F9 you will get a alot of data. The first two numbers are your current coordinations.\n" +
                        "The first number is how far east you are in your zone. The second number is how far North you are.\n" +
                        "If you run North and press F9 while walking, you will see the second number get higher.\n" +
                        "To use coords you will need to know where you are and where you are going.\n" +
                        "If you are at 100 200 and you want to go to 555 777 then you would type /tell " + bot.Character + " coords 100 200 555 777";
                case "calc":
                    return "This command will calculate numbers for you.\n" +
                        "Usage: /tell " + bot.Character + " calc [formula]";
                case "roll":
                    return "This will roll a random number in the range you specify.\n" +
                        "Usage: /tell " + bot.Character + " roll [minimum] [maximum]";
                case "flip":
                    return "This will flip a coin and tell you the random result.\n" +
                        "Usage: /tell " + bot.Character + " flip";
                case "verify":
                    return "This will tell you the result of a roll or flip and when it happened.\n" +
                        "Usage: /tell " + bot.Character + " verify [id]";
                case "compass":
                    return "This will give you a reference Compass in case you wanted to know if NWN was north of NW or west of it.\n" +
                        "Usage: /tell " + bot.Character + " compass";
                case "init":
                    return "This will calculate the optimal aggdef slider settings based on your input.\n" +
                        "Usage: /tell " + bot.Character + " init [attack speed] [recharge speed] [[init]] [[target aggdef value]]";
                case "random":
                    return "This will randomly sort a list of input. You must include at least 2 names for it to work.\n" +
                        "Usage: /tell " + bot.Character + " random [name] [name] [[name]] [[name]] ...";
                default:
                    return null;
            }
        }
    }
}