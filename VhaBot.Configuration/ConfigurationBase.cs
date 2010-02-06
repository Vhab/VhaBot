using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace VhaBot.Configuration
{
    [XmlRoot("Configuration"), Serializable]
    public class ConfigurationBase
    {
        [XmlElement("Core")]
        public ConfigurationCore Core;
        [XmlElement("Bot")]
        public ConfigurationBot[] Bots;
    }
}
