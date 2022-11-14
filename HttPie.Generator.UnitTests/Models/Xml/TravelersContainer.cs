using System.Xml.Serialization;

namespace HttPie.Generator.UnitTests.Models.Xml
{
    public class TravelersContainer
    {
        [XmlElement("Travelerinformation")]
        public List<Traveler> Items { get; } = new();
    }
}