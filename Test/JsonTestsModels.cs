using CakeHttp.Attributes;
using CakeHttp.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DevEverywhere.CakeHttp;

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.

public class Order
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public int Quantity { get; set; }
    public DateTime ShipDate { get; set; }
    public OrderStatus Status { get; set; }
    public bool Complete { get; set; }
}

public interface IOrder
{
    Task<Order> PostAsync(Order newOrder);
    IOrderActionsByOrderId this[int orderId] { get; }
}

public interface IOrderActionsByOrderId 
{
    Task<Order> GetAsync();
    Task<ApiResponse> DeleteAsync();
}

public interface IPet
{
    IPetActionsByPetId this[long petId] { get; set; }
    IPetActionsByStatus FindByStatus { get; set; }
    IOrderActions Order { get; }
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

public interface IOrderActions {
    Task<User> PostAsync(User user);
    Task<User> PutAsync(User user);
}

public interface IPetActionsByPetId 
{
    Task<Pet> GetAsync();
    Task<Pet> PostAsync(Pet pet);
    Task<ApiResponse> DeleteAsync();
    IPetActionsByPetIdUploadImage UploadImage { get; }
}

public interface IPetActionsByPetIdUploadImage
{
    Task<ApiResponse> PostAsync([FormData] FileInfo file);

}

public interface IStore
{
    IOrder Order { get; }
    IStoreInventory Inventory { get; }
}

public interface IStoreInventory
{
    Task<Dictionary<string, long>> GetAsync();
}

[CakeHttpOptions("https://petstore.swagger.io/v2/", true, EnumSerialization.CamelCaseString)]
public interface IPetStoreApi
{
    IStore Store { get; }
    IPet Pet { get; }
    IUser User { get; }
}

public interface IUser
{
    IPostUsersArray UsersWithArrayInputTask { get; }
    IUserStringIndexer this[string userName] { get; }
    Task<ApiResponse> PostAsync(User pet);
}

public interface IPostUsersArray { 

}

public interface IUserStringIndexer
{
    Task<ApiResponse> IDeleteAsync();
    Task<User> GetAsync();
    Task<User> PutAsync(User user);

}

public enum PetStatus
{
    Available, Pending, Sold
}

public enum OrderStatus
{
    Placed, Approved, Delivered
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
    