#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HttPie.Attributes;
using HttPie.Enums;
using HttPie.Policy;
using Microsoft.CodeAnalysis;

namespace HttPie.Generator;

internal sealed class BuilderOptions
{
    internal BuilderOptions(AttributeData attr, string agentName)
    {
        BaseUrl = new(attr.ConstructorArguments[0].Value!.ToString());
        AgentTypeName = agentName;

        if (attr.NamedArguments.ToDictionary(kv => kv.Key, kv => kv.Value) is { Count: > 0 } dic)
        {
            PathCasing = dic.TryGetValue(nameof(HttpOptionsAttribute.PathCasing), out var _pathCasing) ? (Casing)_pathCasing.Value! : Casing.None;
            QueryCasing = dic.TryGetValue(nameof(HttpOptionsAttribute.QueryCasing), out var _queryCasing) ? (Casing)_queryCasing.Value! : Casing.None;
            PropertyCasing = dic.TryGetValue(nameof(HttpOptionsAttribute.PropertyCasing), out var _propertyCasing) ? (Casing)_propertyCasing.Value! : Casing.None;
            EnumQueryCasing = dic.TryGetValue(nameof(HttpOptionsAttribute.EnumQueryCasing), out var _enumQueryCasing) ? (Casing)_enumQueryCasing.Value! : Casing.None;
            EnumSerializationCasing = dic.TryGetValue(nameof(HttpOptionsAttribute.EnumSerializationCasing), out var _enumSerializationCasing) ? (Casing)_enumSerializationCasing.Value! : Casing.None;
            DefaultBodyType = dic.TryGetValue(nameof(HttpOptionsAttribute.DefaultBodyType), out var _defaultBodyType) ? (BodyType)_defaultBodyType.Value! : BodyType.Json;
            DefaultResponseType = dic.TryGetValue(nameof(HttpOptionsAttribute.DefaultResponseType), out var _defaultResponseType) ? (ResponseType)_defaultResponseType.Value! : ResponseType.Json;
        }

        PathCasingFn = CasingPolicy.GetConverter(PathCasing);
        QueryPropCasingFn = CasingPolicy.GetConverter(QueryCasing);
        PropertyCasingFn = CasingPolicy.GetConverter(PropertyCasing);
        EnumQueryCasingFn = CasingPolicy.GetConverter(EnumQueryCasing);
        EnumSerializationCasingFn = CasingPolicy.GetConverter(EnumSerializationCasing);
    }

    internal Uri BaseUrl { get; }
    internal string AgentTypeName { get; }
    internal Casing PathCasing { get; } = Casing.CamelCase;
    internal Casing QueryCasing { get; } = Casing.CamelCase;
    internal Casing PropertyCasing { get; } = Casing.CamelCase;
    internal Casing EnumQueryCasing { get; } = Casing.CamelCase;
    internal Casing EnumSerializationCasing { get; } = Casing.CamelCase;
    internal Func<string, string> PathCasingFn { get; }
    internal Func<string, string> QueryPropCasingFn { get; }
    internal Func<string, string> EnumQueryCasingFn { get; }
    internal Func<string, string> EnumSerializationCasingFn { get; }
    internal Func<string, string> PropertyCasingFn { get; }
    public BodyType DefaultBodyType { get; set; } = BodyType.Json;
    public ResponseType DefaultResponseType { get; } = ResponseType.Json;
    internal string? SegmentFallback { get; set; }
    internal Dictionary<string, string> HelperMethods { get; } = new();
    internal bool NeedsJsonOptions { get; set; }
    internal bool NeedsXmlOptions { get; set; }

    internal List<string> logs = new();
}
