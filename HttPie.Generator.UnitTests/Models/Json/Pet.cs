
using HttPie.Generator.UnitTests.Models.Json;

public class Pet
{
    public long Id { get; set; }
    public Category Category { get; set; }
    public string Name { get; set; }
    public string[] PhotoUrls { get; set; }
    public Tag[] Tags { get; set; }
    public PetStatus Status { get; set; }
}
