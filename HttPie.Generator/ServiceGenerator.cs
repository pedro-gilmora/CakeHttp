#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using HttPie.Enums;
using HttPie.CasingPolicies;
using Microsoft.CodeAnalysis.CSharp;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Text;
using System.Xml.Linq;
using System.Net;
using System.ComponentModel;

namespace HttPie.Generator;

[Generator]
public class ServiceClientGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //var comments = context.SyntaxProvider.CreateSyntaxProvider(
        //        static (node, _) => node.IsKind(SyntaxKind.MultiLineCommentTrivia),
        //        static (cst, _) => node.IsKind()
        //    );
        var interfaceDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "HttPie.Attributes.HttpOptionsAttribute",
                static (n, _) => n is InterfaceDeclarationSyntax,
                static (ctx, c) => (Attr: ctx.Attributes[0], semanticModel: ctx.SemanticModel, type: (ITypeSymbol)ctx.TargetSymbol)
            );

        context.RegisterSourceOutput(
            interfaceDeclarations,
            static (sourceProducer, gen) => CreateRelatedTypeFiles(sourceProducer, gen.semanticModel, gen.Attr, gen.type, true)
        );
    }

    private static void RegisterNamespace(HashSet<string> usings, params string[] namespaces)
    {
        foreach (var ns in namespaces)
            usings.Add(ns);
    }

    private static void CreateRelatedTypeFiles(SourceProductionContext productionContext, SemanticModel semanticModel, AttributeData attr, ITypeSymbol typeSymbol, bool root = false)
    {
        string
            interfaceName = typeSymbol.Name,
            globalNamespace = typeSymbol.ContainingModule.GlobalNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            name = interfaceName[1..].Replace("Api", ""),
            clientName = $"{name}Client",
            agentName = $"{name}Agent";

        Dictionary<string, string> fileAndContent = new();

        GetClientBasicConfig(attr, out var baseUrl, out var pathCasing, out var queryCasing, out var propertyCasing);

        Func<string, string>
            pathCasingFn = CasingPolicy.Default(pathCasing).ConvertName,
            queryCasingFn = CasingPolicy.Default(queryCasing).ConvertName,
            propertyCasingFn = CasingPolicy.Default(propertyCasing).ConvertName;

        AgentDescriptior agentDescriptor = new(
            baseUrl,
            agentName,
            pathCasing,
            queryCasing,
            propertyCasing,
            pathCasingFn,
            queryCasingFn,
            propertyCasingFn);

        CollectAndBuildRelatedTypes(
            fileAndContent,
            productionContext,
            semanticModel,
            typeSymbol,
            clientName,
            agentDescriptor,
            root);

        foreach (var kv in fileAndContent)
        {
            productionContext.AddSource($"{clientName}.{kv.Key}", $@"//<auto generated>
        {kv.Value}");
        }

    }

    private static void GetClientBasicConfig(AttributeData attributeData, out string baseUrl, out Casing pathCasing, out Casing queryCasing, out Casing propertyCasing)
    {
        baseUrl = attributeData.ConstructorArguments[0].Value!.ToString();
        while (baseUrl.EndsWith("/"))
            baseUrl = baseUrl[..^1];

        (pathCasing, queryCasing, propertyCasing) =
            attributeData.NamedArguments.ToDictionary(kv => kv.Key, kv => kv.Value) is { Count: > 0 } dic
            ? (
                dic.TryGetValue("PathCasing", out var _pathCasing) ? (Casing)_pathCasing.Value! : Casing.None,
                dic.TryGetValue("QueryCasing", out var _queryCasing) ? (Casing)_queryCasing.Value! : Casing.None,
                dic.TryGetValue("PropertyCasing", out var _propertyCasing) ? (Casing)_propertyCasing.Value! : Casing.None
            )
            : (Casing.None, Casing.None, Casing.None);
    }

    private static void CollectAndBuildRelatedTypes(Dictionary<string, string> fileAndContent, SourceProductionContext productionContext, SemanticModel semanticModel, ITypeSymbol typeSymbol, string typeName, AgentDescriptior agentDescriptor, bool root)
    {
        var containingNameSpace = typeSymbol.ContainingNamespace.ToString();
        var usings = new HashSet<string>(new NamespaceComparer("<global namespace>", containingNameSpace));
        try
        {
            createType(typeSymbol, true);
            //productionContext.AddSource($"{typeName}f.cs", $"/*{trivias.Join(u => $"{u.Key}: {u.Value}", "\n").Replace("*/", "*\\/")}*/");
        }
        catch (Exception e)
        {
            productionContext.AddSource($"{typeName}g.cs", $"/*{e}*/");
        }

        void createType(ISymbol symbol, bool root = false)
        {
            if (symbol is ITypeSymbol cls)
            {
                foreach (var member in cls.GetMembers())
                    if (member is IPropertySymbol prop)
                    {
                        createType(prop.Type);
                    }

                getMethodsInfo(cls);
            }

        }

        IEnumerable<string> getMethodsInfo(ITypeSymbol cls)
        {
            foreach (var sr in cls.DeclaringSyntaxReferences)
            {
                if (sr.GetSyntax() is InterfaceDeclarationSyntax { BaseList.Types: var baseTypes } iFace)
                {
                    foreach (var baseTypeDecl in baseTypes)
                    {
                        if (baseTypeDecl.Type is GenericNameSyntax { TypeArgumentList: { LessThanToken: var ltk, Arguments: { Count: int argsCount and > 0 } args } } type)
                        {
                            string
                                queryParameterName = "query",
                                contentDocType = "Json",
                                responseDocType = "Json";

                            INamedTypeSymbol
                                queryType = null,
                                contentType = null,
                                responseType = null;

                            INamedTypeSymbol paramsType = (INamedTypeSymbol)semanticModel.GetTypeInfo(type).Type!;

                            if (baseTypeDecl
                                .DescendantTrivia()
                                .Where(tr => tr.IsKind(SyntaxKind.MultiLineCommentTrivia))
                                .ToSyntaxTriviaList() is { Count: int commentCount and > 0 } comments)
                            {
                                for (int i = 0, commentIndex = 0, startPoint = ltk.FullSpan.End; i < argsCount; i++)
                                {
                                    (TypeSyntax typeArg, INamedTypeSymbol tInfo, string paramType) = (args[i], (INamedTypeSymbol)paramsType.TypeArguments[i], paramsType.TypeParameters[i].Name);

                                    var endPoint = typeArg.SpanStart;

                                    if (commentIndex < commentCount && comments[commentIndex] is { } comment && TextSpan.FromBounds(startPoint, endPoint).Contains(comment.FullSpan))
                                    {
                                        commentIndex++;
                                        switch (paramType)
                                        {
                                            case "TQuery":
                                                queryParameterName = comment.ToString();
                                                queryType = tInfo;
                                                break;
                                            case "TContent":
                                                contentDocType = comment.ToString();
                                                contentType = tInfo;
                                                break;
                                            case "TResponse":
                                                responseDocType = comment.ToString();
                                                responseType = tInfo;
                                                break;
                                        }
                                    }

                                    startPoint = typeArg.FullSpan.End;
                                }
                            }

                            foreach (var method in paramsType.GetMembers().OfType<IMethodSymbol>())
                            {
                                if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                                {
                                    yield return GenerateMethod(
                                        usings,
                                        agentDescriptor,
                                        paramsType,
                                        method,
                                        queryParameterName,
                                        contentDocType,
                                        responseDocType,
                                        queryType,
                                        contentType,
                                        responseType);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static string GenerateMethod(HashSet<string> usings, AgentDescriptior agentDescriptor, INamedTypeSymbol paramsType, IMethodSymbol method, string? queryParameterName, string? contentDocType, string? responseDocType, INamedTypeSymbol? queryType, INamedTypeSymbol? contentType, INamedTypeSymbol? responseType)
    {
        var name = method.Name;
        var methodType = name[..^5];
        return @$"
        
        public async Task{BuildReturnType(usings, responseType, out var returnType)} {name}({BuildParameters(usings, agentDescriptor, method, queryParameterName, contentDocType, queryType, contentType, out string? contentRefernce, out string? queryReference)})
        {{
            {BuildRequest(queryParameterName, methodType)}
            {BuildSend(returnType)}
            {BuildResponseHandle(methodType, returnType)}
        }}";

        string BuildSend(string? returnType) {
            return "";
        }
        string BuildResponseHandle(string methodType, string? returnType)
        {
            return "";
        }

        void registerQueryBuilder(ITypeSymbol queryType, string partTypeName)
        {
            var signature = $@"string BuildQuery({partTypeName} query)";
            var method = $@"

        internal {signature}
        {{
            if(query != null)
            {{
                var result = ""?"";

                {(partTypeName == "Object"
                        ? @"foreach(var prop in query.GetType().GetProperties())
                    if(prop.GetValue(query) is {} value)
                        result += $""&{_queryCasing(prop.Name)}={value}"";"
                        : string.Join(@"
                ", from member in queryType.GetMembers()
                   let prop = member as IPropertySymbol
                   where prop is { IsIndexer: false }
                   select BuildQueryProperty(prop)))}
                
                if(result.StartsWith(""?&""))
                    return result.Remove(1);
            }}

            return """";
        }}
            ";

            agentDescriptor.HelperMethods[signature] = method;

            string BuildQueryProperty(IPropertySymbol prop)
            {
                var result = $@"result += ""&{agentDescriptor.QueryCasingFn(prop.Name)}={{query.{prop.Name}}}"";";
                if (prop.Type is { IsReferenceType: true } or { SpecialType: SpecialType.System_String })
                    result += $@"

                    if(query.{prop.Name} != null)
                        {result}";
                return result;
            }
        }


    }

    private static string BuildRequest(string? queryParameterName, string? methodType)
    {
        return $"{BuildQueryStatements(queryParameterName, out var pathVar)}HttpMethod.{methodType}, new Uri({pathVar}, UriKind.Relative)";
    }

    private static string? BuildReturnType(HashSet<string> usings, INamedTypeSymbol? responseType, out string? returnTypeName)
    {
        if (responseType != null)
            return returnTypeName = GetType(usings, responseType);
        return returnTypeName = null;
    }

    private static string BuildParameters(HashSet<string> usings, AgentDescriptior agentDescriptor, IMethodSymbol method, string? queryParameterName, string? contentDocType, INamedTypeSymbol? queryType, INamedTypeSymbol? contentType, out string? contentRefernce, out string? queryReference)
    {
        contentRefernce = queryReference = null;
        var paramSkip = 0;
        var parametersSyntax = "";

        if (queryType != null)
        {
            paramSkip++;
            var queryTypeName = GetType(usings, queryType);
            parametersSyntax += $@"{queryTypeName} {queryParameterName}";
            queryReference = "";//TODO: 
        }

        if (contentType != null)
        {
            paramSkip++;

            if (paramSkip > 1)
                parametersSyntax += ", ";

            var contentTypeName = GetType(usings, contentType);

            parametersSyntax += $@"{contentTypeName} content";

            contentRefernce = $@" {{
                    Content = {contentDocType}Content.Create(content)
                }};";
        }

        foreach (var param in method.Parameters.Skip(paramSkip))
        {
            if (paramSkip > 1)
                parametersSyntax += ", ";

            parametersSyntax += $"{GetType(usings, param.Type)} {param.Name}";

            if (param.HasExplicitDefaultValue)
                parametersSyntax += $" = {param.ExplicitDefaultValue ?? agentDescriptor.SegmentFallback ?? "default"}";
        }
        return parametersSyntax;
    }

    private static string BuildQueryStatements(string? queryParameterName, out string pathVar)
    {
        pathVar = "_path";

        if (queryParameterName == null) 
            return ""
                ;
        var requestSyntax = "";

        pathVar = "path";
        requestSyntax += $@"var path = $""{{_path}}";
        requestSyntax += @""";
        ";
        return requestSyntax;
    }

    private static string GetType(HashSet<string> usings, ITypeSymbol type)
    {
        RegisterNamespace(usings, type.ContainingNamespace.ToString());

        return type switch
        {
            INamedTypeSymbol { Name: var name, TypeArguments: { Length: > 0 } generics }
                => $"{name}<{generics.Join(g => GetType(usings, g), ", ")}>",

            INamedTypeSymbol { IsTupleType: true, TupleElements: var elements }
                => $"({elements.Join(f => $"{GetType(usings, f.Type)}{(f.IsExplicitlyNamedTupleElement ? $" {f.Name}" : "")}", ", ")})",

            _
                => IsPrimitive(type as INamedTypeSymbol) ? type.ToString() : type.Name
        };
    }

    private static bool IsPrimitive(INamedTypeSymbol type)
    {
        if (type == null)
            return false;

        return type.SpecialType switch
        {
            SpecialType.System_Boolean or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_Byte or
            SpecialType.System_UInt16 or
            SpecialType.System_UInt32 or
            SpecialType.System_UInt64 or
            SpecialType.System_Decimal or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Char or
            SpecialType.System_String => true,
            _ => false
        };
    }
}


file class NamespaceComparer : IEqualityComparer<string>
{
    private readonly string[] _symbolDisplayParts;

    public NamespaceComparer(params string[] symbolDisplayParts)
    {
        _symbolDisplayParts = symbolDisplayParts;
    }

    public bool Equals(string x, string y)
    {
        return x == y || _symbolDisplayParts.Any(z => z == x || z == y);
    }

    public int GetHashCode(string obj)
    {
        return StringComparer.InvariantCulture.GetHashCode(obj);
    }
}

internal class AgentDescriptior
{
    internal AgentDescriptior(string baseUrl, string agentName, Casing pathCasing, Casing queryCasing, Casing propertyCasing, Func<string, string> pathCasingFn, Func<string, string> queryCasingFn, Func<string, string> propertyCasingFn)
    {
        BaseUrl = baseUrl;
        AgentName = agentName;
        PathCasing = pathCasing;
        QueryCasing = queryCasing;
        PropertyCasing = propertyCasing;
        PathCasingFn = pathCasingFn;
        QueryCasingFn = queryCasingFn;
        PropertyCasingFn = propertyCasingFn;
    }

    internal string BaseUrl { get; }
    internal string AgentName { get; }
    internal Casing PathCasing { get; }
    internal Casing QueryCasing { get; }
    internal Casing PropertyCasing { get; }
    internal Func<string, string> PathCasingFn { get; }
    internal Func<string, string> QueryCasingFn { get; }
    internal Func<string, string> PropertyCasingFn { get; }
    internal string SegmentFallback { get; set; }
    internal Dictionary<string, string> HelperMethods { get; } = new();
    internal bool NeedsJsonOptions { get; set; }
    internal bool NeedsXmlOptions { get; set; }
}
