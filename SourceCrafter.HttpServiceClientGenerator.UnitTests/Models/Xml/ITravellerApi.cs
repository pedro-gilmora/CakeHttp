using HttpServiceClient.UnitTests.Models.Json;
using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Operations;

namespace HttpServiceClient.UnitTests.Models.Xml;

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