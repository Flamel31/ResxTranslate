using System.Collections.Concurrent;
using System.Xml.Linq;

namespace ResxTranslate.Models.ResX
{
    public class ResXContainer : ConcurrentDictionary<string, ResXData>
    {
        public static readonly string DefaultLanguage = "en";

        private readonly XElement Root;

        private readonly string Filepath;
        public string Language { get; private set; }

        public ResXContainer(string filepath, string language) : base()
        {
            Filepath = filepath;
            Language = language;
            Root = XElement.Load(filepath);
            ExtractKeyValuePairs();
        }

        public ResXContainer(string filepath) : this(filepath, DefaultLanguage) 
        { 
        }

        private IEnumerable<XElement> GetDataElements()
        {
            return Root.Elements("data").Where(e => e.Attribute("type") == null);
        }

        private void ExtractKeyValuePairs()
        {
            foreach (var element in GetDataElements())
            {
                string key = element.Attribute("name").Value;
                this[key] = new ResXData
                {
                    Name = key,
                    Value = element.Element("value").Value,
                    Comment = element.Element("comment")?.Value,
                    Type = element.Attribute("type")?.Value,
                    Space = element.Attribute(XName.Get("space", XNamespace.Xml.ToString()))?.Value,
                };
                
            }
        }

        public void Save()
        {
            // Remove all the current elements
            foreach (var item in GetDataElements())
                item.Remove();

            // Add the elements back in sorted order
            foreach (var kvp in this.OrderBy(x => x.Key))
            {
                var data = new XElement("data", new XElement("value", kvp.Value.Value));
                data.SetAttributeValue("name", kvp.Key);
                if (!string.IsNullOrEmpty(kvp.Value.Comment)) data.SetElementValue("comment", kvp.Value.Comment);
                if (!string.IsNullOrEmpty(kvp.Value.Type)) data.SetAttributeValue("type", kvp.Value.Type);
                if (!string.IsNullOrEmpty(kvp.Value.Space)) data.SetAttributeValue(XName.Get("space", XNamespace.Xml.ToString()), kvp.Value.Space);

                Root.Add(data);
            }

            Root.Save(new FileStream(Filepath, FileMode.OpenOrCreate, FileAccess.Write));
        }
    }
}
