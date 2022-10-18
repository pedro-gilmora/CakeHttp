using DevEverywhere.CakeHttp.Attributes;
using DevEverywhere.CakeHttp.Enums;
using DevEverywhere.CakeHttp.Inferfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DevEverywhere.CakeHttp;

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.


[CakeHttpOptions("https://petstore.swagger.io/v2/", true, EnumSerialization.CamelCaseString)]
[RequestHeader("Accept","application/json")]
[ContentHeader("Content-Type","application/json")]
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
    IPetActionsByPetId this[long petId] { get; set; }
    IPetActionsByStatus FindByStatus { get; set; }
    IOrderActions Order { get; }
}

public interface IOrder: IPost<User, User>
{
    IOrderActionsByOrderId this[int orderId] { get; }
}

public interface IOrderActionsByOrderId : IGetRetrieve<Order>, IDeleteRetrieve<ApiResponse>
{
}

public interface IPetActionsByStatus
{    
    Task<List<Pet>> GetAsync([AsQueryValue] PetStatus status);
}

public class ApiKeyResolver : IAsyncValueResolver
{

    Task<string> IAsyncValueResolver.ResolveAsync(string name)
    {
        throw new NotImplementedException();
    }
}

public interface IOrderActions: IPost<Order, Order>, IPut<Order, Order> {
}

public interface IPetActionsByPetId : IGetRetrieve<Pet>, IDeleteRetrieve<ApiResponse>, IPost<Pet, Pet>
{
    IPetActionsByPetIdUploadImage UploadImage { get; }
}

public interface IPetActionsByPetIdUploadImage
{
    Task<ApiResponse> PostAsync([FormData] FileInfo file);

}

public interface IStoreInventory: IGetRetrieve<Dictionary<string, long>>
{
}

public interface IUser : IPost<User, ApiResponse>
{
    IUserStringIndexer this[string userName] { get; }
}

public interface IPostUsersArray { 

}

public interface IUserStringIndexer: IDeleteRetrieve<ApiResponse>, IGetRetrieve<User>, IPut<User, User>
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
    public int Id { get; set; }
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
    