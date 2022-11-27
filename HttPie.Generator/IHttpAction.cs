#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttPie.Generator;

public interface IHttpResource
{

}

[AttributeUsage(AttributeTargets.All)]
public sealed class ResourceNameAttribute : Attribute
{
    public ResourceNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
public enum HttpVerb
{

    //Retrieves the information or entity that is identified by the URI of the request.
    Get = 1,

    //Posts a new entity as an addition to a URI.
    Post = 2,

    //Replaces an entity that is identified by a URI.
    Put = 4,

    //Requests that a specified URI be deleted.
    Delete = 8,

    //Retrieves the message headers for the information or entity that is identified by the URI of the request.
    Head = 16,

    //Requests that a set of changes described in the request entity be applied to the resource identified by the Request- URI.
    Patch = 32,

    //Represents a request for information about the communication options available on the request/response chain identified by the Request-URI.
    Options = 64
}
public readonly struct Query<T>: IHttpResource
{
    internal T Value { get; }
    public Query(T value) => Value = value;

    public static implicit operator T(Query<T> query) => query.Value;
    public static implicit operator Query<T>(T value) => new(value);
}

public interface IGet<TResponse>
{
    Task<TResponse> GetAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IGet<TQuery, TResponse>
{
    Task<TResponse> GetAsync(TQuery query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IDelete<TResponse>
{
    Task<TResponse> DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IDelete<TQuery, TResponse>
{
    Task<TResponse> DeleteAsync(TQuery query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPost<TResponse>
{
    Task PostAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPost<TContent, TResponse>
{
    Task<TResponse> PostAsync(TContent content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPost<TQuery, TContent, TResponse>
{
    Task<TResponse> PostAsync(TQuery query, TContent content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPut<TResponse>
{
    Task PutAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPut<TContent, TResponse>
{
    Task<TResponse> PutAsync(TContent content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPut<TQuery, TContent, TResponse>
{
    Task<TResponse> PutAsync(TQuery query, TContent content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPatch<TResponse>
{
    Task PatchAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPatch<TContent, TResponse>
{
    Task<TResponse> PatchAsync(TContent content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

public interface IPatch<TQuery, TContent, TResponse>
{
    Task<TResponse> PatchAsync(TQuery query, TContent content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default, CancellationToken cancellationToken = default);
}

//#region JSON
//#region Empty
//public interface IJsonDelete : IHttpAction
//{
//    Task DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPost : IHttpAction
//{
//    Task PostAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPut : IHttpAction
//{
//    Task PutAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion

//#region TIn
//public interface IJsonDelete<TQuery> : IHttpAction
//{
//    Task DeleteAsync(TQuery query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPost<TIn> : IHttpAction
//{
//    Task PostAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPut<TIn> : IHttpAction
//{
//    Task PutAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion

//#region TInOut
//public interface IJsonGet<TQuery, TOut> : IHttpAction
//{
//    Task<TOut> GetAsync(TQuery query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonDelete<TQuery, TOut> : IHttpAction
//{
//    Task<TOut> DeleteAsync(TQuery query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPost<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PostAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPut<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PutAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPostQuery<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PostAsync<TQuery>(TQuery query, TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPutQuery<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PutAsync<TQuery>(TQuery query, TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion

//#region Out
//public interface IJsonGetRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> GetAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonDeleteRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPostRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> PostAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPutRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> PutAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion
//#endregion


//#region XML
//#region Empty
//public interface IXmlDelete : IHttpAction
//{
//    Task DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IXmlPost : IHttpAction
//{
//    Task PostAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPut : IHttpAction
//{
//    Task PutAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion

//#region TIn
//public interface IXmlDelete<TIn> : IHttpAction
//{
//    Task DeleteAsync(TIn query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPost<TIn> : IHttpAction
//{
//    Task PostAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPut<TIn> : IHttpAction
//{
//    Task PutAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion

//#region TInOut
//public interface IXmlGet<TIn, TOut> : IHttpAction
//{
//    Task<TOut> GetAsync(TIn query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IXmlDelete<TIn, TOut> : IHttpAction
//{
//    Task<TOut> DeleteAsync(TIn query, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPost<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PostAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPut<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PutAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPostQuery<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PostAsync<TQuery>(TQuery query, TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPutQuery<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PutAsync<TQuery>(TQuery query, TIn content, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion

//#region Out
//public interface IXmlGetRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> GetAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IXmlDeleteRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPostRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> PostAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IXmlPutRetrieve<TOut> : IHttpAction
//{
//    Task<TOut> PutAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion
//#endregion


//#region Empty
//public interface IJsonDelete : IHttpAction
//{
//    Task DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonGet : IHttpAction
//{
//    Task GetAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPost : IHttpAction
//{
//    Task PostAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPut : IHttpAction
//{
//    Task PutAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//#endregion

//#region Send
//public interface IJsonSendDelete<T> : IHttpAction
//{
//    Task SendDeleteAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonSendPost<T> : IHttpAction
//{
//    Task SendPostAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonSendPut<T> : IHttpAction
//{
//    Task SendPutAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//#endregion

//#region Update
//public interface IJsonUpdatePost<TOut> : IHttpAction
//{
//    Task<TOut> UpdatePostAsync(TOut _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonUpdatePut<T> : IHttpAction
//{
//    Task<T> UpdatePutAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonQueryUpdatePost<TQuery, TOut> : IHttpAction
//{
//    Task<TOut> QueryUpdatePostAsync(TQuery query, TOut _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonQueryUpdatePut<TQuery, TOut> : IHttpAction
//{
//    Task<TOut> QueryUpdatePutAsync(TQuery query, TOut _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//#endregion

//#region FilterOrSend 
//public interface IJsonGetQueryResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryGetAndReceiveAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonDeleteQueryResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryDeleteAndReceiveAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPostContentResponse<TOut> : IHttpAction
//{
//    Task<TOut> PostSendAndReceiveAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPostQueryContentResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryPostAndReceiveAsync<TQuery, TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPutQueryResponse<TOut> : IHttpAction
//{
//    Task<TOut> PutContentAndReadAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPutQueryContentResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryPutAndReceiveAsync<TQuery, TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPostTypedContentResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PostTypedSendAndReceiveAsync(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPostTypedQueryContentResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> QueryPostTypedAndReceiveAsync<TQuery>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonPutTypedQueryResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PutTypedContentAndReadAsync(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonPutTypedQueryContentResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> QueryTypedPutAndReceiveAsync<TQuery>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//#endregion

//#region From
//public interface IJsonFromDelete<T> : IHttpAction
//{
//    Task<T> FromDeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonFromGet<T> : IHttpAction
//{
//    Task<T> FromGetAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonFromPost<T> : IHttpAction
//{
//    Task<T> FromPostAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonFromPut<T> : IHttpAction
//{
//    Task<T> FromPutAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}
//public interface IJsonQueryPostAndReveive<T> : IHttpAction
//{
//    Task<T> QueryPostAndReceiveAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//public interface IJsonQueryPutAndReveive<T> : IHttpAction
//{
//    Task<T> QueryPutAndReceiveAsync(Func<HttpRequestMessage, Task>? beforeSend = default, Func<HttpResponseMessage, Task>? afterSend = default);
//}

//#endregion