#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace HttPie.Generator;

[Generator]
public class ServiceClientGenerator : IIncrementalGenerator
{
    static volatile object _lock = new();
    static readonly DiagnosticDescriptor propertyDiagnosis = new("SG001", "Interface {0} has not defined properties for posible nested services", "", "Service Source Generator", DiagnosticSeverity.Warning, true);
    static readonly DiagnosticDescriptor methodDiagnosis = new("SG002", "Interface {0} has not defined method to implement", "", "Service Source Generator", DiagnosticSeverity.Warning, true);
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        lock (_lock)
        {
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
    }

    private static void RegisterNamespace(HashSet<string> usings, params string[] namespaces)
    {
        foreach (var ns in namespaces)
            if (ns != "<global namespace>")
                usings.Add(ns);
    }

    private static void CreateRelatedTypeFiles(SourceProductionContext productionContext, SemanticModel semanticModel, AttributeData attr, ITypeSymbol typeSymbol, bool root = false)
    {
        string
            interfaceName = typeSymbol.Name,
            name = interfaceName[1..].Replace("Api", ""),
            clientName = $"{name}Client",
            agentName = $"{name}Agent";
        try
        {

            Dictionary<string, string> fileAndContent = new();

            BuilderOptions builderOptions = new(attr, agentName);

            CollectAndBuildRelatedTypes(
                fileAndContent,
                productionContext,
                semanticModel,
                typeSymbol,
                clientName,
                interfaceName,
                builderOptions,
                root);

            var agentClass = $@"{(builderOptions.NeedsJsonOptions ? $@"using System.Text.Json;
using System.Text.Json.Serialization;" : "")}
using HttPie.Generator;
using HttPie.Policy;
using HttPie.Enums;
using HttPie.Converters;

namespace {typeSymbol.ContainingNamespace.ToDisplayString()}
{{
    internal sealed class {builderOptions.AgentName} 
    {{

        internal HttpClient _httpClient;{(builderOptions.NeedsJsonOptions ? @"
        internal JsonSerializerOptions _jsonOptions;" : "")}
        internal Func<string, string> _pathCasing, _queryCasing, _propertyCasing, _enumQueryCasing, _enumSerializationCasing;

        internal {builderOptions.AgentName}()
        {{
            _httpClient = new HttpClient {{ 
                BaseAddress = new Uri(""{builderOptions.BaseUrl.AbsoluteUri.Replace(builderOptions.BaseUrl.PathAndQuery, "")}"")
            }};
            _pathCasing = CasingPolicy.Create(Casing.{builderOptions.PathCasing}).ConvertName;
            _queryCasing = CasingPolicy.Create(Casing.{builderOptions.QueryCasing}).ConvertName;
            _propertyCasing = CasingPolicy.Create(Casing.{builderOptions.PropertyCasing}).ConvertName;
            _enumQueryCasing = CasingPolicy.Create(Casing.{builderOptions.EnumQueryCasing}).ConvertName;
            _enumSerializationCasing = CasingPolicy.Create(Casing.{builderOptions.EnumSerializationCasing}).ConvertName;{(builderOptions.NeedsJsonOptions ? $@"
            _jsonOptions = new JsonSerializerOptions {{                
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNamingPolicy = CasingPolicy.Create(Casing.{builderOptions.PropertyCasing})
            }};
            _jsonOptions.Converters.Insert(0, new EnumJsonConverter(Casing.{builderOptions.EnumSerializationCasing}));" : "")}
        }}{builderOptions.HelperMethods.Values.Join(@"
                
        ")}

    }}
}}";


            productionContext.AddSource($"{clientName}.{agentName}.g.cs", $@"//<auto generated>
{agentClass}");

            foreach (var kv in fileAndContent)
            {
                string code = $@"//<auto generated>
using static HttPie.Generator.HttPieHelpers;
{kv.Value}";
                productionContext.AddSource($"{clientName}.{kv.Key}.g.cs", code);
            }
        }
        catch (Exception e)
        {
            productionContext.AddSource($"{clientName}.{agentName}.g.cs", $"/*{e}*/");
        }

    }

    private static void CollectAndBuildRelatedTypes(Dictionary<string, string> fileAndContent, SourceProductionContext productionContext, SemanticModel semanticModel, ITypeSymbol typeSymbol, string typeName, string interfaceName, BuilderOptions builderOptions, bool root)
    {
        string absolutePath = builderOptions.BaseUrl.AbsolutePath;

        if (absolutePath.EndsWith("/"))
            absolutePath = absolutePath[..^1];

        var ctor = $@"public {typeName}(){{
            _agent = new();
            _path = ""{absolutePath}"";            
        }}";

        fileAndContent[typeName] = createType(typeSymbol, typeName, interfaceName, ctor, true);

        string createType(ITypeSymbol typeSymbol, string typeName, string underlyingInterface, string ctor, bool root = false)
        {
            var containingNameSpace = typeSymbol.ContainingNamespace.ToDisplayString();
            var usings = new HashSet<string>() { };

            try
            {
                string classType = $@"namespace {containingNameSpace}
{{
    public class {typeName} : {underlyingInterface} 
    {{
        private readonly {builderOptions.AgentName} _agent;
        private readonly string _path;
        
        {ctor}{getMembers().Join()}
    }}
}}";
                return $@"{usings.Union(new[] { "HttPie.Generator" }).Except(new[] { containingNameSpace }).Join(u => $"using {u};", @"
")}

{classType}";
            }
            catch (Exception e)
            {
                return $"/*{e}*/";
            }

            IEnumerable<string> getMembers()
            {
                //int propsCount = 0, methodsCount = -1;

                if (typeSymbol.GetMembers().OfType<IPropertySymbol>() is { } props)
                    foreach (var prop in props)
                        yield return buildPropertyAndTypes(prop.Name, prop.Type, prop.IsIndexer, prop.Parameters, GetType(usings, prop.Type), usings);


                if (BuildMethods(typeSymbol, semanticModel, builderOptions, usings).ToImmutableArray() is { Length: int mLen and > 0 } methods)
                    foreach (var methodStr in methods)
                        yield return methodStr;

                //if (typeSymbol.Locations.FirstOrDefault() is { } firstLocation)
                //{
                //    if (propsCount == 0)
                //        productionContext.ReportDiagnostic(
                //                Diagnostic.Create(
                //                       propertyDiagnosis,
                //                       firstLocation));

                //    if (methodsCount == 0)
                //        productionContext.ReportDiagnostic(
                //                Diagnostic.Create(
                //                       methodDiagnosis,
                //                       firstLocation));
                //}
            }

        }

        string buildPropertyAndTypes(string propName, ITypeSymbol type, bool isIndexer, ImmutableArray<IParameterSymbol> parameters, string interfaceName, HashSet<string> usings)
        {
            string
                serviceName = $"{interfaceName[1..]}Service",
                implField = $"_{char.ToLower(serviceName[0]) + serviceName[1..]}";
            var ctor = $@"internal {serviceName}({builderOptions.AgentName} agent, string path)
        {{
            _agent = agent;
            _path = path;            
        }}";

            fileAndContent[serviceName] = createType(type, serviceName, interfaceName, ctor, true);

            return isIndexer
                ? $@"

        public {interfaceName} this[{getPropertyParameters(out var paramsSegments)}] => new {serviceName}(_agent, $""{{_path}}{paramsSegments}"");"
                : $@"

        private {serviceName} {implField} = null!;
        public {interfaceName} {propName} => {implField} ??= new {serviceName}(_agent, $""{{_path}}/{builderOptions.PathCasingFn(propName)}"");";

            string getPropertyParameters(out string pathSegments)
            {
                string? comma = null;
                var paramsDefinition = "";
                pathSegments = "";

                foreach (var ip in parameters)
                {
                    string paramName = ip.Name;
                    paramsDefinition += $"{comma}{GetType(usings, ip.Type)} {ip.Name}";
                    pathSegments += $@"/{{{paramName}}}";
                    comma ??= ", ";
                }

                return paramsDefinition;
            }
        }
    }

    private static IEnumerable<string> BuildMethods(ITypeSymbol cls, SemanticModel semanticModel, BuilderOptions builderOptions, HashSet<string> usings)
    {
        List<string> methods = new();
        ForEach(cls.DeclaringSyntaxReferences.AsSpan(), sr =>
        {
            if (sr.GetSyntax() is InterfaceDeclarationSyntax { BaseList: { ColonToken.SpanStart: int startPoint, Types: var baseTypes } baseList } iFace)
            {
                Queue<SyntaxTrivia> comments = new(iFace.DescendantTrivia(baseList.FullSpan).Where(c => c.IsKind(SyntaxKind.SingleLineCommentTrivia)));

                ForEach(baseTypes.ToImmutableArray().AsSpan(), baseTypeDecl =>
                {
                    if (baseTypeDecl.Type is GenericNameSyntax { TypeArgumentList: { LessThanToken: var ltk, Arguments: { Count: int argsCount and > 0 } args } } type)
                    {
                        INamedTypeSymbol paramType = (INamedTypeSymbol)semanticModel.GetTypeInfo(type).Type!;

                        OperationDescriptor opDescriptor = new(builderOptions);

                        for (int i = 0; i < argsCount; i++)

                            switch (paramType.TypeParameters[i].Name)
                            {
                                case "TResponse":
                                    opDescriptor.ResponseType = (INamedTypeSymbol)paramType.TypeArguments[i];
                                    break;
                                case "TQuery":
                                    opDescriptor.QueryType = (INamedTypeSymbol)paramType.TypeArguments[i];
                                    break;
                                case "TContent":
                                    opDescriptor.ContentType = (INamedTypeSymbol)paramType.TypeArguments[i];
                                    break;
                            }

                        int endPoint = type.SpanStart;

                        GetComment(comments, startPoint, endPoint, opDescriptor);

                        startPoint = type.Span.End;

                        if (opDescriptor is { ContentFormatType: "Json" } or { ResponseFormatType: "Json" })
                            builderOptions.NeedsJsonOptions |= true;

                        if (opDescriptor.ContentFormatType is "Json" or "Xml")
                            usings.Add($"System.Net.Http.{opDescriptor.ContentFormatType}");

                        if (opDescriptor.ResponseFormatType is "Json" or "Xml")
                            usings.Add($"System.Net.Http.{opDescriptor.ContentFormatType}");


                        foreach (var method in paramType.GetMembers().OfType<IMethodSymbol>())
                        {
                            if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                            {
                                methods.Add(
                                    GenerateMethod(
                                        usings,
                                        builderOptions,
                                        method,
                                        opDescriptor));
                            }
                        }
                    }
                });
            }
        });
        return methods;
    }

    private static void GetComment(Queue<SyntaxTrivia> comments, int startPoint, int endPoint, OperationDescriptor desc)
    {
        SyntaxTrivia comm = comments.FirstOrDefault();

        if (!TextSpan.FromBounds(startPoint, endPoint).Contains(comm.FullSpan)) 
            return;

        ForEach<string[]>(GetMetadata(comm).AsSpan(), parts =>
        {
            (string key, string value) = (parts[0].Trim(), parts[1].Trim());
            switch (key)
            {
                case "contentType":
                    desc.ContentFormatType = value;
                    break;
                case "responseType":
                    desc.ResponseFormatType = value;
                    break;
                case "queryParamName":
                    desc.QueryParameterName = value;
                    break;
                case "contentParamName":
                    desc.ContentParameterName = value;
                    break;
                default:
                    desc.Headers.Add((key, value));
                    break;
            }
        });

        comments.Dequeue();

        static string[][] GetMetadata(SyntaxTrivia comm)
        {
            return comm
                .ToString()
                .Trim('*', '/', '\n', '\n', '\t', ' ')
                .Split(',')
                .Where(x => x.Trim() is not { Length: 0 })
                .Select(x => x.Split(':'))
                .ToArray();
        }
    }

    private static string[] GetHeaderTuple(JsonElement r)
    {
        var jsonElements = r.EnumerateArray().ToArray();
        return new[] { jsonElements[0].GetString()!, jsonElements[1].GetString()! };
    }

    static void ForEach<T>(ReadOnlySpan<T> data, Action<T> action)
    {
        ref var searchSpace = ref MemoryMarshal.GetReference(data);
        for (int i = 0, length = data.Length; i < length; i++)
        {
            action(Unsafe.Add(ref searchSpace, i));
        }
    }

    const string defaultCancelToken = "default";

    private static string GenerateMethod(HashSet<string> usings, BuilderOptions builderOptions, IMethodSymbol method, OperationDescriptor contentDesc)
    {
        string name = method.Name,
            methodType = name[..^5],
            pathVar = "_path",
            cancelToken = defaultCancelToken,
            parameters = buildParameters(out string? queryReference, out string? contentRefernce);

        return @$"

        public async Task{buildReturnType(out var returnType, out var responseHandler)} {name}({parameters})
        {{
            {buildRequestSubmission()}{responseHandler}
        }}";

        string? buildReturnType(out string? returnTypeName, out string? responseHandler)
        {
            if (contentDesc is { ResponseType: { } returnType, ResponseFormatType: { } returnFormatType })
            {
                var options = returnFormatType == "Json" ? "_agent._jsonOptions" : "";

                responseHandler = $@"

            return response switch 
            {{
                {{ IsSuccessStatusCode: true, Content: {{}} responseContent }} => 
                    await responseContent.ReadFrom{returnFormatType}Async<{returnTypeName = GetType(usings, returnType)}>({Concat(options, cancelToken)}),

                {{ IsSuccessStatusCode: false }} => 
                    throw new HttpRequestException(response.ReasonPhrase),

                _ => default({returnTypeName})
            }};";
                return $"<{returnTypeName}>";
            }
            return responseHandler = returnTypeName = null;
        }

        string buildParameters(out string? queryReference, out string? contentRefernce)
        {
            contentRefernce = queryReference = null;
            var paramSkip = 0;
            var parametersSyntax = "";

            if (contentDesc is { QueryType: { } queryType, QueryParameterName: { } queryParameterName })
            {
                paramSkip++;

                var queryTypeName = GetType(usings, queryType);

                parametersSyntax += $@"{queryTypeName} {queryParameterName}";
                queryReference = buildQueryParams(out pathVar);
            }

            if (contentDesc is { ContentFormatType: { } contentDocType, ContentParameterName: { } contentParamName, ContentType: INamedTypeSymbol contentType })
            {
                paramSkip++;

                if (paramSkip > 1)
                    parametersSyntax += ", ";

                var contentHeaders = "";

                var contentTypeName = GetType(usings, contentType);

                var isFile = contentTypeName.Contains("FileInfo");

                if (isFile) 
                    contentDocType = "MultipartFormData";

                var content = contentDocType switch
                {
                    "MultipartFormData" => $"{nameof(HttPieHelpers.ArrayFrom)}({BuildMultipartItems(usings, builderOptions, contentType, isFile, contentHeaders, contentParamName)})",

                    "FormUrlEncoded" => $@"new Dictionary<string, string> {{
                    {BuildFormUrlEncodedItems(usings, builderOptions, contentType, contentHeaders)}
                }}",

                    _ => contentParamName
                };

                parametersSyntax += $@"{contentTypeName} {contentParamName}";

                contentRefernce = $@" {{
                Content = HttPieHelpers.Create{contentDocType}({content}{(contentDocType == "Json" ? ", _agent._jsonOptions" : "")})
            }}";
            }

            foreach (var param in method.Parameters.Skip(paramSkip))
            {
                if (paramSkip++ > 0)
                    parametersSyntax += ", ";

                string
                    paramType = GetType(usings, param.Type),
                    paramName = param.Name;

                if (cancelToken == defaultCancelToken && paramType == "CancellationToken")
                    cancelToken = paramName;

                parametersSyntax += $"{paramType} {paramName}";

                if (param.HasExplicitDefaultValue)
                    parametersSyntax += $" = {param.ExplicitDefaultValue ?? builderOptions.SegmentFallback ?? "default"}";
            }
            return parametersSyntax;
        }

        string buildRequestSubmission()
        {
            var sendPrefix = "";
            if (returnType != null)
                sendPrefix = "var response = ";

            return $@"{queryReference}var request = new HttpRequestMessage(HttpMethod.{methodType}, new Uri({pathVar}, UriKind.Relative)){contentRefernce};
            {sendPrefix}await _agent._httpClient.SendAsync(request, {cancelToken});";
        }

        string buildQueryParams(out string pathVar)
        {
            pathVar = "_path";

            if (contentDesc is { QueryType: { } queryType, QueryParameterName: { } queryParameterName })
            {
                var requestSyntax = "";

                pathVar = "path";
                requestSyntax += $@"var path = $""{{_path}}";

                if (queryType.IsValueType)
                    requestSyntax += $@"?{builderOptions.QueryCasingFn(queryParameterName)}={{_agent._enumQueryCasing({queryParameterName}.ToString())}}";
                else
                {
                    requestSyntax += $@"?{{_agent.BuildQuery({queryParameterName})}}";
                    registerQueryBuilder(queryParameterName, queryType);
                }
                requestSyntax += @""";
            ";

                return requestSyntax;
            }

            return "";
        }

        string registerQueryBuilder(string queryTypeName, INamedTypeSymbol queryType)
        {
            var signature = $@"string BuildQuery({queryTypeName} query)";

            return builderOptions.HelperMethods[signature] = $@"

        internal {signature}
        {{
            if(query != null)
            {{
                var result = ""?"";

                {(queryTypeName == "Object"
                        ? $@"foreach(var prop in typeof({{{queryTypeName}}}).GetProperties())
                    if(prop.GetValue(query) is {{}} value)
                        result += $""&{{_queryCasing(prop.Name)}}={{prop.Type.IsEnum ? _enumQueryCasing(value.ToString()) : value}}"";"
                        : getQueryPropertyBuilder(builderOptions, queryType))}
                
                if(result.StartsWith(""?&""))
                    return result.Remove(1);
            }}

            return """";
        }}";
        }

        string getQueryPropertyBuilder(BuilderOptions builderOptions, ITypeSymbol queryType)
        {
            return (from member in queryType.GetMembers()
                    let prop = member as IPropertySymbol
                    where prop is { IsIndexer: false }
                    select buildQueryProperty(prop)).Join(@"
                ");
        }

        string buildQueryProperty(IPropertySymbol prop)
        {
            var value = $"query.{prop.Name}";

            if (((INamedTypeSymbol)prop.Type).EnumUnderlyingType != null)
                value = $"_agent.enumCasing({value})";

            var result = $@"result += ""&{builderOptions.QueryCasingFn(prop.Name)}={{{value}}}"";";

            if (prop.Type is { IsReferenceType: true } or { SpecialType: SpecialType.System_String })
                result += $@"

                    if(query.{prop.Name} != null)
                        {result}";

            return result;
        }
    }

    private static string Concat(params string[] values)
    {
        return values.Where(e => e is { Length: > 0 }).Join(", ");
    }

    private static string BuildMultipartItems(HashSet<string> usings, BuilderOptions builderOptions, INamedTypeSymbol contentType, bool isFile, string headers, string contentParamName = "content")
    {
        if (isFile)
            return @$"({contentParamName}.ToByteArrayContent(), ""{contentParamName}"", {contentParamName}.Name)";

        return contentType switch
        {
            { IsValueType: true } =>
                @$"({BuildHttpContent(usings, builderOptions, contentType, isFile, headers)}, ""{contentParamName}"", null)",

            { IsTupleType: true, TupleElements: { } els } =>
                els
                .Select(el => BuildMultiPartTuple(usings, builderOptions, (INamedTypeSymbol)el.Type, el.IsExplicitlyNamedTupleElement ? $@"""{el.Name}""" : "null", headers, isFile))
                .Join(", "),

            _ => contentType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsIndexer)
                .Select(p => BuildMultiPartTuple(usings, builderOptions, (INamedTypeSymbol)p.Type, $@"""{p.Name}""", headers, isFile))
                .Join(", ")
        };
    }

    private static string BuildFormUrlEncodedItems(HashSet<string> usings, BuilderOptions builderOptions, INamedTypeSymbol contentType, string headers)
    {
        return contentType switch
        {
            { NullableAnnotation: { } nullability, IsValueType: true } =>
                @$"{{ ""content"": content{(nullability == NullableAnnotation.Annotated ? "?" : "")}.ToString() }}",

            { NullableAnnotation: { } nullability, IsTupleType: true, TupleElements: { } els } =>
                els
                .Select(el => @$"{{ ""{builderOptions.QueryCasingFn(el.Name)}"": content{(nullability == NullableAnnotation.Annotated ? "?" : "")}.{{{$"{el.Name}{(el.Type.IsValueType ? "" : "?")}"}}}.ToString() }}")
                .Join(@", 
                        "),

            _ => contentType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsIndexer)
                .Select(p => BuildMultiPartTuple(usings, builderOptions, (INamedTypeSymbol)p.Type, $@"""{p.Name}""", headers, false)).Join(@", 
                        ")
        };
    }

    private static string BuildMultiPartTuple(HashSet<string> usings, BuilderOptions builderOptions, INamedTypeSymbol type, string fieldName, string headers, bool isFile)
    {
        var typeName = GetType(usings, type);
        return $@"({BuildHttpContent(usings, builderOptions, type, isFile, headers)}, {fieldName}, {isFile}{(isFile ? ", content.Name" : "")})";
    }

    private static string BuildHttpContent(HashSet<string> usings, BuilderOptions builderOptions, INamedTypeSymbol contentType, bool isFile, string headers)
    {
        return isFile ? "content.ToByteArrayContent()" : contentType switch
        {
            _ => $@"CreateFormUrlEncoded(new Dictionary<string, string> {{
                        {BuildFormUrlEncodedItems(usings, builderOptions, contentType, headers)}
                    }}"
        };
    }

    private static string GetType(HashSet<string> usings, ITypeSymbol type, out string name)
    {
        return name = GetType(usings, type);
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
                => IsPrimitive((INamedTypeSymbol)type) ? type.ToDisplayString() : type.Name
        };
    }

    private static bool IsPrimitive(INamedTypeSymbol type)
    {
        return type?.SpecialType switch
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
