using System.Xml.Serialization;

namespace HttPie.Generator.UnitTests.Models.Xml
{

    [XmlRoot("traveler")]
    public class Traveler
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("name")]
        public string Name { get; set; } = default!;

        [XmlElement("email")]
        public string Email { get; set; } = default!;

        [XmlElement("adderes")]
        public string Address { get; set; } = default!;

        [XmlElement("createdat")]
        public DateTime CreatedDate { get; set; }
    }
}