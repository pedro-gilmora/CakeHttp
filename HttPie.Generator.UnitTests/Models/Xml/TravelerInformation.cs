using System.Xml.Serialization;

namespace HttPie.Generator.UnitTests.Models.Xml
{
    [XmlRoot("Travelerinformation"), Serializable]
    public class TravelerInformation : Traveler { }
}