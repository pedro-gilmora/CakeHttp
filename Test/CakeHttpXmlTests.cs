using System;
using CakeHttp;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CakeHttp;

public static partial class CakeHttpXmlTests
{
    private static ITravellerApi _requester = null!;

    [SetUp]
    public static void SetUp()
    {
        _requester = CakeHttp.CreateClient<ITravellerApi>()!;
    }

    [Test]
    public static async Task ShouldMakeTheCall()
    {
        var travellers = await _requester.Traveler.GetAsync(new { page = 1 });
        travellers.Should().NotBeNull();
        travellers.Travelers.Items.Should().HaveCount(travellers.PerPage);
    }

    [Test]
    public static async Task ShouldMakeIndexerTheCall()
    {
        const int Id = 11187;
        var traveller = await _requester.Traveler[Id].GetAsync();
        traveller.Should().NotBeNull();
        traveller.Id.Should().Be(Id);
    }

}

[CakeHttpOptions("http://restapi.adequateshop.com/api/", true)]
public interface ITravellerApi
{
    ITravellerActions Traveler { get; }
}

public interface ITravellerActions  { 
    Task<TravelerInformationResponse> GetAsync<TQuery>(TQuery query); 
    ITravelerActionsById this[int id] { get; }
}

public interface ITravelerActionsById
{
    Task<TravelerInformation> GetAsync();
}

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
public class Traveler { 

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
