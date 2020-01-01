using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Plugins.RTC.Plugin
{
    public class Settings
    {
        public bool EnableMasterLogging { get; set; }
        public bool EnableSlaveLogging { get; set; }
        public bool EnableBusLogging { get; set; }
        public bool TruncateLogsOnStartup { get; set; }
        public string MasterLogFile { get; set; }
        public string SlaveLogFile { get; set; }
        public string BusLogFile { get; set; }

        public Settings()
        {
            MasterLogFile = "I2CMasterLog.txt";
            SlaveLogFile = "DS1307Log.txt";
            BusLogFile = "I2CBusLog.txt";
        }

        public string ToXML()
        {
            string output = "";
            var xmlSerializer = new XmlSerializer(GetType());
            using (var memoryStream = new MemoryStream())
            using (var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
            {
                xmlTextWriter.Formatting = Formatting.Indented;
                xmlSerializer.Serialize(xmlTextWriter, this);
                output = Encoding.UTF8.GetString(memoryStream.ToArray());
                string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                if (output.StartsWith(_byteOrderMarkUtf8))
                    output = output.Remove(0, _byteOrderMarkUtf8.Length);
            }
            return output;
        }

        public static Settings Load(string OptionalFileNameAndPath = null)
        {
            Settings settings;
            try
            {
                string fn = string.IsNullOrWhiteSpace(OptionalFileNameAndPath) ? GetFileName() : OptionalFileNameAndPath;
                string xml = File.ReadAllText(fn);
                var reader = new StringReader(xml);
                using (reader)
                {
                    var serializer = new XmlSerializer(typeof(Settings));
                    settings = (Settings)serializer.Deserialize(reader);
                    reader.Close();
                }
            }
            catch (Exception /*ex*/)
            {
                settings = new Settings();
            }
            return settings;
        }

        public bool Save(string OptionalFileNameAndPath = null)
        {
            try
            {
                string fn = string.IsNullOrWhiteSpace(OptionalFileNameAndPath) ? GetFileName() : OptionalFileNameAndPath;
                File.WriteAllText(fn, ToXML());
                return true;
            }
            catch (Exception /*ex*/)
            {
                return false;
            }
        }

        public static string GetFileName()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            var dll = Assembly.GetAssembly(typeof(Settings)).ManifestModule.ScopeName + ".config";
            return Path.Combine(path, dll);
        }
    }
}
