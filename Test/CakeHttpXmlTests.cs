using System;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;
using DevEverywhere.CakeHttp.Attributes;

namespace DevEverywhere.CakeHttp;

public static partial class CakeHttpXmlTests
{
    private static ITravellerApi _cakeClient = null!;

    [SetUp]
    public static void SetUp()
    {
        _cakeClient = CakeHttp.CreateClient<ITravellerApi>()!;
    }

    [Test]
    public static async Task ShouldMakeTheCall()
    {
        //Just a comment to deploy
        var travellers = await _cakeClient.Traveler.GetAsync(new { page = 1 });
        travellers.Should().NotBeNull();
        travellers.Travelers.Items.Should().HaveCount(travellers.PerPage);
    }

    [Test]
    public static async Task ShouldMakeIndexerTheCall()
    {
        const int Id = 11187;
        var traveller = await _cakeClient.Traveler[Id].GetAsync();
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
