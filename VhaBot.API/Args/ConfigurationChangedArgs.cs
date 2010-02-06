using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class ConfigurationChangedArgs
    {
        private ConfigType _type;
        private string _section;
        private string _key;
        private object _value;

        public ConfigurationChangedArgs(ConfigType type, string section, string key, object value)
        {
            this._type = type;
            this._section = section;
            this._key = key;
            this._value = value;
        }

        public ConfigType Type { get { return this._type; } }
        public string Section { get { return this._section; } }
        public string Key { get { return this._key; } }
        public object Value { get { return this._value; } }
    }
}
