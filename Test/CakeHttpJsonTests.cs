using System;
using System.IO;
using NUnit.Framework;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevEverywhere.CakeHttp;


public static partial class CakeHttpJsonTests
{
    private readonly static IPetStoreApi _cakeClient = CakeHttp.CreateClient<IPetStoreApi>();

    [SetUp]
    public static void SetUp()
    {
    }

    [Test]
    public static async Task ShouldMakeTheCall()
    {
        var inventory = await _cakeClient.Store.Inventory.GetAsync();
        inventory.Should().BeOfType<Dictionary<string, long>>();
    }

    [Test]
    public static async Task ShouldPostUser()
    {
        Guid guid = Guid.NewGuid();
        User user = new() { Username = $"test_{guid}", Email = $"test_{guid}@test.com", Password = "!CakeHTTP." };
        var response = await _cakeClient.User.PostAsync(user);
        response.Should().BeOfType<ApiResponse>().Subject.Code.Should().Be(200);
    }

    [Test]
    public static async Task ShouldGetPets()
    {
        var pets = await GetPetsAsync();
        pets.Should().BeOfType<List<Pet>>();
    }

    private static Task<List<Pet>> GetPetsAsync()
    {
        return _cakeClient.Pet.FindByStatus.GetAsync(PetStatus.Available);
    }

    [Test]
    public static async Task ShouldMakeTheCallWithIndexer()
    {
        FileInfo file = new("perrito.jpeg");
        var pets = await GetPetsAsync();
        var serverResponse = await _cakeClient.Pet[pets[0].Id].UploadImage.PostAsync(file);
        serverResponse.Code.Should().Be(200);
    }

    [TearDown]
    public static void TearDown()
    {
    }
}
