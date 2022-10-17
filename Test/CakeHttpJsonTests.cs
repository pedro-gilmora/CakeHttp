using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CakeHttp;

public static partial class CakeHttpJsonTests
{
    private static IPetStoreApi _requester = null!;

    [SetUp]
    public static void SetUp()
    {
        _requester = CakeHttp.CreateClient<IPetStoreApi>()!;
    }

    [Test]
    public static async Task ShouldMakeTheCall()
    {
        var inventory = await _requester.Store.Inventory.GetAsync();
        inventory.Should().BeOfType<Dictionary<string, long>>();
    }

    [Test]
    public static async Task ShouldGetPets()
    {
        var pets = await GetPetsAsync();
        pets.Should().BeOfType<List<Pet>>();
    }

    private static Task<List<Pet>> GetPetsAsync()
    {
        return _requester.Pet.FindByStatus.GetAsync(PetStatus.Available);
    }

    [Test]
    public static async Task ShouldMakeTheCallWithIndexer()
    {
        FileInfo file = new("C:\\Users\\Pedro\\Pictures\\136403436_193298835812344_2020814743489326655_n.jpg");
        var pets = await GetPetsAsync();
        var serverResponse = await _requester.Pet[pets[0].Id].UploadImage.PostAsync(file);
        serverResponse.Code.Should().Be(200);
    }

    [TearDown]
    public static void TearDown()
    {
    }
}
