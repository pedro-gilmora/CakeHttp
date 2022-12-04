using FluentAssertions;
using HttPie.Generator.UnitTests.Models.Json;
using HttPie.Generator.UnitTests.Models.Json.Client;

namespace HttPie.Generator.UnitTests
{
    public class PetStoreClientTests : IClassFixture<PetStoreClientTestSetup>
    {
        private readonly PetStoreClient _client;

        public PetStoreClientTests(PetStoreClientTestSetup clientSetup)
        {
            _client = clientSetup._client;
        }

        [Fact]
        public async void ShouldGetStoreInventoryIndex()
        {
            var e = await _client.Store.Inventory.GetAsync();
            e.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task ShouldPostUser()
        {
            Guid guid = Guid.NewGuid();
            User user = new() { Username = $"test_{guid}", Email = $"test_{guid}@test.com", Password = "!HttPie2022." };
            var response = await _client.User.PostAsync(user);
            response.Should().BeOfType<ApiResponse>().Subject.Code.Should().Be(200);
        }

        [Fact]
        public async void ShouldGetPets()
        {
            var pets = await GetPetsAsync();
            pets.Should().BeOfType<List<Pet>>().And.HaveCountGreaterThan(0);
        }

        public async Task<List<Pet>> GetPetsAsync()
        {
            return await _client.Pet.All.GetAsync(PetStatus.Pending);
        }

        [Fact]
        public async Task ShouldMakeTheCallWithIndexer()
        {
            FileInfo file = new("perrito.jpeg");
            var pets = await GetPetsAsync();
            pets.Should().HaveCountGreaterThan(0);
            var serverResponse = await _client.Pet[pets[0].Id].UploadImage.PostAsync(file);
            serverResponse.Should().BeOfType<ApiResponse>().Subject.Code.Should().Be(200);
        }
    }

    public class PetStoreClientTestSetup : IDisposable
    {
        public PetStoreClient _client;

        public PetStoreClientTestSetup()
        {
            _client = new();
        }

        public void Dispose()
        {
            _client = null!;
        }
    }
}