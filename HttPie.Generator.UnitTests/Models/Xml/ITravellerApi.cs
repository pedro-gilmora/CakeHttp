using HttPie.Attributes;
using HttPie.Enums;
using HttPie.Generator.UnitTests.Models.Json;

namespace HttPie.Generator.UnitTests.Models.Xml;

[HttpOptions("http://restapi.adequateshop.com/api/", QueryCasing = Casing.CamelCase)]
public interface ITravellerApi
{
    ITravellerActions Traveler { get; }
}

public interface ITravellerActions : IGet< /*status*/ PetStatus, XmlResponse<TravelerInformationResponse>>
{
    ITravelerActionsById this[int id] { get; }
}
public interface ITravelerActionsById: IGet<XmlResponse<TravelerInformation>>
{
}