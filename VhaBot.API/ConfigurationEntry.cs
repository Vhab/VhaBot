using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class ConfigurationEntry
    {
        private ConfigType _type;
        private string _section;
        private string _key;
        private string _name;
        private object _defaultValue;
        private object[] _values;

        public ConfigurationEntry(ConfigType type, string section, string key, string name, object defaultValue, object[] values)
        {
            this._type = type;
            this._section = section;
            this._key = key;
            this._name = name;
            this._defaultValue = defaultValue;
            this._values = values;
        }

        public ConfigType Type { get { return this._type; } }
        public string Section { get { return this._section; } }
        public string Key { get { return this._key; } }
        public string Name { get { return this._name; } }
        public object DefaultValue { get { return this._defaultValue; } }
        public object[] Values { get { return this._values; } }
        public string[] StringValues
        {
            get
            {
                if (this.Type != ConfigType.String && this.Type != ConfigType.Integer)
                    return null;
                if (this.Values == null || this.Values.Length == 0)
                    return null;
                List<string> list = new List<string>();
                foreach (object value in this.Values)
                {
                    if (this.Type == ConfigType.String)
                        list.Add((string)value);
                    else
                        list.Add(((int)value).ToString());
                }
                return list.ToArray();
            }
        }
    }
}
