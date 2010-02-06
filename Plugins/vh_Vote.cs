using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Data;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class VhVote : PluginBase
    {
        private BotShell _bot;

        private bool _running = false;
        private string _admin = null;
        private string _description = null;
        private int _quorum = 0;
        private int _percentage = 51;
        private int _yes = 0;
        private int _no = 0;
        private int _abs = 0;
        private Dictionary<string, string> _joined = new Dictionary<string, string>();

        public VhVote()
        {
            this.Name = "Vote";
            this.InternalName = "VhVote";
            this.Version = 100;
            this.Author = "Llie - Modded by Naturalistic";
            this.DefaultState = PluginState.Installed;
            this.Description = "Manages Anonymous Voting / Elections";
            this.Commands = new Command[] {
                new Command("vote", true, UserLevel.Guest),
                new Command("vote start", true, UserLevel.Leader),
                new Command("vote abort", true, UserLevel.Leader),
                new Command("vote stop", true, UserLevel.Leader),
                new Command("vote yes", true, UserLevel.Guest),
                new Command("vote no", true, UserLevel.Guest),
                new Command("vote abstain", true, UserLevel.Guest )
            };
        }

        public override void OnLoad(BotShell bot)
        {
            this._bot = bot;
        }

        public override void OnCommand(BotShell bot, CommandArgs e)
        {
            lock (this)
            {
                switch (e.Command)
                {
                    case "vote":
                        this.OnVoteStatusCommand(bot, e);
                        break;
                    case "vote start":
                        this.OnVoteStartCommand(bot, e);
                        break;
                    case "vote stop":
                        this.OnVoteStopCommand(bot, e);
                        break;
                    case "vote abort":
                        this.OnVoteAbortCommand(bot, e);
                        break;
                    case "vote yes":
                        this.OnVoteCastCommand(bot, e);
                        break;
                    case "vote no":
                        this.OnVoteCastCommand(bot, e);
                        break;
                    case "vote abstain":
                        this.OnVoteCastCommand(bot, e);
                        break;
                }
            }
        }

        private void VoteUsage( CommandArgs e )
        {
            _bot.SendPrivateMessage(e.SenderID, "Correct Usage: vote start [quorum] [percentage] [description]");
            _bot.SendPrivateMessage(e.SenderID, "A quorum of zero (0) indicates no quorum is needed.");            
        }

        private void OnVoteStatusCommand(BotShell bot, CommandArgs e)
        {
            if ( !this._running )
                bot.SendPrivateMessage(e.SenderID, "There is nothing to vote on.");
            else
            {
                RichTextWindow window = new RichTextWindow(bot);
                window.AppendTitle( this._admin + " has initiated an election!");
                window.AppendLineBreak();
                window.AppendString("You are being asked to vote for or against the following topic:");
                window.AppendLineBreak();
                window.AppendLineBreak();
                window.AppendString(_description);
                window.AppendLineBreak();
                window.AppendNormalStart();
                window.AppendString("[");
                window.AppendBotCommand("Yes", "vote yes");
                window.AppendString("] [");
                window.AppendBotCommand("No", "vote no");
                window.AppendString("] [");
                window.AppendBotCommand("Abstain", "vote abstain");
                window.AppendString("]");
                window.AppendLineBreak();
                window.AppendLineBreak();
                window.AppendString("There are currently " + Convert.ToString ( _yes + _no ) + " votes.  " );
                if ( ( _quorum > 0 ) && ( _yes + _no + _abs < _quorum ) )
                    window.AppendString( "Quorum has not yet been met." );
                else
                    window.AppendString( "Quorum has been met." );
                if ( ( e.Sender == this._admin ) || ( bot.Users.GetUser(e.Sender) >= UserLevel.Leader ) )
                {
                    window.AppendLineBreak();
                    window.AppendLineBreak();
                    window.AppendString("Quorum is currently: " + Convert.ToString ( _quorum ) + " with a " + Convert.ToString ( _percentage ) + "% approval in order to win this election." );
                    window.AppendLineBreak();
                    window.AppendString("[");
                    window.AppendBotCommand("End Voting", "vote stop");
                    window.AppendString("] [");
                    window.AppendBotCommand("Abort Voting", "vote abort");
                    window.AppendString("]");
                }

                string output = string.Format("{1}{2}{0} has initiated an election »» ", bot.ColorHighlight, bot.ColorHeader, this._admin ) + window.ToString();
//                bot.SendPrivateChannelMessage(output);
                bot.SendPrivateMessage(e.SenderID, output );
            }
        }

        private void OnVoteStartCommand(BotShell bot, CommandArgs e)
        {
            if ( this._running && (e.Words.Length >= 3 ) )
            {
                bot.SendPrivateMessage(e.SenderID, "An election has already been started.");
            }
            if ( !this._running && (e.Words.Length >= 3) )
            {
                if ( ! int.TryParse( e.Args[0], out _quorum ) )
                    _quorum = 0;
                if ( ! int.TryParse( e.Args[1], out _percentage ) )
                {
                    switch ( e.Args[1] )
                    {
                    case "majority":
                        _percentage = 51;
                        break;
                    case "supermajority":
                        _percentage = 67;
                        break;
                    case "unanimous":
                        _percentage = 100;
                        break;
                    }
                }
                if ( _percentage == 0 )
                    _percentage = 51;
                _description = e.Args[2];
                for ( int iter=2; iter<e.Args.Length; iter++ )
                    _description = _description + " " + e.Args[iter];

                this._admin = e.Sender;
                this._running = true;
                _yes = 0;
                _no = 0;
                _abs = 0;
            }

            this.OnVoteStatusCommand(bot, e);
        }

        private void OnVoteAbortCommand(BotShell bot, CommandArgs e)
        {
            if ( ( this._admin.IndexOf(e.Sender,0) >= 0) ||
                 ( bot.Users.GetUser(e.Sender) >= UserLevel.Leader ) )
            {
                if (!this._running)
                {
                    bot.SendPrivateMessage(e.SenderID, "There is election to abort");
                    return;
                }
                this._running = false;
                this._admin = null;
                lock (this._joined)
                    this._joined.Clear();
                bot.SendPrivateChannelMessage( bot.ColorHighlight + HTML.CreateColorString(bot.ColorHeaderHex, e.Sender) + " has aborted the election" );
                bot.SendPrivateMessage(e.SenderID, "You aborted the election");
            }
            else
            {
                bot.SendPrivateMessage(e.SenderID, e.Sender + " was not the person who started the election and/or does not have sufficient privileges to stop the election.");
                return;
            }
        }

        private void OnVoteCastCommand(BotShell bot, CommandArgs e)
        {
            if (!this._running)
            {
                bot.SendPrivateMessage(e.SenderID, "There is nothing to votes for at this time.");
                return;
            }
            string main = bot.Users.GetMain(e.Sender);
            lock (this._joined)
            {
                if (this._joined.ContainsKey(main))
                {
                    bot.SendPrivateMessage(e.SenderID, "You have already cast your vote in this election.");
                    return;
                }
                this._joined.Add(main, e.Sender);
                switch (e.Command)
                {
                    case "vote yes":
                        _yes++;
                        break;
                    case "vote no":
                        _no++;
                        break;
                    case "vote abstain":
                        _abs++;
                        break;
                }
                
                bot.SendPrivateMessage(e.SenderID, "You have cast your vote.");
            }
        }

        private void OnVoteStopCommand(BotShell bot, CommandArgs e)
        {
            if ( ( this._admin.IndexOf(e.Sender,0) >= 0) ||
                 ( bot.Users.GetUser(e.Sender) >= UserLevel.Leader ) )
            {
                if (!this._running)
                {
                    bot.SendPrivateMessage(e.SenderID, "There is no election to stop");
                    return;
                }
                lock (this._joined)
                {
                    bot.SendPrivateMessage(e.SenderID, "You have ended the election");
                    if ( ( _quorum > 0 ) && ( this._yes + this._no + this._abs < this._quorum ) )
                    {
                        bot.SendPrivateChannelMessage(bot.ColorHighlight + "The election has been aborted due to lack of quorum.");
                    }
                    else
                    {
                        string result = "no";
                        int require = (this._yes + this._no) * (this._percentage / 100);
                        if ( this._yes >= require )
                            result = "yes";

                        bot.SendPrivateChannelMessage( "The election has ended for: " + _description );
                        bot.SendPrivateChannelMessage( "Election result is: " + result );
                    }

                    this._running = false;
                    this._admin = null;
                    this._joined.Clear();
                    this._yes = 0;
                    this._no = 0;
                    this._abs = 0;
                    
                }
            }
            else
            {
                bot.SendPrivateMessage(e.SenderID, e.Sender + " was not the person who started the vote or does not have sufficient privileges to end the voting.");
                return;
            }
        }
    }
}
