using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Operations;
using System.Text.Json.Serialization;
using System.Net;

namespace FacilCuba.Infrastructure.QvaPay;

[HttpOptions("https://qvapay.com/api", PropertyCasing = Casing.LowerCase, PathCasing = Casing.CamelCase)]
public interface IQvaPayApi
{
    IAuth Auth { get; }
    IUser User { get; }
    ITransactions Transactions { get; }
    void UpdateAuthenticationStatus(string? authToken);
}

public interface IAuth
{
    ILogin Login { get; }
    ILogout Logout { get; }
}

public interface ILogin : IPost<Credentials, LoginResponse>
{
}

public interface ILogout : IGet</*responseType: Xml*/ object>
{
}

public interface IUser : IGet<MeAsUser>
{
}

public interface ITransactions : IGet<List<Transaction>>
{
}

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
public partial class QvaPayClient
{
    
    public void UpdateAuthenticationStatus(string? authToken)
    {
        if (authToken != null)
            _agent._httpClient.DefaultRequestHeaders.Add("Authorization", authToken);
        else
            _agent._httpClient.DefaultRequestHeaders.Remove("Authorization");
    }
}

public class Credentials
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class ApiResponse  
{
    public HttpStatusCode? Code { get; set; }
    public string Message { get; set; }
}

public class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }
    public User Me { get; set; }
}

[JsonSerializable(typeof(User))]
public class User : UserBase {
    public string Logo { get; set; }
    public int? Kyc { get; set; }
    public string Email { get; set; }
}

public class UserBase
{
    /* uuid */
    public string? Uuid { get; set; }
    public string Name { get; set; }
    [JsonPropertyName("lastname")]
    public string LastName { get; set; }
    [JsonPropertyName("username")]
    public string UserName { get; set; }
    public string Bio { get; set; }
}
public class App
{
    /* logo */
    public string Logo { get; set; }
    /* url */
    public string Url { get; set; }
    /* name */
    public string Name { get; set; }
    /* app_photo_url */
    public string AppPhotoUrl { get; set; }
}

public class AppOwner
{
    /* logo */
    public string Logo { get; set; }
    /* url */
    public string Url { get; set; }
    /* name */
    public string Name { get; set; }
    /* app_photo_url */
    public string AppPhotoUrl { get; set; }
}

public class Transaction
{
    /* uuid */
    public string Uuid { get; set; }
    /* app_id */
    public int AppId { get; set; }
    /* amount */
    public string Amount { get; set; }
    /* description */
    public string Description { get; set; }
    /* remote_id */
    public string RemoteId { get; set; }
    /* status */
    public string Status { get; set; }
    /* created_at */
    public DateTime CreatedAt { get; set; }
    /* updated_at */
    public DateTime UpdatedAt { get; set; }
    /* logo */
    public string Logo { get; set; }
    /* app */
    public App App { get; set; }
    /* paid_by */
    public UserInfo PaidBy { get; set; }
    /* app_owner */
    public AppOwner AppOwner { get; set; }
    /* owner */
    public UserInfo Owner { get; set; }
    /* wallet */
    public WalletInfo Wallet { get; set; }
    /* servicebuy */
    public object Servicebuy { get; set; }
}

public class UserInfo : User
{
    /* profile_photo_path */
    public string ProfilePhotoPath { get; set; }
    /* complete_name */
    public string CompleteName { get; set; }
    /* name_verified */
    public string NameVerified { get; set; }
    /* profile_photo_url */
    public string ProfilePhotoUrl { get; set; }
    /* average_rating */
    public int AverageRating { get; set; }
}

public class MeAsUser : User
{
    /* profile_photo_path */
    public string ProfilePhotoPath { get; set; }
    /* balance */
    public string Balance { get; set; }
    /* total_in */
    public string TotalIn { get; set; }
    /* total_out */
    public string TotalOut { get; set; }
    /* latestTransactions */
    [JsonPropertyName("latestTransactions")]
    public List<Transaction> LatestTransactions { get; set; }
    /* complete_name */
    public string CompleteName { get; set; }
    /* name_verified */
    public string NameVerified { get; set; }
    /* profile_photo_url */
    public string ProfilePhotoUrl { get; set; }
    /* average_rating */
    public int AverageRating { get; set; }
}

public class WalletInfo
{
    /* transaction_id */
    public int TransactionId { get; set; }
    /* invoice_id */
    public string InvoiceId { get; set; }
    /* wallet_type */
    public string WalletType { get; set; }
    /* wallet */
    public string Wallet { get; set; }
    /* value */
    public string Value { get; set; }
    /* received */
    public string Received { get; set; }
    /* txid */
    public string Txid { get; set; }
    /* status */
    public string Status { get; set; }
    /* created_at */
    public DateTime CreatedAt { get; set; }
    /* updated_at */
    public DateTime UpdatedAt { get; set; }
}

#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.