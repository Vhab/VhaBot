using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class SlaveArgs
    {
        private string _slave;
        private Int32 _slaveID;

        public SlaveArgs(string slave, Int32 slaveID)
        {
            this._slave = slave;
            this._slaveID = slaveID;
        }

        public string Slave { get { return this._slave; } }
        public Int32 SlaveID { get { return this._slaveID; } }
    }
}
