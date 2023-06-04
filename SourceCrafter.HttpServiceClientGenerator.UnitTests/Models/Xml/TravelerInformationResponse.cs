using System.Xml.Serialization;

namespace AppsLoveWorld.Xml
{
    [Serializable, XmlRoot("TravelerinformationResponse")]
    public class TravelerInformationResponse
    {
        [XmlElement("page")]
        public int Page { get; set; }
        [XmlElement("per_page")]
        public int PerPage { get; set; }
        [XmlElement("totalrecord")]
        public int TotalRecord { get; set; }
        [XmlElement("total_pages")]
        public int TotalPages { get; set; }

        [XmlElement("travelers")]
        public TravelersContainer Travelers { get; set; } = null!;
    }
}