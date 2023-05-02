using FluentAssertions;
using HttpServiceClient.UnitTests.Models.Json;
using HttpServiceClient.UnitTests.Models.Xml;

namespace SourceCrafter.HttpServiceClient.UnitTests
{
    public class TravellerClientTests : IClassFixture<TravellerClientTestSetup>
    {
        private readonly TravellerClient _client;

        public TravellerClientTests(TravellerClientTestSetup clientSetup)
        {
            _client = clientSetup._client;
        }

        [Fact]
        public async Task ShouldMakeTheCall()
        {
            //Just a comment to deploy
            var travellers = await _client.Traveler.GetAsync(PetStatus.Pending);
            travellers.Should().NotBeNull();
            travellers.Travelers.Items.Should().HaveCount(travellers.PerPage);
        }

        [Fact]
        public async Task ShouldMakeIndexerTheCall()
        {
            const int id = 11187;
            var traveller = await _client.Traveler[id].GetAsync();
            traveller.Should().NotBeNull();
            traveller.Id.Should().Be(id);
        }
    }

    public class TravellerClientTestSetup : IDisposable
    {
        public TravellerClient _client;

        public TravellerClientTestSetup()
        {
            _client = new();
        }

        public void Dispose()
        {
            _client = null!;
        }
    }
}