# 🍰 **HttPie**: Next generation for Rest API Generation. 
>Sucessor for HttPie.Generator

## You just can build/update/generate* your API calls

### Under these premises
- Properties and indexers will generate path segments, encoded to url components
- Methods are HTTP verbs invokers for GET, DELETE, POST, PUT and PATCH requests (e.g.: &nbsp;`GetAsync(...)`), with the following parameters _(all of them are optional):_
  - **`TResponse?`**: Return type for the async method
  - **`TContent? content`**: For POST, PUT and PATCH requests content.
  - **`TQuery? query`**: Generic parameter to be serialized into URL query components
  - **`Action<HttpClient, HttpRequestMessage>? requestHandler`**: A handler for requests before send it to the server
    > Usage: Set some information to headers 
  - **`Action<HttpClient, HttpResponseMessage>? requestHandler`**: A handler for after get the response and before serialize the possible content coming from the server
    > Usage: Gather information from response headers 
  - **`CancellationToken? cancelToken`**: A cancellation token for this task

### Operation metadata
<table>
<tr><td>Key</td><td>Description</td></tr>
<tr>
<td><b><code>queryParamName</code></b></td>
<td>
Sets the query parameter name. Defaults to <code>query</code>. E.g.: <code>?status=pending</code> whereas <b>status</b> is the <code>queryParamName</code> for a value type parameter
</td>
</tr>
<tr>
<td><b><code>contentParamName</code></b></td>
<td>
Sets the content parameter name. Defaults to <code>content</code>. For <code>MultipartFormData</code> or <code>FormUrlEncoded</code> content types creates a named item for value type parameters
</td>
</tr>
<tr>
<td><b><code>contentFormatType</code></b></td>
<td>
Determines the content format type. <code>MultipartFormData</code> and
<code>FormUrlEncoded</code>. For <code>Xml</code>, a <code>StreamContent</code> is created to transport the serialized data. <code>Json</code> uses the same <code>System.Net.Http.Json.JsonContent</code> are available
</td>
</tr>
<tr>
<td><b><code>responseFormatType</code></b></td>
<td>
Determines the response format type. Just <code>Xml</code> and <code>Json</code>
</td>
</tr>
</table>

>Rest of `{key}: {value}` pairs parsed on comments will be trated as request headers

---


### The client

```csharp
// Generates a client class with the following name convention
// Takes Name from I{Name}Api and adds 'Client' to the end
var _cakeClient = new PetStoreClient();
```

### The usage
```csharp
//will produce a call to https://petstore.swagger.io/v2/store/inventory
var inventory = await _cakeClient.Store.Inventory.GetAsync();
```

### The structure (*generated)
```csharp
[HttpOptions("https://petstore.swagger.io/v2/", 
// Url Encode properties are formatted specific casing (CamelCase, PascalCase, Upper and Lower snake casing)
QueryCasing = Casing.CamelCase, 
// Path segments casing format
PathCasing = Casing.CamelCase, 
// Enum values casing format on query and path. None for its underlying value
EnumQueryCasing = Casing.CamelCase, 
// Enum values casing format on content serialization
EnumSerializationCasing = Casing.CamelCase, 
// Properties casing format on content serialization
PropertyCasing = Casing.CamelCase)]
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
```

### The models
```csharp
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
```

---
### Generated content

The following interface
```csharp
public interface IPetActionsByPetIdUploadImage :
    // contentParamName: file
    IPost<FileInfo, ApiResponse>
{
}
```

would generate the following service class (based on the previous definition example):
```csharp
//<auto generated>
using static HttPie.Generator.HttPieHelpers;
using System.Net.Http.Json;
using System.IO;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Domain.Service.Models;
using HttPie.Generator;

namespace Domain.Service
{
    public class PetActionsByPetIdUploadImageService : IPetActionsByPetIdUploadImage 
    {
        private readonly PetStoreAgent _agent;
        private readonly string _path;
        
        internal PetActionsByPetIdUploadImageService(PetStoreAgent agent, string path)
        {
            _agent = agent;
            _path = path;            
        }

        public async Task<ApiResponse> PostAsync(FileInfo file, Func<HttpRequestMessage, Task> beforeSend = default, Func<HttpResponseMessage, Task> afterSend = default, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_path, UriKind.Relative)) {
                Content = HttPieHelpers.CreateMultipartFormData(ArrayFrom((file.ToByteArrayContent(), "file", file.Name)))
            };
            var response = await _agent._httpClient.SendAsync(request, cancellationToken);

            return response switch 
            {
                { IsSuccessStatusCode: true, Content: {} responseContent } => 
                    await responseContent.ReadFromJsonAsync<ApiResponse>(_agent._jsonOptions, cancellationToken),

                { IsSuccessStatusCode: false } => 
                    throw new HttpRequestException(response.ReasonPhrase),

                _ => default(ApiResponse)
            };
        }
    }
}
```


___

## TODO:
- Manage body and response content headers properly