using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DevEverywhere.CakeHttp;

public static partial class CakeHttpJsonTests
{
    private static IPetStoreApi _cakeClient = null!;

    [SetUp]
    public static void SetUp()
    {
        _cakeClient = CakeHttp.CreateClient<IPetStoreApi>();
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
        User user = new() { Username = $"pedro.gilmora_{guid}", Email = $"test_{guid}@test.com", Password = "!CakeHTTP." };
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
        FileInfo file = new("C:\\Users\\Pedro\\Pictures\\136403436_193298835812344_2020814743489326655_n.jpg");
        var pets = await GetPetsAsync();
        var serverResponse = await _cakeClient.Pet[pets[0].Id].UploadImage.PostAsync(file);
        serverResponse.Code.Should().Be(200);
    }

    [TearDown]
    public static void TearDown()
    {
    }
}
