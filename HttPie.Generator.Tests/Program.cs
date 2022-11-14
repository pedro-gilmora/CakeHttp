using System.Text.Json.Serialization;
using System.Text.Json;
using HttPie.Generator.Tests;
using HttPie.Generator;

//new PetStoreAgent()._httpClient.BaseAddress.AbsolutePath
var client = new PetStoreClient();
Dictionary<string, long> e = await client.Store.Inventory.GetAsync();

Console.WriteLine(JsonSerializer.Serialize(e));
//client.