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
using System;
using System.Runtime.Serialization;
using System.Net;

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
        MultipartFormDataContent multipartFormDataContent = [];
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

            { Headers.ContentType.MediaType: { } mediaType } when mediaType.Contains("xml") =>
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
}

public static class Extensions
{
    internal static bool IsNullable(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol != null && (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated || (typeSymbol is INamedTypeSymbol && typeSymbol.Name == "Nullable")))
        {
            return true;
        }

        return false;
    }

    internal static bool AllowsNull(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null || typeSymbol.IsValueType || typeSymbol.IsTupleType)
        {
            return typeSymbol?.IsNullable() ?? false;
        }

        return true;
    }

    public static string Join<T>(this IEnumerable<T> strs, string? separator = "")
    {
        return string.Join(separator, strs);
    }

    public static string ToCamel(this string name)
    {
        var buffer = new char[name.Length];
        var bufferIndex = 0;
        var needUpper = false;

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
        var buffer = new char[name.Length];
        var bufferIndex = 0;
        var needUpper = false;

        foreach (char ch in name)
        {
            bool isDigit = char.IsDigit(ch), isLetter = char.IsLetter(ch), isUpper = char.IsUpper(ch);

            if (isLetter)
            {
                buffer[bufferIndex] = ((bufferIndex++ == 0 || needUpper) && !isUpper)
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

    public static string ToSnakeLower(this string name) => ToJoined(name, "_");

    public static string ToSnakeUpper(this string name) => ToJoined(name, "_", true);

    static string ToJoined(this string name, string separator = "-", bool upper = false)
    {
        var buffer = new char[name.Length * (separator.Length + 1)];
        var bufferIndex = 0;

        for (int i = 0; i < name.Length; i++)
        {
            char ch = name[i];
            bool isLetterOrDigit = char.IsLetterOrDigit(ch), isUpper = char.IsUpper(ch);

            if (i > 0 && isUpper && char.IsLower(name[i - 1]))
            {
                separator.CopyTo(0, buffer, bufferIndex, separator.Length);
                bufferIndex += separator.Length;
            }
            if (isLetterOrDigit)
            {
                buffer[bufferIndex++] = (upper, isUpper) switch
                {
                    (true, false) => char.ToUpperInvariant(ch),
                    (false, true) => char.ToLowerInvariant(ch),
                    _ => ch
                };
            }
        }
        return new string(buffer, 0, bufferIndex);
    }

    public static string? ToCamel(this Enum? value) => value?.ToString().ToCamel();

    public static string? ToPascal(this Enum? value) => value?.ToString().ToPascal();

    public static string? ToSnakeLower(this Enum? value) => value?.ToString().ToSnakeLower();

    public static string ToSnakeUpper(this Enum value) => value.ToString().ToSnakeUpper();

}
