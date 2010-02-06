using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Mdb;
using AoLib.Utils;

namespace VhaBot
{
    public class MessageArgs : SenderArgs
    {
        protected bool _self;
        protected string[] _args = new string[0];
        protected string[] _words = new string[0];
        protected AoItem[] _items = new AoItem[0];
        private string __message;
        protected string _message
        {
            get { return this.__message; }
            set
            {
                this.__message = value;
                if (value == null || value == string.Empty)
                    return;

                try
                {
                    if (value.StartsWith("~&") && value.EndsWith("~"))
                    {
                        DescrambledMessage descrambled = Descrambler.Decode(value);
                        this._descrambled = descrambled;
                        if (descrambled.Message != null && descrambled.Message != string.Empty)
                        {
                            this._message = descrambled.Message;
                            this._isDescrambled = true;
                        }
                    }
                }
                catch { }

                this._args = value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (this._args == null)
                    this._args = new string[0];

                this._words = new string[this._args.Length];
                for (int i = 0; i < this._args.Length; i++)
                {
                    this._words[i] = String.Join(" ", this._args, i, this._args.Length - i);
                }
                this._items = AoItem.ParseString(value);
            }
        }
        protected bool _isDescrambled = false;
        protected DescrambledMessage _descrambled = null;

        public bool Self { get { return this._self; } }
        public string Message { get { return this._message; } }
        public bool IsDescrambled { get { return this._isDescrambled; } }
        public DescrambledMessage Descrambled { get { return this._descrambled; } }
        public string[] Args { get { return this._args; } }
        public string[] Words { get { return this._words; } }
        public AoItem[] Items { get { return this._items; } }
    }
}
