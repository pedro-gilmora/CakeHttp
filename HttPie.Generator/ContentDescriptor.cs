#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace HttPie.Generator;

internal sealed class OperationDescriptor
{
    internal OperationDescriptor(BuilderOptions opts)
    {
        ResponseFormatType = opts.DefaultResponseType.ToString();
        ContentFormatType = opts.DefaultBodyType.ToString();
    }
    public string? ContentParameterName { get; internal set; } = "content";
    public INamedTypeSymbol? QueryType { get; internal set; }
    public INamedTypeSymbol? ContentType { get; internal set; }
    public INamedTypeSymbol? ResponseType { get; internal set; }
    internal string ResponseFormatType { get; set; } = "Json";
    internal string ContentFormatType { get; set; } = "Json";
    internal string? QueryParameterName { get; set; } = "query";
    internal List<(string, string)> Headers { get; set; } = new();
}
