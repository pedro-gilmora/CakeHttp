using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Operations;

namespace SmartBear.Json
{

    [HttpService("https://petstore.swagger.io/v2/")]
    public partial interface IPetStoreApi
    {
        IStore Store { get; }
        IPet Pet { get; }
        IUser User { get; }
    }

    public partial interface IStore
    {
        IOrder Order { get; }
        IHttpGet<Result<Dictionary<string, long>>> Inventory { get;}

    }

    public partial interface IPet: IHttpPost<(Pet body, Pet)>
    {
        IPetById this[long petId] { get; }
        IHttpGet<(PetStatus status, List<Pet>)> FindByStatus { get; }
    }

    public partial interface IOrder : IHttpPost<(Order body, Order)>
    {
        IOrderById this[int orderId] { get; }
    }

    public partial interface IOrderById :
        IHttpGet<Result<Order>>,
        IHttpDelete<Result<ApiResponse>>
    { }

    public partial interface IPetById :
        IHttpGet<(Pet, Dictionary<string, string> _404)>,
        IHttpDelete<Result<ApiResponse>>,
        IHttpPut<(Pet body, Pet)>
    {
        IHttpPost<(FileInfo body, ApiResponse)> UploadImage { get; }
    }

    public partial interface IUser : IHttpPost<(User body, ApiResponse)>
    {
        IUserByUserName this[string userName] { get; }
    }

    public partial interface IUserByUserName :
        IHttpGet<Result<User>>,
        IHttpDelete<Result<ApiResponse>>,
        IHttpPut<(User body, User)>
    { }
}


#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
