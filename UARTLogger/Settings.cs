using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Plugins.UARTLogger
{
    public class Settings
    {
        private const int FLUSH_DEFAULT = 2;
        public bool EnableESPLogging { get; set; }
        public bool EnablePiLogging { get; set; }
        public bool TruncateLogsOnStartup { get; set; }
        public string ESPLogFile { get; set; }
        public string PiLogFile { get; set; }
        public int FlushLogsAfterSecs { get; set; }

        public Settings()
        {
            ESPLogFile = PiLogFile = string.Empty;
            FlushLogsAfterSecs = FLUSH_DEFAULT;
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
            catch (Exception)
            {
                settings = new Settings();
            }
            if (settings.FlushLogsAfterSecs <= 0)
                settings.FlushLogsAfterSecs = FLUSH_DEFAULT;
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
            catch (Exception)
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
