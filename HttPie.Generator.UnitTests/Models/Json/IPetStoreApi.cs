using HttPie.Attributes;
using HttPie.Enums;

namespace HttPie.Generator.UnitTests.Models.Json.Client
{

    [HttpOptions("https://petstore.swagger.io/v2/", QueryCasing = Casing.CamelCase, PathCasing = Casing.CamelCase, EnumQueryCasing = Casing.CamelCase, EnumSerializationCasing = Casing.CamelCase, PropertyCasing = Casing.CamelCase)]
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
        IPost<User, User>
    {
        IOrderActionsByOrderId this[int orderId] { get; }
    }

    public interface IOrderActionsByOrderId : 
        IGet<Order>, 
        IDelete<ApiResponse>
    {
    }
    public interface IPetActionsByStatus :
        // queryParamName: status
        IGet<PetStatus, List<Pet>>
    {
    }
    public interface IOrderActions: 
        IPost<Order, Order>,
        IPut<Order, Order>
    {                               
    }

    public interface IPetActionsByPetId : 
        IGet<Pet>, 
        IDelete<ApiResponse>, 
        IPost<Pet, Pet>
    {
        IPetActionsByPetIdUploadImage UploadImage { get; }
    }
    public interface IPetActionsByPetIdUploadImage :
        // contentParamName: file
        IPost<FileInfo, ApiResponse>
    {
    }

    public interface IStoreInventory: IGet<Dictionary<string, long>>
    {
    }

    public interface IUser:IPost<User, ApiResponse>
    {
        IUserActionsByUserName this[string userName] { get; }
    }
    
    public interface IUserActionsByUserName :
        IGet<User>,
        IDelete<ApiResponse>,
        IPut<User, User>
    {

    }
}


#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
