using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace OCD_Tools
{

    [DataContract]
    public class IgnoreParamDictionary
    {
        [DataMember]
        [XmlElement("Item")]
        public List<IgnoreParamEntry> Components { get; set; } = new List<IgnoreParamEntry>();

        public void Add(string componentName, List<string> ignoreParams)
        {
            Components.Add(new IgnoreParamEntry { Key = componentName, Value = ignoreParams });
        }

        public bool TryGetValue(string componentName, out List<string> ignoreParams)
        {
            var entry = Components.Find(e => e.Key == componentName);
            if (entry != null)
            {
                ignoreParams = entry.Value;
                return true;
            }
            ignoreParams = null;
            return false;
        }
        public void Update(string componentName, List<string> ignoreParams)
        {
            var entry = Components.Find(e => e.Key == componentName);
            if (entry != null)
            {
                entry.Value = ignoreParams;
            }
        }
        public void SerializeToXml()
        {
            //file path of IgnoreOutputsParams.xml should be where this assambly file is located
            string filePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "IgnoreOutputsParams.xml");
            var serializer = new XmlSerializer(typeof(IgnoreParamDictionary));
            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, this);
            }
        }

        public static IgnoreParamDictionary DeserializeFromXml()
        {
            string filePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "IgnoreOutputsParams.xml");
            var serializer = new XmlSerializer(typeof(IgnoreParamDictionary));
            using (var reader = new StreamReader(filePath))
            {
                return (IgnoreParamDictionary)serializer.Deserialize(reader);
            }
        }



    }

    [DataContract]
    public class IgnoreParamEntry
    {
        [DataMember]
        public string Key { get; set; }

        [DataMember]
        [XmlArray("Value")]
        [XmlArrayItem("Param")]
        public List<string> Value { get; set; }
    }


}
