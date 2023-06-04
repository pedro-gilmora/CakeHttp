using System.Xml.Serialization;

namespace AppsLoveWorld.Xml
{
    public class TravelersContainer
    {
        [XmlElement("Travelerinformation")]
        public List<Traveler> Items { get; } = new();
    }
}