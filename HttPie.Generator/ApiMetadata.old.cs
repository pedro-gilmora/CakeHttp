using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

internal class ApiMetadata
{
    Dictionary<string, string> requestHeaders = new();
    HashSet<InterfaceDeclarationSyntax> types = new();
    string url;
    private readonly GeneratorExecutionContext _context;

    public string PathAndSegmentCasing { get; }

    public ApiMetadata(InterfaceDeclarationSyntax __interface, GeneratorExecutionContext context)
    {
        foreach (var attrs in __interface.AttributeLists)
        {
            foreach (var attr in attrs.Attributes)
            {
                if($"{attr.Name}" == "RequestHeaderAttribute")
                {
                    var keyArg = attr.ArgumentList.Arguments[0].Expression.ToString();
                    var valueArg = attr.ArgumentList.Arguments[1].Expression.ToString();
                    requestHeaders[keyArg] = valueArg;
                }
            }
        }
        BuildHeaders(__interface.AttributeLists);
        _context = context;
    }

    internal void RegisterInterface(TypeSyntax typeName)
    {
        //types.Add(typeName);
    }

    internal void RegisterParameterMetadata(ParameterSyntax ip)
    {
        //throw new NotImplementedException();
    }

    private void BuildHeaders(SyntaxList<AttributeListSyntax> attributeLists)
    {
        //throw new NotImplementedException();
    }
}