#nullable enable
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SourceCrafter.HttpServiceClient.Operations;

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
public readonly struct Query<T> : IHttpResource
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