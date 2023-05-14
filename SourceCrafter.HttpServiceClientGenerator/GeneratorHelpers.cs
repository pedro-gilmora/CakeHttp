#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;
using System.Xml.Serialization;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Text;

namespace SourceCrafter.HttpServiceClient
{
    public static class GeneratorHelpers
    {

        internal static bool IsNullable(this ITypeSymbol typeSymbol) =>
            typeSymbol is { NullableAnnotation: NullableAnnotation.Annotated } or INamedTypeSymbol { Name: "Nullable" };
        
        internal static bool AllowsNull(this ITypeSymbol typeSymbol) =>
            typeSymbol is
            {
                IsValueType: false,
                IsTupleType: false
            } || typeSymbol.IsNullable();

        public static string Join<T>(this IEnumerable<T> strs, Func<T, string> formmater, string? separator = "") =>
            string.Join(separator, strs?.Select(formmater) ?? Enumerable.Empty<string>());

        public static string Join<T>(this IEnumerable<T> strs, string? separator = "")
            => string.Join(separator, strs);

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

        public static bool TryGetNameOfFromAttributeArg<TSymbol>(AttributeData attr, SemanticModel model, int index, out TSymbol symbol, Func<ISymbol, bool>? predicate = null)
            where TSymbol : class, ISymbol
        {
            return null != (symbol = attr.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax
            {
                ArgumentList.Arguments: { Count: { } count } args
            } && count > index && // It's inside the range
            args[index].Expression is InvocationExpressionSyntax
            {
                Expression: SimpleNameSyntax { Identifier.Text: "nameof" }, // Is nameof
                ArgumentList.Arguments: [{ Expression: { } expr }] // Has just one argument
            }
                    ? model.GetSymbolInfo(expr) switch // Contains symbol
                    {
                        { Symbol: TSymbol foundSymbol } => foundSymbol, // Directly found
                        {
                            CandidateSymbols: { Length: > 0 } symbols,
                            CandidateReason: CandidateReason.Ambiguous | CandidateReason.MemberGroup
                        } =>
                          (symbols.FirstOrDefault(predicate ?? IsTSymbol) as TSymbol)!, // The first one meeting the requirements
                        _ => default!
                    }
                    : default!);

            static bool IsTSymbol(ISymbol symbol) => symbol is TSymbol;
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

        public static string? Capitalize(this string? name) => name is { Length: > 1 } ? char.ToUpper(name[0]) + name[1..] : name;

        public static string ToCamel(this string name)
        {
            var buffer = new char[name.Length];
            int bufferIndex = 0;
            bool needUpper = false;

            foreach (char ch in name)
            {
                bool isDigit = char.IsDigit(ch), isLetter = char.IsLetter(ch), isUpper = char.IsUpper(ch);
                if (isLetter)
                {
                    buffer[bufferIndex++] = bufferIndex == 1 && isUpper
                        ? char.ToLower(ch)
                        : !isUpper && needUpper && bufferIndex > 1 && !char.IsUpper(buffer[bufferIndex - 2])
                            ? char.ToUpper(ch)
                            : ch;
                    needUpper = false;
                    continue;
                }
                else if (isDigit)
                {
                    if (bufferIndex == 0)
                        (buffer = new char[buffer.Length + 1])[bufferIndex++] = '_';
                    buffer[bufferIndex++] = ch;
                }
                needUpper = true;
            }
            return new string(buffer, 0, bufferIndex);
        }

        public static string ToPascal(this string name)
        {
            var buffer = "";
            var index = 0;
            var needUpper = true;
            char lastChar = default;

            foreach (char ch in name)
            {
                if (char.IsDigit(ch))
                {
                    if (index == 0)
                    {
                        buffer += '_';
                    }
                    buffer += ch;
                    needUpper = true;
                }
                else if (char.IsSeparator(ch) || ch is '_' or '-' or '~')
                    needUpper = true;
                else if (needUpper || char.IsLower(lastChar) && char.IsUpper(ch))
                {
                    buffer += char.ToUpperInvariant(ch);
                    needUpper = false;
                }
                else
                {
                    buffer += ch;
                    needUpper = false;
                }

                lastChar = ch;
            }

            return buffer;
        }

        public static string ToSnakeLower(this string name)
        {
            var buffer = "";
            var start = false;
            char lastChar = char.MinValue;

            for (int current = 0; current < name.Length; current++)
            {
                char ch = name[current];
                bool isUpper = char.IsUpper(ch), isDigit = char.IsDigit(ch), isSeparator = !char.IsLetterOrDigit(ch);

                if (isSeparator || (char.IsLower(lastChar) && isUpper) || isDigit)
                {
                    if (!start)
                    {
                        if (isSeparator)
                            continue;
                    }

                    if (char.IsLetterOrDigit(lastChar) && isSeparator || char.IsLower(lastChar) && isUpper)
                        buffer += '_';
                }

                if (!isSeparator)
                    buffer += char.ToLowerInvariant(ch);

                if (!start)
                    start = true;

                lastChar = ch;
            }

            return buffer;
        }

        public static string ToSnakeUpper(this string name)
        {
            var buffer = "";
            var start = false;
            char lastChar = default;

            for (int current = 0; current < name.Length; current++)
            {
                char ch = name[current];
                bool isUpper = char.IsUpper(ch), isDigit = char.IsDigit(ch), isSeparator = !char.IsLetterOrDigit(ch);

                if (isSeparator || (char.IsLower(lastChar) && isUpper) || isDigit)
                {
                    if (!start)
                    {
                        if (isSeparator)
                            continue;
                    }

                    if (char.IsLetterOrDigit(lastChar) && isSeparator || char.IsLower(lastChar) && isUpper)
                        buffer += '_';
                }

                if (!isSeparator)
                    buffer += char.ToUpperInvariant(ch);

                if (!start)
                    start = true;

                lastChar = ch;
            }

            return buffer;
        }

        public static string? ToCamel(this Enum? value) => value?.ToString().ToCamel();

        public static string? ToPascal(this Enum? value) => value?.ToString().ToPascal();

        public static string? ToSnakeLower(this Enum? value) => value?.ToString().ToSnakeLower();

        public static string ToSnakeUpper(this Enum value) => value.ToString().ToSnakeUpper();

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
}
