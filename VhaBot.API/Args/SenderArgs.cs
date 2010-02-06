using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;
using AoLib.Net;

namespace VhaBot
{
    public abstract class SenderArgs : EventArgs
    {
        protected Server _dimension = Server.Test;
        protected UInt32 _senderID;
        protected WhoisResult _senderWhois = null;
        private bool _senderWhoisDone = false;
        protected string _sender;

        public UInt32 SenderID { get { return this._senderID; } }
        public WhoisResult SenderWhois
        {
            get
            {
                if (!this._senderWhoisDone)
                {
                    if (this._dimension == Server.Test) return null;
                    if (this._sender == null || this._sender == string.Empty) return null;
                    WhoisResult whois = XML.GetWhois(this._sender, this._dimension);
                    if (whois != null && whois.Success) this._senderWhois = whois;
                    this._senderWhoisDone = true;
                }
                return this._senderWhois;
            }
        }
        public string Sender { get { return this._sender; } }
    }
}
