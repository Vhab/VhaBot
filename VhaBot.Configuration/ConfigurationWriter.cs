using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace VhaBot.Configuration
{
    public static class ConfigurationWriter
    {
        private static string ToXml(ConfigurationBase config)
        {
            string xml = null;
            MemoryStream stream = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationBase));
                stream = new MemoryStream();
                serializer.Serialize(stream, config);
                xml = Encoding.UTF8.GetString(stream.ToArray());
            }
            catch { }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
            return xml;
        }

        public static bool Write(string file, ConfigurationBase config)
        {
            string xml = ToXml(config);
            if (xml == null)
                return false;

            try
            {
                if (File.Exists(file + ".bak"))
                    File.Delete(file + ".bak");

                if (File.Exists(file))
                    File.Move(file, file + ".bak");
            }
            catch { return false; }

            StreamWriter writer = null;
            try
            {
                writer = File.CreateText(file);
                writer.Write(xml);
            }
            catch { return false; }
            finally
            {
                if (writer != null)
                    writer.Close();
            }

            try
            {
                if (File.Exists(file + ".bak"))
                    File.Delete(file + ".bak");
            }
            catch { }
            return true;
        }
    }
}
