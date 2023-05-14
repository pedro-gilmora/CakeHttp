using FluentAssertions;
using HttpServiceClient.UnitTests.Models.Json;
using HttpServiceClient.UnitTests.Models.Xml;
using Xunit.Abstractions;

namespace SourceCrafter.HttpServiceClient.UnitTests;

public class TravellerClientTests : IClassFixture<TravellerClient>
{
    private readonly TravellerClient _client;

    public TravellerClientTests(TravellerClient clientSetup)
    {
        _client = clientSetup;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldGetAllItems(bool useDedicatedService)
    {
        //Just a comment to deploy
        if (await GetTravellers(useDedicatedService) is { } travellers)
        {
            travellers.Should().NotBeNull();
            travellers.Travelers.Items.Should().HaveCount(travellers.PerPage);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldMakeIndexerTheCall(bool useDedicatedService)
    {
        var result = await GetTravellers(useDedicatedService);
        if (result is { Travelers.Items:[ { Id: { } id },..] } && await GetTravelerById(useDedicatedService, id) is { } traveller)
        {
            traveller.Should().NotBeNull();
            traveller.Id.Should().Be(id);
        }
    }

    private async Task<TravelerInformationResponse?> GetTravellers(bool useDedicatedService)
    {
        var _ITravellerActions = useDedicatedService ? new TravellerActionsService() : _client.Traveler;
        return await _ITravellerActions.GetAsync(PetStatus.Pending);
    }

    private async Task<TravelerInformation?> GetTravelerById(bool useDedicatedService, int id)
    {
        var _ITravellerActions = useDedicatedService ? new TravelerByIdService(id) : _client.Traveler[id];
        return await _ITravellerActions.GetAsync();
    }
}