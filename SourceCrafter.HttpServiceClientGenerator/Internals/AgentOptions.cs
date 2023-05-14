#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Policies;
using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SourceCrafter.HttpServiceClient.Internals;

internal sealed class AgentOptions
{
    internal AgentOptions(AttributeData attr, string agentNamespace, string agentTypeName)
    {
        BaseUrl = new(attr.ConstructorArguments.FirstOrDefault().Value as string ?? "http://localhost");
        BaseUrlAbsolutePath = BaseUrl.AbsolutePath.TrimEnd('/');
        BaseAddress = BaseUrl.ToString().TrimEnd('/');
        AgentNamespace = agentNamespace;
        AgentTypeName = agentTypeName;
        AgentFullTypeName = $"{agentNamespace}.{agentTypeName}";

        if (attr.AttributeClass is { TypeArguments: [{ } contextType] })
        {
            DefaultJsonContext = contextType.ToDisplayString(ServiceGenerator.GlobalizedNamespace)+".Default.Options";
            DefaultResultFormat = DefaultFormat = ResultFormat.Json;
            DefaultBodyFormat = BodyFormat.Json;
        }

        foreach (var kv in attr.NamedArguments)
        {
            switch (kv.Key)
            {
                case nameof(HttpServiceAttribute.PathCasing): PathCasing = (Casing)kv.Value.Value!; continue;
                case nameof(HttpServiceAttribute.QueryCasing): QueryCasing = (Casing)kv.Value.Value!; continue;
                case nameof(HttpServiceAttribute.PropertyCasing): PropertyCasing = (Casing)kv.Value.Value!; continue;
                case nameof(HttpServiceAttribute.EnumQueryCasing): EnumQueryCasing = (Casing)kv.Value.Value!; continue;
                case nameof(HttpServiceAttribute.EnumSerializationCasing): EnumSerializationCasing = (Casing)kv.Value.Value!; continue;
                case nameof(HttpServiceAttribute.DefaultCasing):
                    EnumSerializationCasing = PathCasing = QueryCasing = PropertyCasing = EnumQueryCasing = DefaultCasing = (Casing)kv.Value.Value!;
                    continue;
                case nameof(HttpServiceAttribute.DefaultFormat) when DefaultJsonContext == null : 
                    DefaultBodyFormat = (DefaultResultFormat = DefaultFormat = (ResultFormat)kv.Value.Value!) switch
                    {
                        ResultFormat.Json => BodyFormat.Json,
                        ResultFormat.Xml => BodyFormat.Xml,
                        _ => DefaultBodyFormat
                    };
                    continue;
                case nameof(HttpServiceAttribute.DefaultBodyFormat) when DefaultJsonContext == null : DefaultBodyFormat = (BodyFormat)kv.Value.Value!; continue;
                case nameof(HttpServiceAttribute.DefaultResultFormat) when DefaultJsonContext == null : DefaultResultFormat = (ResultFormat)kv.Value.Value!; continue;
            }
        }
        
        PathCasingFn = CasingPolicy.GetConverter(PathCasing);
        QueryPropCasingFn = CasingPolicy.GetConverter(QueryCasing);
        PropertyCasingFn = CasingPolicy.GetConverter(PropertyCasing);
        EnumQueryCasingFn = CasingPolicy.GetConverter(EnumQueryCasing);
        EnumSerializationCasingFn = CasingPolicy.GetConverter(EnumSerializationCasing);
    }

    public ResultFormat DefaultFormat { get; private set; } = ResultFormat.Json;

    internal Uri BaseUrl { get; }
    internal string AgentTypeName { get; }
    internal string AgentNamespace { get; }
    internal Casing PathCasing { get; } = Casing.CamelCase;
    internal Casing QueryCasing { get; } = Casing.CamelCase;
    internal Casing PropertyCasing { get; } = Casing.CamelCase;
    internal Casing EnumQueryCasing { get; } = Casing.CamelCase;
    internal Casing EnumPathCasing { get; } = Casing.CamelCase;
    internal Casing EnumSerializationCasing { get; } = Casing.CamelCase;
    internal Func<string?, string?> PathCasingFn { get; }
    internal Func<string?, string?> QueryPropCasingFn { get; }
    internal Func<string?, string?> EnumQueryCasingFn { get; }
    internal Func<string?, string?> EnumSerializationCasingFn { get; }
    internal Func<string?, string?> PropertyCasingFn { get; }
    internal BodyFormat DefaultBodyFormat { get; set; } = BodyFormat.Json;
    internal ResultFormat DefaultResultFormat { get; } = ResultFormat.Json;
    internal string? SegmentFallback { get; set; }
    internal Dictionary<string, string> HelperMethods { get; } = new();
    internal bool NeedsJsonOptions { get; set; }
    internal bool NeedsXmlOptions { get; set; }
    internal string? BaseUrlAbsolutePath { get; set; }
    internal string BaseAddress { get; }
    internal string AgentFullTypeName { get; set; }
    internal (string, string)[] ResponseTypes { get; } = new(string, string)[] { };
    internal Casing DefaultCasing { get; }
    internal string? DefaultJsonContext { get; }

    internal List<string> Logs = new();
    internal Func<object?, StringBuilder> HelperMethod;
}
