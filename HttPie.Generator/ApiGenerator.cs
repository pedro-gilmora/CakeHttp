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

namespace HttPie.Generator;

[Generator]
public class SdkApiImplementor : IIncrementalGenerator
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
                static (ctx, c) => (Attr: ctx.Attributes[0], semanticModel: ctx.SemanticModel, type: ctx.TargetSymbol as ITypeSymbol)
            );

        context.RegisterSourceOutput(
            interfaceDeclarations,
            static (sourceProducer, gen) => CreateRelatedTypeFiles(sourceProducer, gen.semanticModel, gen.Attr, gen.type, true)
        );
    }

    private static void RegisterNamespace(HashSet<string> usings, params string[] namespaces)
    {
        foreach (var ns in namespaces)
        {
            if (ns != "<global namespace>")
                usings.Add(ns);
        }
    }



    private static void CreateRelatedTypeFiles(SourceProductionContext productionContext, SemanticModel semanticModel, AttributeData attr, ITypeSymbol typeSymbol, bool root = false)
    {
        string interfaceName = typeSymbol.Name;
        string globalNamespace = typeSymbol.ContainingModule.GlobalNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        string name = interfaceName[1..].Replace("Api", "");
        string clientName = $"{name}Client";
        string agentName = $"{name}Agent";

        Dictionary<string, string> fileAndContent = new();

        GetClientBasicConfig(attr, out var baseUrl, out var pathCasing, out var queryCasing, out var propertyCasing);

        Func<string, string>
            pathCasingFn = CasingPolicy.Default(pathCasing).ConvertName,
            queryCasingFn = CasingPolicy.Default(queryCasing).ConvertName,
            propertyCasingFn = CasingPolicy.Default(propertyCasing).ConvertName;

        AgentDescriptior agentDescriptor = new(baseUrl, agentName, pathCasing, queryCasing, propertyCasing, pathCasingFn, queryCasingFn, propertyCasingFn);

        CollectAndBuildRelatedTypes(fileAndContent, productionContext, typeSymbol, clientName, agentDescriptor, root);

        foreach (var kv in fileAndContent)
        {
            productionContext.AddSource($"{clientName}.{kv.Key}", $@"//<auto generated>
        {kv.Value}");
        }

    }

    private static void CollectAndBuildRelatedTypes(Dictionary<string, string> fileAndContent, SourceProductionContext productionContext, ITypeSymbol typeSymbol, string typeName, AgentDescriptior agentDescriptor, bool root)
    {
        List<string> trivias = new();

        collect(typeSymbol, true);

        void collect(ISymbol symbol, bool root = false)
        {
            if (symbol is ITypeSymbol { } cls)
            {
                if (cls.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is InterfaceDeclarationSyntax { BaseList.Types:{  } baseTypes } iFace)
                {
                    foreach (var baseTypeDecl in baseTypes)
                        if (baseTypeDecl.Type is GenericNameSyntax { TypeArgumentList: { LessThanToken: { } ltk, GreaterThanToken: { } gtk, Arguments: { } args } } type)
                        {
                            if (baseTypeDecl.FindTrivia(ltk.SpanStart, tr => tr.IsKind(SyntaxKind.MultiLineCommentTrivia | SyntaxKind.SingleLineCommentTrivia)) is { } triv)
                                trivias.Add(triv.ToFullString());
                        }
                        //        if (baseTypeDecl.Type is GenericNameSyntax { TypeArgumentList:{ Arguments: { } types } })
                        //            foreach (var implTypes in types)
                        //                if (implTypes is GenericNameSyntax { TypeArgumentList: { Arguments: { } Impl } })
                        //if (baseTypeDecl is { RawKind: (int)SyntaxKind.MultiLineCommentTrivia, Span.Length: > 0 } coment)
                        //    trivias.Add(baseTypeDecl.ToString() + baseTypeDecl.FullSpan.ToString());
                }

                // detect attributes
                foreach (var member in cls.GetMembers())
                    if (member is IPropertySymbol prop)
                        collect(prop.Type);

            }

        }
        productionContext.AddSource($"{typeName}g.cs", $"/*{trivias.Join("\n").Replace("*/","*\\/")}*/");
        //        try
        //        {
        //            var containingNameSpace = typeSymbol.ContainingNamespace.ToString();
        //            var usings = new HashSet<string>(new NamespaceComparer("<global namespace>", containingNameSpace));

        //            var result = "";

        //            appendClassType();

        //            if (root)
        //            {
        //                appendAgentType();
        //            }

        //            buildFileContent();

        //            fileAndContent.Add($"{typeName}.g.cs", result);

        //            #region Global Generators
        //            // TODO: Evaluate the possibility to have multiple exchange docs optios (XML and JSON Options)
        //            void appendAgentType()
        //            {
        //                RegisterNamespace(usings,
        //                    "HttPie.Enums",
        //                    "HttPie.CasingPolicies",
        //                    "System.Net.Http");

        //                if (agentDescriptor.NeedsJsonOptions)
        //                    RegisterNamespace(usings,
        //                        "System.Text.Json",
        //                        "System.Net.Http.Json",
        //                        "System.Text.Json.Serialization");


        //                result = $@"
        //    internal sealed class {agentDescriptor.AgentName} 
        //    {{

        //        internal HttpClient _httpClient;{(agentDescriptor.NeedsJsonOptions ? @"
        //        internal JsonSerializerOptions _jsonOptions;" : "")}
        //        internal Func<string, string> _pathCasing, _queryCasing, _propertyCasing;

        //        internal {agentDescriptor.AgentName}()
        //        {{
        //            _httpClient = new HttpClient {{ 
        //                BaseAddress = new Uri(""{agentDescriptor.BaseUrl}"")
        //            }};{(agentDescriptor.NeedsJsonOptions ? $@"
        //            _jsonOptions = new JsonSerializerOptions {{                
        //                ReferenceHandler = ReferenceHandler.IgnoreCycles,
        //                PropertyNamingPolicy = CasingPolicy.Default(Casing.{agentDescriptor.PropertyCasing})
        //            }};" : "")}
        //            _pathCasing = CasingPolicy.Default(Casing.{agentDescriptor.PathCasing}).ConvertName;
        //            _queryCasing = CasingPolicy.Default(Casing.{agentDescriptor.QueryCasing}).ConvertName;
        //            _propertyCasing = CasingPolicy.Default(Casing.{agentDescriptor.PropertyCasing}).ConvertName;
        //        }}___HELPER_METHODS___

        //    }}{result}";
        //            }

        //            void appendClassType()
        //            {
        //                result += $@"
        //    {(root ? "public" : "internal")} sealed class {typeName} : {typeSymbol.Name}
        //    {{
        //        private readonly {agentDescriptor.AgentName} _agent;
        //        private readonly string _path;

        //        {(root
        //            ? $@"
        //        public {typeName}()
        //        {{
        //            _agent = new {agentDescriptor.AgentName}();
        //            _path = _agent._httpClient.BaseAddress.AbsolutePath;
        //        }}"
        //            : $@"
        //        internal {typeName}({agentDescriptor.AgentName} agent, string path)
        //        {{
        //            _path = path;
        //            _agent = agent;
        //        }}")}{generateMembers(typeSymbol.GetMembers(), typeSymbol as INamedTypeSymbol, getInterfacesMethods())}
        //    }}";
        //            }

        //            string generateMembers(ImmutableArray<ISymbol> members, INamedTypeSymbol typeSymbol, IEnumerable<string> interfacesMethods)
        //            {
        //                var declaration = typeSymbol.DeclaringSyntaxReferences;
        //                var properties = from member in members
        //                                 where member.Kind != SymbolKind.Event && IsProperty(member)
        //                                 select member switch
        //                                 {
        //                                     IPropertySymbol propertySymbol => GenerateProperty(fileAndContent, usings, propertySymbol, productionContext, agentDescriptor),
        //                                     IMethodSymbol methodSymbol => GenerateMethod(usings, methodSymbol, declaration, typeSymbol, agentDescriptor),
        //                                     _ => $"//Unable to implement member {member.Name}"
        //                                 };

        //                return string.Join(@"", properties.Concat(interfacesMethods));
        //            }

        //            IEnumerable<string> getInterfacesMethods()
        //            {
        //                return (from iface in typeSymbol.AllInterfaces
        //                        let declaration = iface.DeclaringSyntaxReferences
        //                        from method in iface.GetMembers()
        //                        let m = method as IMethodSymbol
        //                        where m is { } && IsProperty(m)
        //                        select GenerateMethod(usings, m, declaration, iface, agentDescriptor)).ToImmutableArray();
        //            }

        //            void buildFileContent()
        //            {
        //                result = $@"{string.Join("\n", usings.Select(u => $"using {u};"))}

        //namespace {containingNameSpace}
        //{{
        //    {result.Replace("___HELPER_METHODS___", string.Join(@",
        //        ", agentDescriptor.HelperMethods.Values))}
        //}}";
        //            }

        //            #endregion

        //        }
        //        catch (Exception e)
        //        {
        //            fileAndContent.Add($"{typeName}.g.cs", e.ToString());
        //        }
    }

    private static void GetClientBasicConfig(AttributeData attributeData, out string baseUrl, out Casing pathCasing, out Casing queryCasing, out Casing propertyCasing)
    {
        baseUrl = attributeData.ConstructorArguments[0].Value.ToString();
        while (baseUrl.EndsWith("/"))
            baseUrl = baseUrl[..^1];

        (pathCasing, queryCasing, propertyCasing) =
            attributeData.NamedArguments.ToDictionary(kv => kv.Key, kv => kv.Value) is { Count: > 0 } dic
            ? (
                dic.TryGetValue("PathCasing", out var _pathCasing) ? (Casing)_pathCasing.Value : Casing.None,
                dic.TryGetValue("QueryCasing", out var _queryCasing) ? (Casing)_queryCasing.Value : Casing.None,
                dic.TryGetValue("PropertyCasing", out var _propertyCasing) ? (Casing)_propertyCasing.Value : Casing.None
            )
            : (Casing.None, Casing.None, Casing.None);
    }

    private static bool IsProperty(ISymbol member, out IPropertySymbol property)
    {
        return null != (
            property = member is IPropertySymbol { Kind: SymbolKind.Property, Name: { } name } prop &&
            name.StartsWith("get_") && !name.StartsWith("set_")
                ? prop
                : null);
    }

    private static string GenerateMethod(HashSet<string> usings, IMethodSymbol methodSymbol, ImmutableArray<SyntaxReference> declaration, INamedTypeSymbol containingType, AgentDescriptior agentDescriptor)
    {
        (string returnType2, string responseType2, bool isAsync2) = methodSymbol.ReturnType as INamedTypeSymbol switch
        {
            { Name: "Task", TypeArguments: [INamedTypeSymbol { Name: [.. var _responseType, 'R', 'e', 's', 'p', 'o', 'n', 's', 'e'], TypeArguments: [var _type] }] } =>
                (GetType(usings, _type), _responseType, true),
            { Name: [.. var _responseType, 'R', 'e', 's', 'p', 'o', 'n', 's', 'e'] } type =>
                (GetType(usings, type), _responseType, true),
            { Name: { } name } type =>
                (GetType(usings, type), name is "void" or "Task" ? null : name, true),
        };

        if (responseType2 == "Json" && !agentDescriptor.NeedsJsonOptions) agentDescriptor.NeedsJsonOptions = true;

        string
            body = "",
            pathStr = "_path",
            cancelTokenParam = null,
            content = null,
            contentType = null,
            parameters = string.Join(", ", methodSymbol.Parameters.Select(getParameterDefinition)),
            originalReturnType = GetType(usings, methodSymbol.ReturnType),
            method = methodSymbol.Name.Replace("Async", "");

        body += $@"var request = new HttpRequestMessage(HttpMethod.{method}, new Uri({pathStr}, UriKind.Relative))";

        cancelTokenParam ??= "default";

        if (content != null)
            body += $@" {{
                Content = {content}
            }}";

        body += @";
            
            ";

        bool isNotVoid = originalReturnType != "Task" || originalReturnType.ToLowerInvariant() != "void";

        if (isNotVoid)
            body += "var response = ";

        body += $@"await _agent._httpClient.SendAsync(request, {cancelTokenParam});";

        if (isNotVoid)
        {
            if (responseType2 != null)
                usings.Add($"System.Net.Http.{responseType2}");
            var options = responseType2 == "Json" ? "_agent._jsonOptions, " : "";
            body += $@"

            return response switch 
            {{
                {{ IsSuccessStatusCode: true, Content: {{}} responseContent }} => 
                    await responseContent.ReadFrom{responseType2}Async<{returnType2}>({options}{cancelTokenParam}),

                {{ IsSuccessStatusCode: false }} => 
                    throw new HttpRequestException(response.ReasonPhrase),

                _ => default({returnType2})
            }};";
        }

        return $@"

        /*{declaration.Length}*/
        public {(isAsync2 ? "async " : "")}{originalReturnType} {methodSymbol.Name}({parameters})
        {{
            {body}
        }}";

        string getParameterDefinition(IParameterSymbol p)
        {
            if (tryGetPart(out var isQuery, out var isJson, out var partType, out var partTypeName, out var partName))
            {
                if (isQuery)
                {
                    var queryStr = $"var path = ";
                    if (partType.IsTupleType)
                    {
                        if (partType.TupleElements is [
                            { Type: { IsValueType: true }, Name: { } propName, IsExplicitlyNamedTupleElement: true },
                            { Type: { SpecialType: SpecialType.System_Object }, Name: "_" }
                        ])
                            queryStr += $@"$""{{_path}}?{agentDescriptor.QueryCasingFn(propName)}={{query.{propName}}}"";";

                    }
                    else if (partType.IsValueType)
                        queryStr += $@"$""{{_path}}?{agentDescriptor.QueryCasingFn(partName)}={{{partName}}}"";";
                    else
                    {
                        queryStr += $@"$""{{_path}}{{_agent.BuildQuery({partName})}}"";";
                        registerQueryBuilder(partType, partTypeName);
                    }
                    body += queryStr + "\n\t\t\t";
                    pathStr = "path";
                }
                else
                {
                    content = $"{responseType2}Content.Create(({partTypeName}){partName}";
                    if (isJson)
                    {
                        if (!agentDescriptor.NeedsJsonOptions) agentDescriptor.NeedsJsonOptions = true;
                        content += ", options: _agent._jsonOptions";
                    }
                    content += ")";
                }
                contentType ??= isJson ? "Json" : "Xml";
            }
            else if (p.Type.Name == "CancellationToken")
                cancelTokenParam = p.Name;
            string typeName = GetType(usings, p.Type);

            return $"{typeName} {p.Name}{(p.HasExplicitDefaultValue ? getExplicitDefaultValue(p, typeName) : "")}";

            bool tryGetPart(out bool isQuery, out bool isJson, out INamedTypeSymbol partType, out string partTypeName, out string name)
            {
                partType = null;
                name = partTypeName = null;
                isJson = isQuery = false;

                if (p is
                    {
                        Type: INamedTypeSymbol
                        {
                            ContainingNamespace: { Name: "Generator", ContainingNamespace.Name: "HttPie" } ns,
                            Name: { } _typeName and ("Query" or [.., 'C', 'o', 'n', 't', 'e', 'n', 't']),
                            TypeArguments: [INamedTypeSymbol _type]
                        },
                        Name: { } _name
                    })
                {
                    partType = _type;
                    partTypeName = GetType(usings, _type);
                    name = _name;
                    isJson = _typeName == "JsonContent";
                    isQuery = _typeName == "Query";
                    return true;
                }
                return false;
            }
        }

        string getExplicitDefaultValue(IParameterSymbol p, string typeName)
        {
            return $" = {p.ExplicitDefaultValue ?? agentDescriptor.SegmentFallback ?? "default"}";
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
                   select buildQueryProperty(prop)))}
                
                if(result.StartsWith(""?&""))
                    return result.Remove(1);
            }}

            return """";
        }}";

            agentDescriptor.HelperMethods[signature] = method;

            string buildQueryProperty(IPropertySymbol prop)
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

    private static string GetType(HashSet<string> usings, ITypeSymbol type)
    {
        var result = IsPrimitive(type as INamedTypeSymbol) ? type.ToString() : type.Name;

        if (type.ContainingNamespace.ToString() is { } nmspace and not "System")
            RegisterNamespace(usings, nmspace);

        if (type is INamedTypeSymbol { TypeArguments: { Length: > 0 } generics })
        {
            result += $"<{string.Join(", ", generics.Select(g => GetType(usings, g)))}>";
        }
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            result += '?';
        return result;
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

    private static string GenerateProperty(Dictionary<string, string> fileAndContent, HashSet<string> usings, IPropertySymbol prop, SourceProductionContext productionContext, AgentDescriptior agentDescriptor)
    {
        var type = prop.Type;
        string
            implName = $"{prop.Type.Name[1..]}Service",
            implField = $"_{Camelize(implName)}",
            pathStart = @"$""{_path}";

        if (prop.IsIndexer)
        {
            getPropertyParameters(out var paramsDefinition, out var paramsSegments);

            CollectAndBuildRelatedTypes(fileAndContent, productionContext, type, implName, agentDescriptor, false);
            return $@"
        
        public {GetType(usings, type)} this[{paramsDefinition}] => new {implName}(_agent, {pathStart}{paramsSegments}"");";
        }

        CollectAndBuildRelatedTypes(fileAndContent, productionContext, type, implName, agentDescriptor, false);

        string propName = prop.Name;
        return $@"
        
        private {implName} {implField} = null!;
        public {GetType(usings, type)} {propName} => {implField} ??= new (_agent, {pathStart}/{agentDescriptor.PathCasingFn(propName)}"");";

        void getPropertyParameters(out string paramsDefinition, out string pathSegments)
        {
            string comma = null;
            paramsDefinition = "";
            pathSegments = "";

            foreach (var ip in prop.Parameters)
            {
                string paramName = ip.Name;
                paramsDefinition += $"{comma}{GetType(usings, ip.Type)} {ip.Name}";
                pathSegments += $@"/{{{paramName}}}";
                comma ??= ", ";
            }
        }
    }

    private static string Camelize(string name)
    {
        return char.ToLower(name[0]) + name[1..];
    }
}

internal class NamespaceComparer : IEqualityComparer<string>
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
    public AgentDescriptior(string baseUrl, string agentName, Casing pathCasing, Casing queryCasing, Casing propertyCasing, Func<string, string> pathCasingFn, Func<string, string> queryCasingFn, Func<string, string> propertyCasingFn)
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
