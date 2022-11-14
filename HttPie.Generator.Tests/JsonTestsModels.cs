using HttPie.Attributes;
using HttPie.Enums;

namespace HttPie.Generator.Tests;


#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.



[HttpOptions("https://petstore.swagger.io/v2/", QueryCasing = Casing.CamelCase, PathCasing = Casing.CamelCase)]
//[RequestHeader("Accept","application/json")]
//[ContentHeader("Content-Type","application/json")]
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

public interface IOrder : 
    IPost<JsonContent<User>, JsonResponse<User>>
{
    IOrderActionsByOrderId this[int orderId] { get; }
}

public interface IOrderActionsByOrderId : 
    IGet<JsonResponse<Order>>, 
    IDelete<JsonResponse<ApiResponse>>
{
}

public interface IPetActionsByStatus
{
    Task<JsonResponse<List<Pet>>> GetAsync(Query<UserMini> status);
}

public class UserMini { 
    public int Id { get; set; }
    public string Name { get; set; }
}

//public class ApiKeyResolver : IJsonAsyncValueResolver
//{

//    Task<string> IAsyncValueResolver.ResolveAsync(string name)
//    {
//        throw new NotImplementedException();
//    }
//}

public interface IOrderActions : 
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

public interface IPetActionsByPetIdUploadImage : IPost<FileInfo, JsonResponse<ApiResponse>>
{
}

public interface IStoreInventory : IGet<JsonResponse<Dictionary<string, long>>>
{
}

public interface IUser : 
    IPost<JsonContent<User>, JsonResponse<ApiResponse>>
{
    IUserStringIndexer this[string userName] { get; }
}

public interface IPostUsersArray
{

}

public interface IUserStringIndexer :
    IDelete<JsonResponse<ApiResponse>>, 
    IGet<JsonResponse<User>>, 
    IPut<JsonContent<User>, JsonResponse<User>>
{

}

public enum PetStatus
{
    Available, Pending, Sold
}

public enum OrderStatus
{
    Placed, Approved, Delivered
}
public class Order
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public int Quantity { get; set; }
    public DateTime ShipDate { get; set; }
    public OrderStatus Status { get; set; }
    public bool Complete { get; set; }
}

public class ApiResponse
{
    public int Code { get; set; }
    public string Type { get; set; }
    public string Message { get; set; }
}

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; }
}

public class Pet
{
    public long Id { get; set; }
    public Category Category { get; set; }
    public string Name { get; set; }
    public string[] PhotoUrls { get; set; }
    public Tag[] Tags { get; set; }
    public PetStatus Status { get; set; }


}
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class User
{
    public long Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Phone { get; set; }
    public int UserStatus { get; set; }
    public OrderStatus Status { get; set; }

}
#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
