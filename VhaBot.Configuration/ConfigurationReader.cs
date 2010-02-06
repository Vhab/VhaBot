using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace VhaBot.Configuration
{
    public static class ConfigurationReader
    {
        public static ConfigurationBase Read(string file)
        {
            ConfigurationBase config = null;
            try
            {
                
                string xml = File.ReadAllText(file);
                config = ConfigurationReader.FromString(xml);
            }
            catch { }
            return config;
        }

        private static ConfigurationBase FromString(string xml)
        {
            ConfigurationBase config = null;
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationBase));
                config = (ConfigurationBase)serializer.Deserialize(stream);
            }
            catch { }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return config;
        }
    }
}
