#nullable enable
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;
using System.Xml.Serialization;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;

namespace SourceCrafter;

public static class HttpExtensions
{
    public static FormUrlEncodedContent CreateFormUrlEncoded<T>(this IEnumerable<KeyValuePair<string, string>> dictionary)
    {
        return new FormUrlEncodedContent(dictionary.Where(kv => kv.Value != null));
    }

    public static HttpContent ToByteArrayContent(this FileInfo file)
    {
        using MemoryStream memoryStream = new();
        file.OpenRead().CopyTo(memoryStream);
        return new ByteArrayContent(memoryStream.GetBuffer());
    }
    public static MultipartFormDataContent CreateMultipartFormData(this (HttpContent, string?, string?)[] contents)
    {
        MultipartFormDataContent multipartFormDataContent = new();
        foreach (var (content, name, fileName) in contents)
        {
            if (content != null)
                if (fileName != null)
                    multipartFormDataContent.Add(content, name ?? "file", fileName);
                else if (name != null)
                    multipartFormDataContent.Add(content, name);
                else
                    multipartFormDataContent.Add(content);
        }
        return multipartFormDataContent;
    }

    public static HttpRequestException RequestException(this HttpResponseMessage response)
    {
        var result = new HttpRequestException(response.ReasonPhrase);

        result.Data["StatusCode"] = response.StatusCode;

        if (response.Content is { } content)
            result.Data["$RequestContent$"] = content;

        return result;
    }

    public static async Task<T?> TryRetrieveContentAsync<T>(this HttpRequestException exc, JsonSerializerOptions? opts = null, CancellationToken cancellToken = default)
    {
        var content = exc.Data["$RequestContent$"] as HttpContent;

        return content switch
        {
            JsonContent or { Headers.ContentType.MediaType: "application/json" } =>
                await content.ReadFromJsonAsync<T?>(opts ?? new(), cancellToken),

            { Headers.ContentType.MediaType: [.., '/', 'x', 'm', 'l'] } =>
                await content.ToXmlAsync<T?>(cancellToken),

            { } when typeof(T) == typeof(string) && await Task.Run(content.ReadAsStringAsync, cancellToken) is T _out => _out,

            _ => default
        };
    }

    public static async Task<T> ToXmlAsync<T>(this HttpContent content, CancellationToken token = default) =>

        (T)new XmlSerializer(typeof(T)).Deserialize(await Task.Run(content.ReadAsStreamAsync, token));

#pragma warning disable CS8619 // La nulabilidad de los tipos de referencia del valor no coincide con el tipo de destino
    public static Task<T> ToJsonAsync<T>(this HttpContent content, JsonSerializerOptions? jsonOpts = null, CancellationToken token = default) =>

        content.ReadFromJsonAsync<T>(jsonOpts, token);
#pragma warning restore CS8619 // La nulabilidad de los tipos de referencia del valor no coincide con el tipo de destino

    public static JsonContent CreateJson<T>(this T tIn, JsonSerializerOptions? opts = null)
    {
        return JsonContent.Create(tIn, options: opts);
    }

    public static StreamContent CreateXml<T>(this T tIn)
    {
        using MemoryStream memoryStream = new();
        new XmlSerializer(typeof(T)).Serialize(memoryStream, tIn);
        return new StreamContent(memoryStream);
    }


    internal static readonly Dictionary<int, string> HttpStatuses = new() {
            { 100, "Continue" },
            { 101, "SwitchingProtocols" },
            { 102, "Processing" },
            { 103, "EarlyHints" },
            { 200, "OK" },
            { 201, "Created" },
            { 202, "Accepted" },
            { 203, "NonAuthoritativeInformation" },
            { 204, "NoContent" },
            { 205, "ResetContent" },
            { 206, "PartialContent" },
            { 207, "MultiStatus" },
            { 208, "AlreadyReported" },
            { 226, "IMUsed" },
            { 300, "Ambiguous" },
            { 301, "Moved" },
            { 302, "Found" },
            { 303, "RedirectMethod" },
            { 304, "NotModified" },
            { 305, "UseProxy" },
            { 306, "Unused" },
            { 307, "TemporaryRedirect" },
            { 308, "PermanentRedirect" },
            { 400, "BadRequest" },
            { 401, "Unauthorized" },
            { 402, "PaymentRequired" },
            { 403, "Forbidden" },
            { 404, "NotFound" },
            { 405, "MethodNotAllowed" },
            { 406, "NotAcceptable" },
            { 407, "ProxyAuthenticationRequired" },
            { 408, "RequestTimeout" },
            { 409, "Conflict" },
            { 410, "Gone" },
            { 411, "LengthRequired" },
            { 412, "PreconditionFailed" },
            { 413, "RequestEntityTooLarge" },
            { 414, "RequestUriTooLong" },
            { 415, "UnsupportedMediaType" },
            { 416, "RequestedRangeNotSatisfiable" },
            { 417, "ExpectationFailed" },
            { 421, "MisdirectedRequest" },
            { 422, "UnprocessableEntity" },
            { 423, "Locked" },
            { 424, "FailedDependency" },
            { 426, "UpgradeRequired" },
            { 428, "PreconditionRequired" },
            { 429, "TooManyRequests" },
            { 431, "RequestHeaderFieldsTooLarge" },
            { 451, "UnavailableForLegalReasons" },
            { 500, "InternalServerError" },
            { 501, "NotImplemented" },
            { 502, "BadGateway" },
            { 503, "ServiceUnavailable" },
            { 504, "GatewayTimeout" },
            { 505, "HttpVersionNotSupported" },
            { 506, "VariantAlsoNegotiates" },
            { 507, "InsufficientStorage" },
            { 508, "LoopDetected" },
            { 510, "NotExtended" },
            { 511, "NetworkAuthenticationRequired" }
        };
}
