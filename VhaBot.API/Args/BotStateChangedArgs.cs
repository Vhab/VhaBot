using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class BotStateChangedArgs : EventArgs
    {
        private BotState _state;
        private bool _isSlave;
        private Int32 _id;
        private string _character;

        public BotStateChangedArgs(BotState state, bool isSlave, Int32 id, string character)
        {
            this._state = state;
            this._isSlave = isSlave;
            this._id = id;
            this._character = character;
        }

        public BotState State { get { return this._state; } }
        public bool IsSlave { get { return this._isSlave; } }
        public Int32 ID { get { return this._id; } }
        public string Character { get { return this._character; } }
    }
}
