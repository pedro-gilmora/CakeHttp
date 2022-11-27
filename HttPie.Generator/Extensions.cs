#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Serialization;

namespace HttPie.Generator
{
    public static class HttPieHelpers
    {
        public static string Join<T>(this IEnumerable<T> strs, Func<T, string> formmater, string? separator = "")
        {
            return string.Join(separator, strs?.Select(formmater) ?? Enumerable.Empty<string>());
        }
        public static string Join<T>(this IEnumerable<T> strs, string? separator = "")
        {
            return Join(strs, t => t?.ToString() ?? "", separator);
        }

        public static T[] ArrayFrom<T>(params T[] items) => items;

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

        public static StreamContent CreateXml<T>(this T tIn, JsonSerializerOptions? opts = null)
        {
            using MemoryStream memoryStream = new();
            new XmlSerializer(typeof(T)).Serialize(memoryStream, tIn);
            return new StreamContent(memoryStream);
        }

        public static MultipartFormDataContent CreateMultipartFormData(this (HttpContent, string?, string?)[] contents)
        {
            MultipartFormDataContent multipartFormDataContent = new();
            foreach((HttpContent content, string? name, string? fileName) in contents)
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

        public static JsonContent CreateJson<T>(this T tIn, JsonSerializerOptions? opts = null)
        {
            return JsonContent.Create(tIn, options: opts);
        }

        public static async Task<T> ReadFromXmlAsync<T>(this HttpContent content, CancellationToken? token = default)
        {
            return (T)new XmlSerializer(typeof(T)).Deserialize(await content.ReadAsStreamAsync());
        }
    }
}
