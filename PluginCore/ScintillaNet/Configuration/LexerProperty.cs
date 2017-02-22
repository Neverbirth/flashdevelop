using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ScintillaNet.Configuration
{
    [Serializable()]
    public class LexerProperty : ConfigItem
    {
        [XmlAttribute("key")]
        public string key;

        [XmlAttribute("value")]
        public string value;

        [XmlAttribute("default")]
        public string defaultValue;
    }
}
