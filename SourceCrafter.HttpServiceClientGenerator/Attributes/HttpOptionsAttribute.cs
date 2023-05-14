using System;
using System.Text.Json.Serialization;
using SourceCrafter.HttpServiceClient.Enums;

namespace SourceCrafter.HttpServiceClient.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public sealed class HttpJsonServiceAttribute<T> : HttpServiceAttribute where T : JsonSerializerContext
{
    public HttpJsonServiceAttribute(string baseUrl) : base(baseUrl) { }
}

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class HttpServiceAttribute : Attribute
{
    public string BaseUrl { get; }
    public Casing PathCasing { get; set; }
    public Casing QueryCasing { get; set; }
    public Casing PropertyCasing { get;set; }
    public Casing EnumQueryCasing { get; set; }
    public Casing EnumSerializationCasing { get; set; }
    public BodyFormat DefaultBodyFormat { get; set; } = BodyFormat.Json;
    public ResultFormat DefaultResultFormat { get;set; } = ResultFormat.Json;
    public ResultFormat DefaultFormat { get;set; } = ResultFormat.Json;
    public Casing DefaultCasing { get; set; }

#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
    public HttpServiceAttribute(string baseUrl)
#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
    {
        BaseUrl = baseUrl;
    }
}