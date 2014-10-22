using System;
using System.Collections.Generic;
using System.Text;

namespace ASCompletion.Completion
{
    public class MetaEntry : ICloneable
    {
        [Flags]
        public enum DecoratableField : byte
        {
            Class = 1,
            Attribute = 2,
            Function = 4
        }

        public DecoratableField DecoratableFields { get; set; }

        public string Label { get; set; }

        public List<MetaField> Fields { get; set; }

        private string _defaultDescriptionKey;  // I'd prefer to use an OrderedDictionary, but I guess it would be a bit overkill for just this case
        public string DefaultDescriptionKey {
            get
            {
                if (_defaultDescriptionKey == null && Description != null && Description.Count > 0)
                    return GetFirstDescription().Key;

                return _defaultDescriptionKey;
            }
            set
            {
                if (value != null && (Description == null || !Description.ContainsKey(_defaultDescriptionKey)))
                    throw new ArgumentException("They key must be present in the Description collection");

                _defaultDescriptionKey = value;
            }
        }

        public Dictionary<string, string> Description { get; set; }

        public bool AppearsInFieldHelp { get; set; }

        private KeyValuePair<string, string> GetFirstDescription()
        {
            foreach (var entry in Description)
            {
                return entry;
            }

            return default(KeyValuePair<string, string>);
        }

        public object Clone()
        {
            MetaEntry retVal = new MetaEntry()
                {
                    AppearsInFieldHelp = AppearsInFieldHelp,
                    DecoratableFields = DecoratableFields,
                    Label = Label
                };

            if (Description != null)
                retVal.Description = new Dictionary<string, string>(Description);
            retVal.DefaultDescriptionKey = _defaultDescriptionKey;

            if (Fields != null)
            {
                retVal.Fields = new List<MetaField>(Fields.Count);
                foreach (var field in Fields)
                    retVal.Fields.Add((MetaField)field.Clone());
            }

            return retVal;
        }

    }

    public class MetaField : ICloneable
    {
        
        public bool Mandatory {get; set; }

        public string Name { get; set; }

        // TODO?: Type? Pattern? Enumeration? We could do it, but we'd need to add validation logic to the parsing part or somewhere else, let's see what people say

        public object Clone()
        {
            return new MetaField() {Mandatory = Mandatory, Name = Name};
        }

    }
}
