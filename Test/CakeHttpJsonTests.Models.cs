using System;
using System.Xml.Serialization;
using System.Collections.Generic;

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

public class TravelersContainer
{
    [XmlElement("Travelerinformation")]
    public List<Traveler> Items { get; } = new();
}

[XmlRoot("Travelerinformation"), Serializable]
public class TravelerInformation : Traveler { }

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