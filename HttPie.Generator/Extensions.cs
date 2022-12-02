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
using System.Text;
using HttPie.Enums;
using HttPie.Attributes;

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

        public static HttpRequestException GetJsonException<T>(this HttpResponseMessage response, JsonSerializerOptions opts, CancellationToken cancellation) {
            var result = new HttpRequestException(response.ReasonPhrase);
            if (response.Content is JsonContent { } content)
                result.Data[Constants.Constants.EXCEPTION_CONTENT] = content.ReadFromJsonAsync<T>(opts, cancellation);
            return result;
        }

        public static HttpRequestException GetXmlException<T>(this HttpResponseMessage response, CancellationToken cancellation) {
            var result = new HttpRequestException(response.ReasonPhrase);
            if (response.Content is { } content)
                result.Data[Constants.Constants.EXCEPTION_CONTENT] = content.ReadFromXmlAsync<T>(cancellation);
            return result;
        }

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

        public static string ToCamelCase(this string name)
        {
            StringBuilder builder = new();
            int current = -1;
            int length = name.Length;
            bool needUpper = true;
            char lastChar = char.MinValue;

            while (++current < length)
            {
                var ch = name[current];

                if (char.IsDigit(ch))
                {
                    if (builder.Length == 0)
                        builder.Append("_");

                    builder.Append(ch);
                    needUpper = true;
                }
                else if (char.IsSeparator(ch) || ch == '_' || ch == '-' || ch == '~')
                    needUpper = true;
                else if (builder.Length == 0 && char.IsUpper(ch))
                {
                    builder.Append(char.ToLowerInvariant(ch));
                    needUpper = false;
                }
                else if (builder.Length > 0 && (needUpper || (char.IsLower(lastChar) && char.IsUpper(ch))))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                    needUpper = false;
                }
                else
                {
                    builder.Append(ch);
                    needUpper = false;
                }

                lastChar = ch;
            }

            return builder.ToString();
        }

        public static string ToPascalCase(this string name)
        {
            StringBuilder builder = new();
            int current = -1;
            int length = name.Length;
            bool needUpper = true;
            char lastChar = char.MinValue;

            while (++current < length)
            {
                var ch = name[current];

                if (char.IsDigit(ch))
                {
                    if (builder.Length == 0)
                        builder.Append("_");

                    builder.Append(ch);
                    needUpper = true;
                }
                else if (char.IsSeparator(ch) || ch == '_' || ch == '-' || ch == '~')
                    needUpper = true;

                else if (needUpper || (char.IsLower(lastChar) && char.IsUpper(ch)))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                    needUpper = false;
                }
                else
                {
                    builder.Append(ch);
                    needUpper = false;
                }

                lastChar = ch;
            }

            return builder.ToString();
        }

        public static string ToLowerSnakeCase(this string name)
        {
            StringBuilder builder = new();
            int current = -1;
            int length = name.Length;
            bool start = false;
            char lastChar = char.MinValue;

            while (++current < length)
            {
                var ch = name[current];
                if (char.IsSeparator(ch))
                {
                    if (!start) continue;

                    builder.Append('_');
                }
                else
                {
                    bool isUpper = char.IsUpper(ch);

                    if ((char.IsLower(lastChar) && isUpper) || char.IsDigit(ch))
                        builder.Append('_');

                    if (isUpper)
                        builder.Append(char.ToLowerInvariant(ch));
                    else
                        builder.Append(ch);

                    if (!start)
                        start = true;

                    lastChar = ch;
                }
            }

            return builder.ToString();
        }

        public static string ToUpperSnakeCase(this string name)
        {
            StringBuilder builder = new();
            int current = -1;
            int length = name.Length;
            bool start = false;
            char lastChar = char.MinValue;

            while (++current < length)
            {
                var ch = name[current];
                if (char.IsSeparator(ch))
                {
                    if (!start) continue;

                    builder.Append('_');
                }
                else
                {
                    bool isUpper = char.IsUpper(ch);

                    if ((char.IsLower(lastChar) && isUpper) || char.IsDigit(ch))
                        builder.Append('_');

                    if (!isUpper)
                        builder.Append(char.ToUpperInvariant(ch));
                    else
                        builder.Append(ch);

                    if (!start)
                        start = true;

                    lastChar = ch;
                }
            }

            return builder.ToString();
        }

        public static string ToCamelCase(this Enum value) => value.ToString().ToCamelCase();

        public static string ToPascalCase(this Enum value) => value.ToString().ToPascalCase();

        public static string ToLowerSnakeCase(this Enum value) => value.ToString().ToLowerSnakeCase();

        public static string ToUpperSnakeCase(this Enum value) => value.ToString().ToUpperSnakeCase();

    }
}
