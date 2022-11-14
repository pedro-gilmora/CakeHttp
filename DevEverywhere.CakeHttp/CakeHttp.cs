using System.Xml;
using System.Text;
using System.Net.Mime;
using System.Xml.Linq;
using System.Text.Json;
using System.Reflection;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Xml.Serialization;
using System.Text.Encodings.Web;
using DevEverywhere.CakeHttp.Enums;
using DevEverywhere.CakeHttp.Attributes;
using DevEverywhere.CakeHttp.Converters;
using DevEverywhere.CakeHttp.Inferfaces;

namespace DevEverywhere.CakeHttp;

public class CakeHttp : DispatchProxy
{
    #region Utils
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

    #endregion

    #region Creators
    public static TApi CreateClient<TApi>() where TApi : class
    {
        Type apiType = typeof(TApi);
        if (apiType.GetCustomAttribute<CakeHttpOptionsAttribute>() is{ } initOptions)
        {
            var httpClient = new HttpClient() { BaseAddress = new Uri(initOptions.BaseUrl) };
            return CreateClient<TApi>(httpClient, initOptions, apiType);
        }
        throw new TypeInitializationException(
            apiType.FullName,
            new Exception("You must provide a URL value through url parameter or the CakeHttpInitOptionsAttribute")
        );
    }

    public static TApi CreateClient<TApi>(string url, bool camelCasePathAndQuery = false, PropertyCasing enumSerialization = PropertyCasing.CamelCase) where TApi : class
    {
        var client = new HttpClient() { BaseAddress = new Uri(url) };
        var options = new CakeHttpOptionsAttribute(url, camelCasePathAndQuery, enumSerialization);
        return CreateClient<TApi>(client, options, typeof(TApi));
    }

    public static TApi CreateClient<TApi>(HttpClient client, bool camelCasePathAndQuery = false, PropertyCasing enumSerialization = PropertyCasing.CamelCase) where TApi : class
    {
        CakeHttpOptionsAttribute options = new (client.BaseAddress!.ToString(), camelCasePathAndQuery, enumSerialization);
        return CreateClient<TApi>(client, options, typeof(TApi));
    }

    internal static TApi CreateClient<TApi>(HttpClient httpClient, CakeHttpOptionsAttribute opts, Type apiType) where TApi : class
    {

        httpClient.BaseAddress ??= new (opts.BaseUrl);

        if (apiType.GetCustomAttributes<RequestHeaderAttribute>()?.ToList() is { } defaultRequestHeaders)
            defaultRequestHeaders.ForEach(header =>
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Name, header.Value);
            });

        if (apiType.GetCustomAttributes<ContentHeaderAttribute>()?.ToList() is { } defaultRequestContentHeaders)
            defaultRequestContentHeaders.ForEach(contentHeader =>
            {
                opts.RequestContentHeaders.Add(contentHeader.Name, contentHeader.Value);
            });

        dynamic apiProxy = Create<TApi, CakeHttp>()!;
        apiProxy._httpClient = httpClient;
        apiProxy._initOptions = opts;

        return (TApi)apiProxy;
    }
    #endregion

    #region Instance Members

    private CakeHttpOptionsAttribute _initOptions = null!;
    private HttpClient _httpClient = null!;
    private List<object?> _pathSegments = new();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is { Name: { } name, IsGenericMethod: { } isGeneric, ReturnType: { } methodReturnType })
        {
            #region Attributes
            if (targetMethod.GetCustomAttributes<ContentHeaderAttribute>()?.ToList() is { } defaultRequestContentHeaders)
                defaultRequestContentHeaders.ForEach(contentHeader =>
                {
                    if (!_initOptions.RequestContentHeaders.TryAdd(contentHeader.Name, contentHeader.Value))
                        _initOptions.RequestContentHeaders[contentHeader.Name] = contentHeader.Value;
                });
            #endregion

            #region Properties and Indexers
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
            #endregion

            #region API Invocation
            ParameterInfo[] parameterInfos = targetMethod.GetParameters();

            var method = targetMethod.Name.ToUpper() switch
            {
                { } deleteParse when deleteParse.Contains(DELETE_METHOD) => DELETE_METHOD,
                { } postParse when postParse.Contains(POST_METHOD) => POST_METHOD,
                { } putParse when putParse.Contains(PUT_METHOD) => PUT_METHOD,
                _ => GET_METHOD,
            };

            var jsonOptions = _initOptions.JsonOptions;

            string url = BuildPath(_pathSegments, _initOptions);

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

            var request = CreateRequest(method, url, query, queryType, queryFromParameters, jsonOptions, _initOptions);

            var dynamicMethod = returnType == voidType ? invokeRequestMethod : getResponseMethod;

            return CakeHttp.ReturnAs(
                returnType,
                _httpClient,
                request,
                method,
                content,
                contentType,
                formData,
                targetMethod
                    .GetCustomAttributes<HeaderBaseAttribute>()
                    .Concat(CreateContentHeaders(_initOptions.RequestContentHeaders))
                    .ToArray(),
                requestHandlers,
                responseHandlers,
                jsonOptions,
                token
            );

            #endregion
        }
        return this;
    }

    #endregion

    #region Helpers

    private static HttpRequestMessage CreateRequest(string method, string url, object? query, Type? queryType, Dictionary<string, object?> queryFromParameters, JsonSerializerOptions jsonOptions, ICakeHttpInitOptions cakeOptions)
    {
        var httpMethod = method.ToUpperInvariant() switch
        {
            POST_METHOD => HttpMethod.Post,
            PUT_METHOD => HttpMethod.Put,
            DELETE_METHOD => HttpMethod.Delete,
            _ => HttpMethod.Get
        };

        url = method is GET_METHOD or DELETE_METHOD && (query is { } && queryType is { } || queryFromParameters.Count > 0)
            ? AddQuery(url, query, queryType, queryFromParameters, jsonOptions, cakeOptions)
            : url;

        HttpRequestMessage request = new(httpMethod, url);

        return request;
    }  
    
    private static async Task InvokeRequest(HttpClient httpClient, HttpRequestMessage request, string method, object? content, Type? contentType, List<(string, object?, bool)> formData, HeaderBaseAttribute[] headers, Func<HttpRequestMessage, Task>? requestHandler, Func<HttpResponseMessage, Task>? responseHandler, JsonSerializerOptions jsonOptions, CancellationToken token)
    {
        await SetContentAndHeaders(request, method, content, contentType, formData, headers, jsonOptions);

        if (requestHandler is { })
            await requestHandler(request);

        var response = await httpClient.SendAsync(request, token);

        request.Dispose();

        if (responseHandler is { })
            await responseHandler(response);
    }
    
    private static async Task<T> GetResponse<T>(Type returnType, HttpClient httpClient, HttpRequestMessage request, string method, object? content, Type? contentType, List<(string, object?, bool)> formData, HeaderBaseAttribute[] requestContents, Func<HttpRequestMessage, Task>? requestHandler, Func<HttpResponseMessage, Task>? responseHandler, JsonSerializerOptions jsonOptions, CancellationToken token)
    {
        await SetContentAndHeaders(request, method, content, contentType, formData, requestContents, jsonOptions);

        if (requestHandler is { })
            await requestHandler(request);

        using HttpResponseMessage response = await httpClient.SendAsync(request, token);

        request.Dispose();

        if (responseHandler is { })
            await responseHandler(response);

        if (response is { Content: { Headers: { } _headers } responseContent })
        {
            //JSON
            if (response.Content.Headers.ContentType?.MediaType?.Contains("application/json") ?? false)
                return (await responseContent.ReadFromJsonAsync<T>(jsonOptions, token))!;

            //XML
            else if (response.Content.Headers.ContentType?.MediaType?.Contains("application/xml") ?? false)
                // Type
                if (returnType.IsSerializable)
                    return (T)(new XmlSerializer(returnType).Deserialize(await GetXmlReaderAsync(responseContent, true, token)))!;

                //XDocument
                else if (returnType == typeof(XDocument))
                    return (T)((object)XDocument.Parse(await responseContent.ReadAsStringAsync()))!;
                // XElement
                else
                    return (T)(object)XElement.Parse(await responseContent.ReadAsStringAsync());

            // String
            else if (returnType == typeof(string))
                return (T)(object)await responseContent.ReadAsStringAsync();

            // Stream
            else if (typeof(Stream).IsAssignableFrom(returnType))
                return (T)(object)await responseContent.ReadAsStreamAsync();

            // Byte[]
            else if (returnType == typeof(byte[]))
                return (T)(object)await responseContent.ReadAsByteArrayAsync();

            // Not serializable
            throw new FormatException($"Unable to deserialize {responseContent.GetType().Name} into {returnType.Name}");
        }

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(response.ReasonPhrase);

        return default!;
    }

    private static string AddQuery(string requestUri, object? queryParam, Type? type, Dictionary<string, object?> queryFromParameters, JsonSerializerOptions jsonOptions, ICakeHttpInitOptions options)
    {
        List<(string, string?)> tuples = new();

        if (queryParam is (string, string?)[] pdic)
            tuples = pdic.ToList();

        if (type?.GetProperties() is { } properties)
            foreach (var p in properties)
            {
                tuples.Add((KeyToUrlEncode(p.Name), ValueToUrlEncode(TransformObjectValue(queryParam, options), jsonOptions)));
            }

        foreach (var (key, value) in queryFromParameters.Deconstruct())
        {
            tuples.Add((KeyToUrlEncode(key), ValueToUrlEncode(TransformObjectValue(value, options), jsonOptions)));
        }

        if (GetQuery(tuples, jsonOptions) is { Length: > 0 } query)
            return requestUri + "?" + query;
        return requestUri;
    }

    private static string TransformObjectValue(object? queryParam, ICakeHttpInitOptions options)
    {
        return KeyToUrlEncode(queryParam switch
        {
            Enum en => EnumJsonConverter.GetSuitableValue(en, options.EnumSerialization).ToString()!,
            string str => options.PathAndQueryFormatter(str),
            { } item => options.PathAndQueryFormatter(JsonSerializer.Serialize(item, options.JsonOptions)),
            _ => "null"
        });
    }

    private static string BuildPath(List<object?> pathSegments, ICakeHttpInitOptions options)
    {
        return pathSegments.Aggregate("", (list, item) => list + AddSegmentSeparator(list) + item switch
        {
            Enum en => options.PathAndQueryFormatter(KeyToUrlEncode(en.ToString())),
            string str => options.PathAndQueryFormatter(KeyToUrlEncode(str)),
            { } => options.PathAndQueryFormatter(KeyToUrlEncode(JsonSerializer.Serialize(item, options.JsonOptions))),
            _ => "null"
        });
    } private static IEnumerable<HeaderBaseAttribute> CreateContentHeaders(Dictionary<string, string> requestContentHeaders)
    {
        foreach ((string key, string value) in requestContentHeaders.Deconstruct())
            yield return new RequestHeaderAttribute(key, value);
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

                else if (paramName != "query" && pInfo.GetCustomAttribute<AsQueryValueAttribute>() is not null)
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

        responseHandlers -= defaultResponseHandler;
        requestHandlers -= defaultRequestHandler;
    }

    private static HttpContent? CreateContent(string? contentTypeHeader, object? content, Type? contentType, JsonSerializerOptions jsonOptions)
    {
        return (contentTypeHeader, content) switch
        {
            ({ } contentTypes, { }) when contentTypes.Contains("application/json") || content is JsonDocument || content is JsonElement || (content as string)?.Trim() is ['{', .., '}'] =>
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

    private static dynamic ReturnAs(Type returnType, params object?[] parameters)
    {
        if (returnType == voidType)
            return invokeRequestMethod
                .Invoke(null, parameters)!;

        return getResponseMethod
            .MakeGenericMethod(new[] { returnType })
            .Invoke(null, new[] { returnType }.Concat(parameters).ToArray())!;
    }

    private static string AddSegmentSeparator(string list)
    {
        return string.IsNullOrEmpty(list) ? "" : "/";
    }

    private static async Task<XmlReader> GetXmlReaderAsync(HttpContent content, bool mandatoryDocument, CancellationToken token)
    {
        string xmlText = await content.ReadAsStringAsync();
        if (mandatoryDocument && !xmlText.StartsWith("<?xml"))
            xmlText = xmlText.Insert(0, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        return XDocument.Parse(xmlText).CreateReader();
    }
    
    private static async Task SetContentAndHeaders(HttpRequestMessage message, string method, object? content, Type? contentType, List<(string, object?, bool)> formData, HeaderBaseAttribute[] headers, JsonSerializerOptions jsonOptions)
    {
        var headersList = headers.ToList();
        string? contentTypeHeader = null;

        for (int i = 0; i < headersList.Count; i++)
        {
            HeaderBaseAttribute? resolver = headersList[i];
            (string name, string? value) = await GetHeaderNameAndValue(resolver);
            if (message.Headers.TryAddWithoutValidation(name, value))
                headersList.Remove(resolver);
            if(name == "content-type")
                contentTypeHeader = value;
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
                        var fileStream = file.OpenRead();
                        FileStreamToByteArrayContent(formDataContent, key, file, fileStream);
                    }
                    else if (CreateContent(contentTypeHeader, content, contentType, jsonOptions) is { } nested)
                        formDataContent.Add(nested, key);

                }
                message.Content = formDataContent;
            }
            else if (content is { })
                message.Content = CreateContent(contentTypeHeader, content, contentType, jsonOptions);

            if (message.Content?.Headers is { } _headers)
                for (int i = 0; i < headersList.Count; i++)
                {
                    HeaderBaseAttribute? resolver = headersList[i];
                    (string name, string? value) = await GetHeaderNameAndValue(resolver);
                    if(!_headers.Contains(name))
                        _headers.TryAddWithoutValidation(name, value);
                }
        }
    }

    private static void FileStreamToByteArrayContent(MultipartFormDataContent formDataContent, string key, FileInfo file, FileStream fileStream)
    {
        using MemoryStream memoryStream = new();
        fileStream.CopyTo(memoryStream);
        ByteArrayContent byteArratyContent = new(memoryStream.GetBuffer());
        formDataContent.Add(byteArratyContent, key, Path.GetFileName(file.FullName));
    }

    private static async Task<(string, string)> GetHeaderNameAndValue(HeaderBaseAttribute headerAttr)
    {
        string name = headerAttr.Name.ToLower();
        return (
            name,
            headerAttr switch
            {
                HeaderAsyncResolverAttribute<IAsyncValueResolver> async => await async.Resolver.ResolveAsync(name),
                HeaderResolverAttribute<IValueResolver> normal => normal.Resolver.Resolve(name),
                { } header => ((RequestHeaderAttribute)header).Value,
            }
        );
            
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
                xml = new StringContent(xmlString, Encoding.Default, mediaType.ToString()),

            true when returnType.IsSerializable && contentTypes.Contains("/xml") =>
                xml = ObjectSerializationToContent(returnType, content, mediaType),

            true when content is XContainer xdoc =>
                xml = new StringContent(xdoc.ToString(), Encoding.Default, mediaType.ToString()),

            _ => xml = null!
        }) is not null;

        static StringContent ObjectSerializationToContent(Type firstArgType, TContent? content, MediaTypeHeaderValue mediaType)
        {
            using MemoryStream memory = new();
            new XmlSerializer(firstArgType).Serialize(memory, content);
            return new(Encoding.Default.GetString(memory.GetBuffer()), Encoding.Default, mediaType.ToString());
        }
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

    private static string KeyToUrlEncode(string arg)
    {
        if (arg is null)
        {
            throw new ArgumentNullException(nameof(arg));
        }

        return UrlEncoder.Default.Encode(arg.ToString());

    }

    #endregion
}
