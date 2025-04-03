using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Plugins.UARTReplacement
{
    public class Settings
    {
        private Dictionary<int, int> _bauds;
        public string EspPortName { get; set; }
        public string PiPortName { get; set; }
        public string PiMapGpio4And5ToDtrAndRtsEnable { get; set; }
        public BaudRateSubstitutionClass BaudRateSubstitutions { get; set; }

        public Settings()
        {
            EspPortName = "COM1";
            PiPortName = "COM2";
            PiMapGpio4And5ToDtrAndRtsEnable = "False";
            BaudRateSubstitutions = new BaudRateSubstitutionClass();
            BaudRateSubstitutions.List = new Substitution[0];
            _bauds = new Dictionary<int, int>();
        }

        [XmlType("BaudRateSubstitutions")]
        public class BaudRateSubstitutionClass
        {
            [XmlAttribute("enableForEsp")]
            public string EnableForEspText { get; set; }

            [XmlAttribute("enableForPi")]
            public string EnableForPiText { get; set; }

            [XmlElement("Substitution")]
            public Substitution[] List { get; set; }

            [XmlIgnore]
            public bool EspEnabled
            {
                get
                {
                    return (EnableForEspText ?? "").Trim().ToLower() == "true";
                }
            }

            [XmlIgnore]
            public bool PiEnabled
            {
                get
                {
                    return (EnableForPiText ?? "").Trim().ToLower() == "true";
                }
            }
        }

        public class Substitution
        {
            [XmlAttribute("use")]
            public string UseText { get; set; }

            [XmlAttribute("for")]
            public string ForText { get; set; }

            [XmlIgnore]
            public int Use
            {
                get
                {
                    int.TryParse((UseText ?? "").Trim(), out int value);
                    return value > 0 ? value : 0;
                }
            }

            [XmlIgnore]
            public int For
            {
                get
                {
                    int.TryParse((ForText ?? "").Trim(), out int value);
                    return value > 0 ? value : 0;
                }
            }

            public bool IsValid() 
                => Use > 0 && For > 0 && Use != For;
        }

        public bool GetPiMapGpio4And5ToDtrAndRtsEnable()
        {
            return (PiMapGpio4And5ToDtrAndRtsEnable ?? "").Trim().ToLower() == "true";
        }

        public Settings RemoveInvalidSubstitutions()
        {
            foreach (var item in BaudRateSubstitutions.List)
            {
                if (!item.IsValid())
                    Console.Error.WriteLine($"{UARTReplacement_Device.PluginName}Invalid baud rate substitution, can't use \"{item.UseText}\" for \"{item.ForText}\"");
                else if (_bauds.ContainsKey(item.For))
                    Console.Error.WriteLine($"{UARTReplacement_Device.PluginName}Duplicate baud rate substitution, can't use \"{item.UseText}\" for \"{item.ForText}\"");
                else
                    _bauds.Add(item.For, item.Use);
            }
            return this;
        }

        private void Init()
        {
            if (BaudRateSubstitutions is null)
                BaudRateSubstitutions = new BaudRateSubstitutionClass();
            if (BaudRateSubstitutions.List is null)
                BaudRateSubstitutions.List = new Substitution[0];
            if (_bauds is null)
                _bauds = new Dictionary<int, int>();
        }

        public int GetBaud(int originalBaud, UARTTargets target)
        {
            if (target == UARTTargets.ESP && !BaudRateSubstitutions.EspEnabled)
                return originalBaud;
            if (target == UARTTargets.Pi && !BaudRateSubstitutions.PiEnabled)
                return originalBaud;
            else if (_bauds.ContainsKey(originalBaud))
                return _bauds[originalBaud];
            else
                return originalBaud;
        }

        #region Serialization
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
            if (settings is null)
                settings = new Settings();
            settings.Init();
            return settings;
        }

        public bool Save(string OptionalFileNameAndPath = null)
        {
            try
            {
                Init();
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

        #endregion Serialization
    }
}
