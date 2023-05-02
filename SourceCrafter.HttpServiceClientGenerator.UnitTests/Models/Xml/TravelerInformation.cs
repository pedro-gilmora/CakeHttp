using System.Xml.Serialization;

namespace HttpServiceClient.UnitTests.Models.Xml
{
    [XmlRoot("Travelerinformation"), Serializable]
    public class TravelerInformation : Traveler { }
}