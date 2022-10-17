//using DevEverywhere.CakeHttp.Inferfaces;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using CakeHttp.Attributes;
using CakeHttp.Converters;
using CakeHttp.Enums;

namespace DevEverywhere.CakeHttp;

public class CakeHttp : DispatchProxy
{

    private static readonly MethodInfo creator = typeof(DispatchProxy).GetMethod(nameof(Create))!;

    const string
        HEADER_PROP = "Headers",
        GET_METHOD = "GET",
        DELETE_METHOD = "DELETE",
        POST_METHOD = "POST",
        PUT_METHOD = "PUT",
        PATCH_METHOD = "PATCH";

    private static readonly Type
        taskType = typeof(Task),
        objType = typeof(object),
        cancelTokenType = typeof(CancellationToken),
        thisType = typeof(CakeHttp),
        voidType = typeof(void);

    private static readonly MethodInfo
        invokeRequestMethod = thisType.GetMethod(nameof(InvokeRequest), BindingFlags.Static | BindingFlags.NonPublic)!,
        getResponseMethod = thisType.GetMethod(nameof(GetResponse), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly Func<HttpRequestMessage, Task> defaultRequestHandler = async r => { await Task.CompletedTask; };
    private static readonly Func<HttpResponseMessage, Task> defaultResponseHandler = async r => { await Task.CompletedTask; };

    public static T CreateClient<T>()
    {
        Type apiType = typeof(T);
        if (apiType.GetCustomAttribute<CakeHttpOptionsAttribute>() is { BaseUrl: { } url } initOptions)
        {
            dynamic t = Create<T, CakeHttp>()!;
            t._httpClient = new HttpClient() { BaseAddress = new Uri(url) };
            t._initOptions = initOptions;
            return (T)t;
        }
        throw new TypeInitializationException(
            apiType.FullName,
            new Exception("You must provide a URL value through url parameter or the CakeHttpInitOptionsAttribute")
        );
    }

    public static T CreateClient<T>(string url, bool camelCasePathAndQuery = false, EnumSerialization enumSerialization = EnumSerialization.CamelCaseString)
    {
        dynamic t = Create<T, CakeHttp>()!;
        t._httpClient = new HttpClient() { BaseAddress = new Uri(url) };
        t._initOptions = new CakeHttpOptionsAttribute(url, camelCasePathAndQuery, enumSerialization);
        return (T)t;
    }

    public static T CreateClient<T>(HttpClient client, bool camelCasePathAndQuery = false, EnumSerialization enumSerialization = EnumSerialization.CamelCaseString)
    {
        dynamic t = Create<T, CakeHttp>()!;
        t._httpClient = client;
        t._initOptions = new CakeHttpOptionsAttribute(client.BaseAddress!.ToString(), camelCasePathAndQuery, enumSerialization);
        return (T)t;
    }

    internal static object CreateClient<TApi>(HttpClient httpClient, CakeHttpOptionsAttribute opts) where TApi : class
    {
        dynamic t = Create<TApi, CakeHttp>()!;
        t._httpClient = httpClient;
        t._initOptions = opts;
        return (TApi)t;
    }

    private ICakeHttpInitOptions _initOptions = null!;
    private HttpClient _httpClient = null!;
    private List<object?> _pathSegments = new();


    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is { Name: { } name, IsGenericMethod: { } isGeneric, ReturnType: { } methodReturnType })
        {

            if (name.StartsWith("get_") || name.StartsWith("set_"))
            {
                dynamic ret = creator.MakeGenericMethod(methodReturnType, typeof(CakeHttp)).Invoke(null, null)!;

                List<object?> list = new(_pathSegments);

                if (name is "get_Item" && args is { Length: > 0 } indexes)
                    list.AddRange(indexes);
                else
                    list.Add(name[4..]);

                ret._pathSegments = list;
                ret._httpClient = _httpClient;
                ret._initOptions = _initOptions;

                return ret;
            }

            ParameterInfo[] parameterInfos = targetMethod.GetParameters();

            var method = targetMethod.Name.ToUpper() switch
            {
                { } deleteParse when deleteParse.Contains(DELETE_METHOD) => DELETE_METHOD,
                { } postParse when postParse.Contains(POST_METHOD) => POST_METHOD,
                { } putParse when putParse.Contains(PUT_METHOD) => PUT_METHOD,
                _ => GET_METHOD,
            };

            string url = BuildPath();

            Type[] argumentTypes = isGeneric ? methodReturnType.GetGenericArguments() : Array.Empty<Type>();

            GetRequestInfo(
                args,
                methodReturnType,
                parameterInfos,
                out var returnType,
                out var content,
                out var contentType,
                out var query,
                out var queryType,
                out var requestHandlers,
                out var responseHandlers,
                out var queryFromParameters,
                out var token,
                out var formData
            );

            var jsonOptions = _initOptions.JsonOptions;

            using var request = CreateRequest(method, url, query, queryType, queryFromParameters, jsonOptions);

            var dynamicMethod = returnType == voidType ? invokeRequestMethod : getResponseMethod;

            return CakeHttp.ReturnAs(
                dynamicMethod,
                returnType,
                _httpClient,
                request,
                method,
                content,
                contentType,
                formData,
                targetMethod.GetCustomAttributes<HeaderBaseAttribute>().ToArray(),
                requestHandlers,
                responseHandlers,
                jsonOptions,
                token
            );

        }
        return this;
    }

    private static (string?, Type, object?) Transform(ParameterInfo t, object? value)
        => (t.Name, t.ParameterType, value);

    private static void GetRequestInfo(object?[]? args, Type methodReturnType, ParameterInfo[] parameterInfos, out Type returnType, out object? content, out Type? contentClrType, out object? query, out Type? queryType, out Func<HttpRequestMessage, Task>? requestHandlers, out Func<HttpResponseMessage, Task>? responseHandlers, out Dictionary<string, object?> queryFromParameters, out CancellationToken token, out List<(string, object?, bool)> formData)
    {
        returnType = methodReturnType.IsGenericType ? methodReturnType.GetGenericArguments()[0] : typeof(void);
        content = default;
        contentClrType = default;
        query = default;
        queryType = default;
        requestHandlers = defaultRequestHandler;
        responseHandlers = defaultResponseHandler;
        token = default;
        queryFromParameters = new();
        formData = new();

        string? contentType = null;

        if (args is { })
        {
            int i = -1, paramsLen = args.Length;

            while (++i < paramsLen)
            {
                ParameterInfo pInfo = parameterInfos[i];
                (string? paramName, Type type, object? prm) = Transform(pInfo, args[i++]);

                if (paramName is null) continue;

                else if (prm is { } && contentType is null or "multipart/form-data" && pInfo.GetCustomAttribute<FormDataAttribute>() is { Key: var key })
                {
                    contentType ??= "multipart/form-data";
                    formData.Add((key ?? paramName, prm, prm is FileInfo));
                }

                else if (paramName != "query" || pInfo.GetCustomAttribute<AsQueryValueAttribute>() is not null)
                    queryFromParameters.Add(paramName, prm);

                else if (paramName == "content")
                {
                    content = prm;
                    contentClrType = type;
                }

                else if (paramName == "query")
                {
                    query = prm!;
                    queryType = type;
                }

                else if (paramsLen == i && type == typeof(CancellationToken))
                    token = (CancellationToken)prm!;

                else if (type == typeof(Func<HttpResponseMessage, Task>))
                    responseHandlers += (Func<HttpResponseMessage, Task>)prm!;

                else if (type == typeof(Func<HttpRequestMessage, Task>))
                    requestHandlers += (Func<HttpRequestMessage, Task>)prm!;

            }
        }

#pragma warning disable CS8601 // Posible asignación de referencia nula
        responseHandlers -= defaultResponseHandler;
        requestHandlers -= defaultRequestHandler;
#pragma warning restore CS8601 // Posible asignación de referencia nula
    }

    private static bool HasParameter<T>(object?[]? args, out T param, params int[] indexes)
    {
        if (args is { })
            foreach (var idx in indexes)
                if (args.Length > idx && args[^idx] is T _param)
                    return (param = _param, true).Item2;

        return (param = default!, false).Item2;
    }

    private static dynamic ReturnAs(MethodInfo method, Type returnType, params object?[] parameters)
    {
        if (returnType == voidType)
            return invokeRequestMethod
                .Invoke(null, parameters)!;

        return getResponseMethod
            .MakeGenericMethod(new[] { returnType })
            .Invoke(null, new[] { returnType }.Concat(parameters).ToArray())!;
    }

    private string BuildPath()
    {
        return _pathSegments.Aggregate("", (list, item) => list + AddSegmentSeparator(list) + item switch
        {
            Enum en => _initOptions.PathAndQueryFormatter(KeyToUrlEncode(en.ToString())),
            string str => _initOptions.PathAndQueryFormatter(KeyToUrlEncode(str)),
            { } => _initOptions.PathAndQueryFormatter(KeyToUrlEncode(JsonSerializer.Serialize(item, _initOptions.JsonOptions))),
            _ => "null"
        });
    }

    private static string AddSegmentSeparator(string list)
    {
        return string.IsNullOrEmpty(list) ? "" : "/";
    }

    private static TItem[] Args<TItem>(params TItem[] list) => list;

    private static async Task InvokeRequest(HttpClient httpClient, HttpRequestMessage request, string method, object? content, Type? contentType, List<(string, object?, bool)> formData, HeaderBaseAttribute[] headers, Func<HttpRequestMessage, Task>? requestHandler, Func<HttpResponseMessage, Task>? responseHandler, JsonSerializerOptions jsonOptions, CancellationToken token)
    {
        await SetContentAndHeaders(request, method, content, contentType, formData, headers, jsonOptions);

        if (requestHandler is { })
            await requestHandler(request);

        var response = await httpClient.SendAsync(request, token);

        if (responseHandler is { })
            await responseHandler(response);
    }

    private static async Task<T> GetResponse<T>(Type returnType, HttpClient httpClient, HttpRequestMessage request, string method, object? content, Type? contentType, List<(string, object?, bool)> formData, HeaderBaseAttribute[] requestContents, Func<HttpRequestMessage, Task>? requestHandler, Func<HttpResponseMessage, Task>? responseHandler, JsonSerializerOptions jsonOptions, CancellationToken token)
    {
        await SetContentAndHeaders(request, method, content, contentType, formData, requestContents, jsonOptions);

        if (requestHandler is { })
            await requestHandler(request);

        using HttpResponseMessage response = await httpClient.SendAsync(request, token);

        if (responseHandler is { })
            await responseHandler(response);

        if (response is { Content: { Headers: { } _headers } responseContent })
        {
            //JSON
            if (response.Content.Headers.ContentType?.MediaType is MediaTypeNames.Application.Json)
                return (await responseContent.ReadFromJsonAsync<T>(jsonOptions, token))!;

            //XML
            else if (response.Content.Headers.ContentType?.MediaType is MediaTypeNames.Application.Xml)
                // Type
                if (returnType.IsSerializable)
                    return (T)(new XmlSerializer(returnType).Deserialize(await GetXmlReaderAsync(responseContent, true, token)))!;

                //XDocument
                else if (returnType == typeof(XDocument))
                    return (T)((object)XDocument.Parse(await responseContent.ReadAsStringAsync(token)))!;
                // XElement
                else
                    return (T)(object)XElement.Parse(await responseContent.ReadAsStringAsync(token));

            // String
            else if (returnType == typeof(string))
                return (T)(object)await responseContent.ReadAsStringAsync(token);

            // Stream
            else if (returnType.IsAssignableTo(typeof(Stream)))
                return (T)(object)await responseContent.ReadAsStreamAsync(token);

            // Byte[]
            else if (returnType == typeof(byte[]))
                return (T)(object)await responseContent.ReadAsByteArrayAsync(token);

            // Not serializable
            throw new FormatException($"Unable to deserialize {responseContent.GetType().Name} into {returnType.Name}");
        }

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);

        return default!;
    }

    private static async Task<XmlReader> GetXmlReaderAsync(HttpContent content, bool mandatoryDocument, CancellationToken token)
    {
        string xmlText = await content.ReadAsStringAsync(token);
        if (mandatoryDocument && !xmlText.StartsWith("<?xml"))
            xmlText = xmlText.Insert(0, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        return XDocument.Parse(xmlText).CreateReader();
    }

    private HttpRequestMessage CreateRequest(string method, string url, object? query, Type? queryType, Dictionary<string, object?> queryFromParameters, JsonSerializerOptions jsonOptions)
    {
        var httpMethod = method.ToUpperInvariant() switch
        {
            POST_METHOD => HttpMethod.Post,
            PUT_METHOD => HttpMethod.Put,
            DELETE_METHOD => HttpMethod.Delete,
            _ => HttpMethod.Get
        };

        url = method is GET_METHOD or DELETE_METHOD && (query is { } && queryType is { } || queryFromParameters.Count > 0)
            ? AddQuery(url, query, queryType, queryFromParameters, jsonOptions)
            : url;

        HttpRequestMessage request = new(httpMethod, url);

        return request;
    }


    private static async Task SetContentAndHeaders(HttpRequestMessage message, string method, object? content, Type? contentType, List<(string, object?, bool)> formData, HeaderBaseAttribute[] headerResolvers, JsonSerializerOptions jsonOptions)
    {
        NameValueCollection contentHeaders = new();

        foreach (var resolver in headerResolvers)
        {
            var name = resolver.Name.ToLower();
            var value = resolver switch
            {
                HeaderAsyncResolverAttribute<IAsyncValueResolver> async => await async.Resolver.ResolveAsync(name),
                HeaderResolverAttribute<IValueResolver> normal => normal.Resolver.Resolve(name),
                { } header => ((HeaderAttribute)header).Value,
            };
            if (!message.Headers.TryAddWithoutValidation(name, value))
                contentHeaders.Add(name, value);
        }

        if (method is POST_METHOD or PUT_METHOD)
        {
            if (formData.Count > 0)
            {
                MultipartFormDataContent formDataContent = new();
                foreach ((string key, object? val, bool isFileName) in formData)
                {
                    if (isFileName && val is FileInfo file)
                    {
                        using MemoryStream memoryStream = new();
                        file.OpenRead().CopyTo(memoryStream);
                        ByteArrayContent fileStreamContent = new(memoryStream.GetBuffer());
                        formDataContent.Add(fileStreamContent, key, Path.GetFileName(file.FullName));
                    }
                    else if (CreateContent(contentHeaders, content, contentType, jsonOptions) is { } nested)
                        formDataContent.Add(nested, key);

                }
                message.Content = formDataContent;
            }
            else if (content is { })
                message.Content = CreateContent(contentHeaders, content, contentType, jsonOptions);

            if (message.Content?.Headers is { } headers)
                foreach (var header in contentHeaders.AllKeys)
                    if (header is { } && contentHeaders.GetValues(header) is { } values)
                        foreach (var value in values)
                            headers.TryAddWithoutValidation(header, value);
        }
    }

    private static HttpContent? CreateContent(NameValueCollection contentHeaders, object? content, Type? contentType, JsonSerializerOptions jsonOptions)
    {
        return (contentHeaders["content-type"]?.ToLower(), content) switch
        {
            ({ } contentTypes, { }) when contentTypes.Contains(MediaTypeNames.Application.Json) || content is JsonDocument || content is JsonElement || (content as string)?.Trim() is ['{', .., '}'] =>
                JsonContent.Create(content, contentType!, mediaType: MediaTypeHeaderValue.Parse(contentTypes), jsonOptions),

            ({ } contentTypes, { }) when TryGetXmlContent(contentTypes, content, contentType!, out StringContent xmlStringContent) =>
               xmlStringContent,

            ({ } contentTypes, { }) when contentTypes.Contains("application/x-www-form-urlencoded") =>
                new FormUrlEncodedContent(GetPropertiesDictionary<object?, string?>(content, KeyToUrlEncode, v => ValueToUrlEncode(v, jsonOptions))),

            ({ } contentTypes, byte[] bytes) when contentTypes.Contains(MediaTypeNames.Application.Octet) =>
                new ByteArrayContent(bytes),

            (_, { }) => throw new ArgumentException("Unable to serialize object to content body", nameof(content)),

            _ => null
        };
    }

    private static IEnumerable<KeyValuePair<string, TOut>> GetPropertiesDictionary<T, TOut>(T props, Func<string, string>? keyTransform = null, Func<object?, TOut>? valueTransform = null)
    {
        keyTransform ??= k => k;
        valueTransform ??= o => o is { } _o ? (TOut)_o : default!;
        var inputType = typeof(T);
        return inputType == typeof(object) && props != null
            ? props.GetType().GetProperties().ToDictionary(p => keyTransform(p.Name), p => valueTransform(p.GetValue(props, null)))
            : props is T val ? inputType.GetProperties().ToDictionary(p => p.Name, p => valueTransform(p.GetValue(val, null))) :
                Array.Empty<KeyValuePair<string, TOut>>();
    }

    private static bool TryGetXmlContent<TContent>(string contentTypes, TContent content, Type returnType, out StringContent xml)
    {
        MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse(contentTypes);

        return (true switch
        {
            true when (content as string)?.Trim() is ['<', '?', 'x', 'm', 'l', .., '>'] xmlString =>
                xml = new StringContent(xmlString, mediaType),

            true when returnType.IsSerializable && contentTypes.Contains("/xml") =>
                xml = ObjectSerializationToContent(returnType, content, mediaType),

            true when content is XContainer xdoc =>
                xml = new StringContent(xdoc.ToString(), mediaType),

            _ => xml = null!
        }) is not null;

        static StringContent ObjectSerializationToContent(Type firstArgType, TContent? content, MediaTypeHeaderValue mediaType)
        {
            using MemoryStream memory = new();
            new XmlSerializer(firstArgType).Serialize(memory, content);
            return new(Encoding.UTF8.GetString(memory.GetBuffer()), mediaType);
        }
    }

    private string AddQuery(string requestUri, object? queryParam, Type? type, Dictionary<string, object?> queryFromParameters, JsonSerializerOptions jsonOptions)
    {
        List<(string, string?)> tuples = new();

        if (queryParam is (string, string?)[] pdic)
            tuples = pdic.ToList();

        if (type?.GetProperties() is { } properties)
            foreach (var p in properties)
            {
                tuples.Add((KeyToUrlEncode(p.Name), ValueToUrlEncode(TransformObjectValue(queryParam), jsonOptions)));
            }

        foreach (var (key, value) in queryFromParameters)
        {
            tuples.Add((KeyToUrlEncode(key), ValueToUrlEncode(TransformObjectValue(value), jsonOptions)));
        }

        if (GetQuery(tuples, jsonOptions) is { Length: > 0 } query)
            return requestUri + "?" + query;
        return requestUri;
    }

    public static string GetQuery<TValue>(List<(string key, TValue value)> props, JsonSerializerOptions jsonOptions, bool useArrayIndexer = false)
    {
        return string.Join("&",
            from kv in props
            group kv by kv.key into g
            let last = g.Last()
            select (useArrayIndexer && last.value is IEnumerable<TValue> enumerable) 
                ? string.Join("&", from val in enumerable select $"{KeyToUrlEncode(last.key)}[]=${ValueToUrlEncode(val, jsonOptions)}") 
                : string.Format("{0}{1}", last.key, last.value == null ? "" : "=" + last.value));
    }

    private string TransformObjectValue(object? queryParam)
    {
        return KeyToUrlEncode(queryParam switch
        {
            Enum en => EnumJsonConverter.GetSuitableValue(en, _initOptions.EnumSerialization).ToString()!,
            string str => _initOptions.PathAndQueryFormatter(str),
            { } item => _initOptions.PathAndQueryFormatter(JsonSerializer.Serialize(item, _initOptions.JsonOptions)),
            _ => "null"
        });
    }

    private static string? ValueToUrlEncode(object? arg, JsonSerializerOptions jsonOptions)
    {
        if (arg is { })
            if (arg is string str)
                return UrlEncoder.Default.Encode(str);
            else if (arg?.GetType() is { IsClass: true, })
                return UrlEncoder.Default.Encode(arg?.ToString() ?? "");
            else
                return JsonSerializer.Serialize(arg, jsonOptions);
        return arg?.ToString();

    }

    private static string KeyToUrlEncode([NotNull] string arg)
    {
        return UrlEncoder.Default.Encode(arg.ToString());

    }
}
