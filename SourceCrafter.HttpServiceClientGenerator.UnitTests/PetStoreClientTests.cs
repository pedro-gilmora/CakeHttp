using FluentAssertions;
using SmartBear.Json;
using Newtonsoft.Json.Converters;
using SourceCrafter.HttpServiceClient.Operations;
using System.Xml.Linq;

namespace SourceCrafter.HttpServiceClient.UnitTests
{
    public class PetStoreClientTests : IClassFixture<PetStoreClient>
    {
        private readonly PetStoreClient _client;

        public PetStoreClientTests(PetStoreClient client)
        {
            _client = client;
        }

        [Theory(DisplayName = "Should create an user")]
        [InlineData(true)]
        [InlineData(false)]
        public async void ShouldGetStoreInventoryIndexService(bool useDedicatedService)
        {
            var e = await (useDedicatedService ? new InventoryService() : _client.Store.Inventory)
                .GetAsync();

            e.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }


        [Theory(DisplayName = "Should create an user")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldPostUser(bool useDedicatedService)
        {
            var guid = Guid.NewGuid().ToString().Replace("-","");
            
            User user = new() { Username = $"test_{guid}", Email = $"test_{guid}@test.com", Password = "!HttPie2022." };
            
            var response = await (useDedicatedService ? new UserService() : _client.User).PostAsync(user);

            response.Should().BeOfType<ApiResponse>().Subject.Code.Should().Be(200);
        }

        [Theory(DisplayName = "Should create a pet")]
        [InlineData(true)]
        [InlineData(false)]
        public async void ShouldCreatePet(bool useDedicatedService)
        {
            if (await CreatePetAsync(useDedicatedService, "Nala") is { } result)
            {
                result.Name.Should().StartWith("Nala ");

                await DeletePetAsync(useDedicatedService, result!.Id);
            }
        }

        private Task<Pet?> CreatePetAsync(bool useDedicatedService, string name)
        {
            var service = useDedicatedService ? new PetService() : _client.Pet;

            Pet pet = new() { 
                Name = name + ' ' + Guid.NewGuid(), 
                Category = new() { Name = "Dog" }, 
                Status = PetStatus.Available 
            };

            return service.PostAsync(pet);
        }

        //[Theory(DisplayName = "Should get available pets")]
        //[InlineData(true)]
        //[InlineData(false)]
        //public async void ShouldGetPets(bool useDedicatedService)
        //{
        //    var pet = await CreatePetAsync(useDedicatedService, "Nala");

        //    var pets = await GetPetsAsync(useDedicatedService);

        //    pets.Should().BeOfType<List<Pet>>().And.HaveCountGreaterThan(0);

        //    await DeletePetAsync(useDedicatedService, pet!.Id);
        //}

        private Task<ApiResponse?> DeletePetAsync(bool useDedicatedService, long id)
            => (useDedicatedService ? new PetByIdService(id) : _client.Pet[id]).DeleteAsync();

        async Task<List<Pet>?> GetPetsAsync(bool useDedicatedService)
        {
            return await (useDedicatedService ? new FindByStatusService() : _client.Pet.FindByStatus)
                .GetAsync(PetStatus.Pending);
        }

        [Theory(DisplayName = "Should upload an image for some pet")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldMakeTheCallWithIndexer(bool useDedicatedService)
        {
            if (await CreatePetAsync(useDedicatedService, "Nala") is { Id: var firstPetId })
            {
                FileInfo file = new("perrito.jpeg");

                var serverResponse = await (useDedicatedService ? new UploadImageService(firstPetId) : _client.Pet[firstPetId].UploadImage)
                    .PostAsync(file);

                serverResponse.Should().BeOfType<ApiResponse>().Subject.Code.Should().Be(200);

                await DeletePetAsync(useDedicatedService, firstPetId);

            }
        }
    }
}