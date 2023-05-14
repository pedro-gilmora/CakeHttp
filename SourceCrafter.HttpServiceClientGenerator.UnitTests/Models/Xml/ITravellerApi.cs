using HttpServiceClient.UnitTests.Models.Json;
using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Operations;

namespace HttpServiceClient.UnitTests.Models.Xml;

[HttpService("http://restapi.adequateshop.com/api/", DefaultFormat = ResultFormat.Xml)]
public interface ITravellerApi
{
    ITravellerActions Traveler { get; }
}

public partial interface ITravellerActions : IHttpGet<(PetStatus status, TravelerInformationResponse)>
{
    [ServiceDescription("TravelerById")]
    IHttpGet<Result<TravelerInformation>> this[int id] { get; }
}