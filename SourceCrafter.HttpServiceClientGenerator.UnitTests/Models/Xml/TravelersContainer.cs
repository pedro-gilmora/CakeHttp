using System.Xml.Serialization;

namespace HttpServiceClient.UnitTests.Models.Xml
{
    public class TravelersContainer
    {
        [XmlElement("Travelerinformation")]
        public List<Traveler> Items { get; } = new();
    }
}