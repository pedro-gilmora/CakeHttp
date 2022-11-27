using HttPie.Attributes;
using HttPie.Enums;
using HttPie.Generator.UnitTests.Models.Json;

namespace HttPie.Generator.UnitTests.Models.Xml;

[HttpOptions(
    "http://restapi.adequateshop.com/api/", 
    QueryCasing = Casing.CamelCase, 
    DefaultBodyType = BodyType.Xml, 
    DefaultResponseType = ResponseType.Xml)]
public interface ITravellerApi
{
    ITravellerActions Traveler { get; }
}

public interface ITravellerActions :
    // queryParamName: status
    IGet<PetStatus, TravelerInformationResponse>
{
    ITravelerActionsById this[int id] { get; }
}
public interface ITravelerActionsById: IGet<TravelerInformation>
{
}