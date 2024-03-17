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
#pragma warning disable CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
    internal AgentOptions(AttributeData attr, string agentNamespace, string agentTypeName)
#pragma warning restore CS8618 // Un campo que no acepta valores NULL debe contener un valor distinto de NULL al salir del constructor. Considere la posibilidad de declararlo como que admite un valor NULL.
    {
        BaseUrl = new(attr.ConstructorArguments.FirstOrDefault().Value as string ?? "http://localhost");
        BaseUrlAbsolutePath = BaseUrl.AbsolutePath.TrimEnd('/');
        BaseAddress = BaseUrl.ToString().TrimEnd('/');
        AgentNamespace = agentNamespace;
        HandlerTypeName = agentTypeName;
        FullTypeName = $"{agentNamespace}.{agentTypeName}";
        string[]? strings = null;

        if(strings?.Length > 0) { }

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
                    EnumSerializationCasing = 
                        PathCasing =
                        QueryCasing =
                        PropertyCasing = 
                        EnumQueryCasing = 
                        DefaultCasing = (Casing)kv.Value.Value!;
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

    internal readonly Uri BaseUrl;

    public ResultFormat DefaultFormat { get; private set; } = ResultFormat.Json;

    internal BodyFormat DefaultBodyFormat = BodyFormat.Json;

    internal readonly ResultFormat DefaultResultFormat = ResultFormat.Json;

    internal readonly (string, string)[] ResponseTypes = [];

    internal readonly Dictionary<string, string> HelperMethods = [];

    internal bool
        NeedsJsonOptions,
        NeedsXmlOptions;

    internal readonly string?
        SegmentFallback,
        DefaultJsonContext,
        BaseUrlAbsolutePath;

    internal readonly string 
        HandlerTypeName,
        AgentNamespace,
        BaseAddress,
        FullTypeName;

    internal readonly Func<string?, string?>
        PathCasingFn,
        QueryPropCasingFn,
        EnumQueryCasingFn,
        EnumSerializationCasingFn,
        PropertyCasingFn;

    internal readonly Casing 
        PathCasing = Casing.CamelCase,
        QueryCasing = Casing.CamelCase,
        PropertyCasing = Casing.CamelCase,
        EnumQueryCasing = Casing.CamelCase,
        EnumPathCasing = Casing.CamelCase,
        EnumSerializationCasing = Casing.CamelCase,
        DefaultCasing;
}
