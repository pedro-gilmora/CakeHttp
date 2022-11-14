using HttPie.Attributes;
using HttPie.Enums;

namespace HttPie.Generator.UnitTests.Models.Json.Client
{

    [HttpOptions("https://petstore.swagger.io/v2/", QueryCasing = Casing.CamelCase, PathCasing = Casing.CamelCase)]
    public interface IPetStoreApi
    {
        IStore Store { get; }
        IPet Pet { get; }
        IUser User { get; }
    }

    public interface IStore
    {
        IOrder Order { get; }
        IStoreInventory Inventory { get; }
    }

    public interface IPet
    {
        IPetActionsByPetId this[long petId] { get; }
        IPetActionsByStatus FindByStatus { get; }
        IOrderActions Order { get; }
    }

    public interface IOrder:
        IPost<JsonContent<User>, JsonResponse<User>>
    {
        IOrderActionsByOrderId this[int orderId] { get; }
    }

    public interface IOrderActionsByOrderId : 
        IGet<JsonResponse<Order>>, 
        IDelete<JsonResponse<ApiResponse>>
    {
    }
    public interface IPetActionsByStatus:
        IGet</* status */PetStatus, JsonResponse<List<Pet>>>
    {
    }
    public interface IOrderActions: 
        IPost<JsonContent<Order>, JsonResponse<Order>>,
        IPut<JsonContent<Order>, JsonResponse<Order>>
    {                               
    }

    public interface IPetActionsByPetId : 
        IGet<JsonResponse<Pet>>, 
        IDelete<JsonResponse<ApiResponse>>, 
        IPost<JsonContent<Pet>, JsonResponse<Pet>>
    {
        IPetActionsByPetIdUploadImage UploadImage { get; }
    }

    public interface IPetActionsByPetIdUploadImage:
        IPost<MultipartFormDataContent<FileInfo>, JsonResponse<ApiResponse>>
    {
    }

    public interface IStoreInventory: IGet<JsonResponse<Dictionary<string, long>>>
    {
    }

    public interface IUser:IPost<JsonContent<User>, JsonResponse<ApiResponse>>
    {
        IUserStringIndexer this[string userName] { get; }
    }
    
    public interface IUserStringIndexer :
        IGet<JsonResponse<User>>,
        IDelete<JsonResponse<ApiResponse>>,
        IPut<JsonContent<User>, JsonResponse<User>>
    {

    }
}


#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
