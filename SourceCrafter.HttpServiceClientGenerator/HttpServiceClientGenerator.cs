#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using SourceCrafter.HttpServiceClient.Internals;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient;
using System.IO;
using System.Reflection;

[assembly: InternalsVisibleTo("SourceCrafter.HttpServiceClientGenerator.UnitTests")]

namespace SourceCrafter;

[Generator]
internal class HttpServiceClientGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor PropertyDiagnosis = new("SG001", "Interface {0} has not defined properties for posible nested services", "", "Service Source Generator", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor MethodDiagnosis = new("SG002", "Interface {0} has not defined method to implement", "", "Service Source Generator", DiagnosticSeverity.Warning, true);
    private readonly static object _lock = new();
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        lock (_lock)
        {
            var interfaceDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "SourceCrafter.HttpServiceClient.Attributes.HttpOptionsAttribute",
                    static (n, _) => n is InterfaceDeclarationSyntax,
                    static (ctx, c) => (Attr: ctx.Attributes[0], semanticModel: ctx.SemanticModel, type: (ITypeSymbol)ctx.TargetSymbol)
                );

            context.RegisterSourceOutput(
                interfaceDeclarations,
                static (sourceProducer, gen) => CreateRelatedTypeFiles(sourceProducer, gen.semanticModel, gen.Attr, gen.type)
            );
        }
    }

    private static void RegisterNamespace(HashSet<string> usings, params string[] namespaces)
    {
        foreach (var ns in namespaces)
            if (ns != "<global namespace>")
                usings.Add(ns);
    }

    private static void CreateRelatedTypeFiles(SourceProductionContext productionContext, SemanticModel semanticModel, AttributeData attr, ITypeSymbol typeSymbol)
    {
        string
            interfaceName = typeSymbol.Name,
            name = interfaceName[1..].Replace("Api", ""),
            clientName = $"{name}Client",
            agentName = $"{name}Agent";
        
        AgentOptions agentOptions = new(attr, typeSymbol.ContainingNamespace.ToDisplayString(), agentName);
        
        try
        {

            HashSet<string> fileNames = new();

            var ctor = $@"public {clientName}(){{
            _agent = new();
            _path = ""{agentOptions.BaseUrlAbsolutePath}"";            
        }}";

            CreateType(
                agentOptions,
                semanticModel,
                $"{agentOptions.AgentNamespace}.{clientName}.http.cs".TrimStart('.'),
                fileNames,
                typeSymbol,
                clientName,
                interfaceName,
                ctor,
                Array.Empty<PathSegment>(),
                productionContext.AddSource);

            var agentClass = $@"namespace {agentOptions.AgentNamespace}
{{
    internal sealed class {agentOptions.AgentTypeName} 
    {{
        internal System.Collections.Generic.Dictionary<string, object> DefaultQueryValues = new();
        internal System.Func<System.Net.Http.HttpRequestMessage, System.Threading.Tasks.Task>? DefaultRequestHandler;
        internal System.Func<System.Net.Http.HttpResponseMessage, System.Threading.Tasks.Task>? DefaultResponseHandler;
        internal System.Net.Http.HttpClient _httpClient;";

            if (agentOptions.NeedsJsonOptions)
                agentClass += @"
        internal System.Text.Json.JsonSerializerOptions _jsonOptions;";

            agentClass += $@"
        internal {agentOptions.AgentTypeName}()
        {{
            _httpClient = new () {{ 
                BaseAddress = new System.Uri(""{agentOptions.BaseUrl.AbsoluteUri.Replace(agentOptions.BaseUrl.PathAndQuery, "")}"")
            }};{(agentOptions.NeedsJsonOptions ? $@"
            _jsonOptions = new System.Text.Json.JsonSerializerOptions {{                
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles{GenerateCaseConverter(agentOptions, "PropertyNamingPolicy")}                
            }};
            _jsonOptions.Converters.Insert(0, new SourceCrafter.HttpServiceClient.Converters.EnumJsonConverter(SourceCrafter.HttpServiceClient.Enums.Casing.{agentOptions.EnumSerializationCasing}));" : ";")}
        }}{agentOptions.HelperMethods.Values.Join(@"
                
        ")}

        internal async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancelToken) {{
            if (DefaultRequestHandler != null)
                await DefaultRequestHandler(request);

            var response = await _httpClient.SendAsync(request, cancelToken);

            if (DefaultResponseHandler != null)
                await DefaultResponseHandler(response);

            return response;
        }}

        internal string GetUrl(string baseUrl) {{
            var append = string.Join(""&"", DefaultQueryValues.Select(kv => kv.Key + ""="" + System.Uri.EscapeDataString($""{{kv.Value}}"")));
            if(!baseUrl.Contains(""?"") && append.Length > 0)
                baseUrl += ""?"";
            return baseUrl + append;
        }}
    }}
}}";


            productionContext.AddSource($"{agentOptions.AgentFullTypeName}.http.cs", $@"//<auto generated>
{agentClass}");
        }
        catch (Exception e)
        {
            productionContext.AddSource($"{agentOptions.AgentFullTypeName}.http.cs", $"/*{e}*/");
        }

    }

    internal static string GenerateCaseConverter(AgentOptions agentOptions, string prop, string? fallBackConverter = "value => value")
    {
        return agentOptions.PropertyCasing != Casing.None
                            ? $@",
                {prop} = SourceCrafter.HttpServiceClient.Policies.CasingPolicy.Create(SourceCrafter.HttpServiceClient.Enums.Casing.{agentOptions.PropertyCasing})"
                            : fallBackConverter != null
                                ? $@",
                {prop} = {fallBackConverter}"
                                : "";
    }

    internal static void CreateType(
        AgentOptions agentOptions,
        SemanticModel semanticModel,
        string fileName,
        HashSet<string> fileNames,
        ITypeSymbol typeSymbol,
        string typeName,
        string underlyingInterface,
        string ctor,
        IEnumerable<PathSegment> dynamicalSegments,
        Action<string, string> createFile)
    {
        var containingNameSpace = typeSymbol.ContainingNamespace.ToDisplayString();
        HashSet<string> usings = new();
        try
        {
            var classType = $@"namespace {containingNameSpace}
{{
    public sealed partial class {typeName} : {underlyingInterface} 
    {{
        private readonly {agentOptions.AgentFullTypeName} _agent;
        private readonly string _path;
        internal System.Collections.Generic.Dictionary<string, object> DefaultQueryValues => _agent.DefaultQueryValues;
        internal System.Net.Http.Headers.HttpRequestHeaders DefaultRequestHeaders => _agent._httpClient.DefaultRequestHeaders;";

            classType += $@"        
        {ctor}{GetMembers(dynamicalSegments).Join()}
    }}
}}";
            createFile(fileName, $@"{usings
                .Append("static SourceCrafter.HttpServiceClient.HttpHelpers")
                .Except(new[] { containingNameSpace })
                .Join(u => $"using {u};", @"
")}

{classType}");
        }
        catch (Exception e)
        {
            createFile(fileName, $"/*{e}*/");
        }

        IEnumerable<string> GetMembers(IEnumerable<PathSegment> parentIndexersParams)
        {
            //int propsCount = 0, methodsCount = -1;

            if (typeSymbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray() is { } props)
                foreach (var prop in props)
                    yield return BuildPropertyAndTypes(
                        agentOptions,
                        semanticModel,
                        fileNames,
                        prop.Name,
                        prop.GetAttributes()
                            .FirstOrDefault(FindSegmentAttr) is { ConstructorArguments: [{ Value: string value }] }
                                ? value
                                : null,
                        prop.Type,
                        prop.IsIndexer,
                        parentIndexersParams,
                        prop.Parameters,
                        GetSimpleTypeName(prop.Type),
                        createFile);

            if (BuildMethods(typeSymbol, semanticModel, agentOptions, usings).ToImmutableArray() is { Length: > 0 } methods)
                foreach (var methodStr in methods)
                    yield return methodStr;
        }

    }

    private static string BuildPropertyAndTypes(
        AgentOptions agentOptions,
        SemanticModel semanticModel,
        HashSet<string> fileNames,
        string propName,
        string? segment,
        ITypeSymbol type,
        bool isIndexer,
        IEnumerable<PathSegment> segments,
        ImmutableArray<IParameterSymbol> parameters,
        string interfaceName,
        Action<string, string> createFile)
    {
        string
            nameSpace= type.ContainingNamespace.ToString().Replace("<global namespace>",""),
            serviceName = $"{interfaceName[1..]}Service",
            fullName = (nameSpace + "." + serviceName).TrimStart('.'),
            implField = $"_{char.ToLower(serviceName[0]) + serviceName[1..]}";

        var ctor = $@"internal {serviceName}({agentOptions.AgentFullTypeName} agent, string path)
        {{
            _agent = agent;            
            _path = path;        
        }}";


        if (isIndexer)
        {
            GetPropertyParameters(out var absolutePath, out var allParamsDef, out var implicitCtorSegments, out var implicitParamsDef);
            
            ctor += $@"
        public {serviceName}({allParamsDef})
        {{
            _agent = new();
            _path = $""{agentOptions.BaseUrlAbsolute}{absolutePath}"";
        }}";

            if (fileNames.Add(serviceName))
                CreateType(
                    agentOptions,
                    semanticModel,
                    fullName + ".http.cs",
                    fileNames,
                    type,
                    serviceName,
                    interfaceName,
                    ctor,
                    segments,
                    createFile);

            return $@"

        public {interfaceName} this[{implicitParamsDef}] => 
            new {fullName}(_agent, $""{{_path}}{implicitCtorSegments}"");";
        
        }
        else
        {
            PathSegment element = new(segment ?? agentOptions.PathCasingFn(propName)!);

            segments = segments.Append(element).ToArray();

            if (fileNames.Add(serviceName))
                CreateType(
                    agentOptions,
                    semanticModel,
                    fullName + ".http.cs",
                    fileNames,
                    type,
                    serviceName,
                    interfaceName,
                    ctor,
                    segments,
                    createFile);

            return $@"

        private {interfaceName} {implField} = null!;

        public {interfaceName} {propName} => 
            {implField} ??= new {fullName}(_agent, $""{{_path}}/{element.Value}"");";
        }

        void GetPropertyParameters(out string absolutePath, out string absoluteDynamicParams, out string contextPathSegments, out string contextParamsDefinition)
        {
            string? comma = null;
            absolutePath = absoluteDynamicParams = contextParamsDefinition = contextPathSegments = "";
            foreach (var segment in segments)
            {
                if (segment.Name != null)
                {
                    absoluteDynamicParams += $"{comma}{segment}";
                    comma ??= ", ";
                }
                absolutePath += segment.Value;

            }

            List<PathSegment> newSegments = new();

            foreach (var ip in parameters)
            {
                var paramName = ip.Name;

                PathSegment pathSegment = new($"/{{{paramName}}}", ip.Name, GetType(ip.Type));

                newSegments.Add(pathSegment);

                absolutePath += pathSegment.Value;
                contextPathSegments += pathSegment.Value;

                var append = $"{comma}{pathSegment}";

                contextParamsDefinition += append;
                absoluteDynamicParams += append;
                comma ??= ", ";
            }

            segments = segments.Concat(newSegments).ToArray();
        }
    }

    private static bool FindSegmentAttr(AttributeData a) => a.AttributeClass is INamedTypeSymbol { Name: nameof(SegmentAttribute) };

    private static IEnumerable<string> BuildMethods(ITypeSymbol cls, SemanticModel semanticModel, AgentOptions agentOptions, HashSet<string> usings)
    {
        List<string> methods = new();

        ForEach(cls.DeclaringSyntaxReferences.AsSpan(), sr =>
        {
            if (sr.GetSyntax() is not InterfaceDeclarationSyntax { BaseList: { ColonToken.SpanStart: var startPoint, Types: var baseTypes } baseList } iFace)
                return;

            Queue<SyntaxTrivia> comments = new(iFace.DescendantTrivia(baseList.FullSpan).Where(c => c.IsKind(SyntaxKind.SingleLineCommentTrivia)));

            ForEach(baseTypes.ToImmutableArray().AsSpan(), baseTypeDecl =>
            {
                if (baseTypeDecl.Type is not GenericNameSyntax { TypeArgumentList: { LessThanToken: var ltk, Arguments: { Count: int argsCount and > 0 } args } } type)
                    return;

                var paramType = (INamedTypeSymbol)semanticModel.GetTypeInfo(type).Type!;

                OperationDescriptor opDescriptor = new(agentOptions);

                for (var i = 0; i < argsCount; i++)

                    switch (paramType.TypeParameters[i])
                    {
                        case { Name: "TResponse" } param:
                            opDescriptor.ResponseType = (INamedTypeSymbol)paramType.TypeArguments[i];
                            break;
                        case { Name: "TQuery" } param:
                            opDescriptor.QueryType = (INamedTypeSymbol)paramType.TypeArguments[i];
                            break;
                        case { Name: "TContent" } param:
                            opDescriptor.BodyType = (INamedTypeSymbol)paramType.TypeArguments[i];
                            break;
                    }

                var endPoint = type.SpanStart;

                GetComment(comments, startPoint, endPoint, opDescriptor);

                startPoint = type.Span.End;

                if (opDescriptor is { BodyFormatType: "Json" } or { ResponseFormatType: "Json" })
                    agentOptions.NeedsJsonOptions |= true;

                if (opDescriptor.BodyFormatType is "Json" or "Xml")
                    usings.Add($"System.Net.Http.{opDescriptor.BodyFormatType}");

                if (opDescriptor.ResponseFormatType is "Json" or "Xml")
                    usings.Add($"System.Net.Http.{opDescriptor.BodyFormatType}");


                foreach (var method in paramType.GetMembers().OfType<IMethodSymbol>())
                {
                    if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                    {
                        methods.Add(
                            GenerateMethod(
                                usings,
                                agentOptions,
                                method,
                                opDescriptor));
                    }
                }

            });
        });
        return methods;
    }

    private static void GetComment(Queue<SyntaxTrivia> comments, int startPoint, int endPoint, OperationDescriptor desc)
    {
        if (comments.Count == 0) return;
        var comm = comments.Peek();

        if (!TextSpan.FromBounds(startPoint, endPoint).Contains(comm.FullSpan))
            return;

        foreach (var tuple in DeserializeComment(comm))
            if (tuple.IndexOf(':') is var index and > 0)
                switch (tuple[..index])
                {
                    case "contentType":
                        desc.BodyFormatType = tuple[(index + 1)..];
                        break;
                    case "responseType":
                        desc.ResponseFormatType = tuple[(index + 1)..];
                        break;
                    case "queryParamName":
                        desc.QueryParamName = tuple[(index + 1)..];
                        break;
                    case "contentParamName":
                        desc.BodyParamName = tuple[(index + 1)..];
                        break;
                    case { } key:
                        desc.Headers.Add((key, tuple[(index + 1)..]));
                        break;
                }

        comments.Dequeue();
    }

    private static Span<string> DeserializeComment(SyntaxTrivia comm) => 
        comm.ToString().Trim('*', '/', '\n', '\r', '\t', ' ').Split(',').AsSpan();

    private static void ForEach<T>(ReadOnlySpan<T> data, Action<T> action)
    {
        ref var searchSpace = ref MemoryMarshal.GetReference(data);
        for (int i = 0, length = data.Length; i < length; i++)
        {
            action(Unsafe.Add(ref searchSpace, i));
        }
    }

    private const string DefaultCancelToken = "default";

    private static string GenerateMethod(HashSet<string> usings, AgentOptions agentOptions, IMethodSymbol method, OperationDescriptor contentDesc)
    {
        string name = method.Name,
            methodType = name[..^5],
            pathVar = "_path",
            cancelToken = DefaultCancelToken,
            parameters = BuildParameters(out var queryReference, out var contentRefernce);
        return @$"

        public async Task{BuildReturnType(out var returnType, out var responseHandler)} {name}({parameters})
        {{
            {BuildRequestSubmission()}{responseHandler}
        }}";

        string? BuildReturnType(out string? returnTypeName, out string? responseHandler)
        {
            if (contentDesc is not { ResponseType: { } returnType, ResponseFormatType: var returnFormatType })
                return responseHandler = returnTypeName = null;

            var options = returnFormatType == "Json" ? $"_agent._jsonOptions" : "";

            responseHandler = $@"

            return response switch 
            {{
                {{ IsSuccessStatusCode: true, Content: {{}} responseContent }} => 
                    await responseContent.ReadFrom{returnFormatType}Async<{returnTypeName = GetType(returnType)}>({Concat(options, cancelToken)}),

                {{ IsSuccessStatusCode: false }} => 
                    throw response.RequestException(),

                _ => default({returnTypeName})
            }};";
            return $"<{returnTypeName}>";

        }

        string BuildParameters(out string? queryReference, out string? contentBuilderSyntax)
        {
            contentBuilderSyntax = queryReference = null;
            var paramSkip = 0;
            var parametersSyntax = "";

            if (contentDesc is { QueryType: { } queryType, QueryParamName: { } queryParameterName })
            {
                paramSkip++;

                var queryTypeName = GetType(queryType);

                parametersSyntax += $@"
                {queryTypeName} {queryParameterName}";
                queryReference = BuildQueryParams(queryTypeName, out pathVar);
            }

            if (contentDesc is { BodyFormatType: { } contentDocType, BodyParamName: { } contentParamName, BodyType: INamedTypeSymbol contentType })
            {
                paramSkip++;

                if (paramSkip > 1)
                    parametersSyntax += ",";

                var contentHeaders = "";

                var contentTypeName = GetType(contentType);

                var isFile = contentTypeName.Contains("FileInfo");

                if (isFile)
                    contentDocType = "MultipartFormData";

                var content = contentDocType switch
                {
                    "MultipartFormData" => $"new[] {{ {BuildMultipartItems(usings, agentOptions, contentType, isFile, contentHeaders, contentParamName)} }}",

                    "FormUrlEncoded" => $@"new System.Collections.Generic.Dictionary<string, string> {{
                    {BuildFormUrlEncodedItems(usings, agentOptions, contentType, contentHeaders)}
                }}",

                    _ => contentParamName
                };

                parametersSyntax += $@"
            {contentTypeName} {contentParamName}";

                contentBuilderSyntax = $@" {{
                Content = {content}.Create{contentDocType}(";

                if (contentDocType == "Json")
                    contentBuilderSyntax += "_agent._jsonOptions";

                contentBuilderSyntax += @")
            }";
            }

            foreach (var param in method.Parameters.Skip(paramSkip))
            {
                if (paramSkip++ > 0)
                    parametersSyntax += ",";

                string
                    paramType = GetType(param.Type),
                    paramName = param.Name;

                if (cancelToken == DefaultCancelToken && paramType == "CancellationToken")
                    cancelToken = paramName;

                parametersSyntax += $@"
            {paramType} {paramName}";

                if (param.HasExplicitDefaultValue)
                    parametersSyntax += $" = {param.ExplicitDefaultValue ?? agentOptions.SegmentFallback ?? "default"}";
            }
            return parametersSyntax;
        }

        string BuildRequestSubmission()
        {

            return $@"{queryReference}var request = new System.Net.Http.HttpRequestMessage(
                System.Net.Http.HttpMethod.{methodType},
                new System.Uri(_agent.GetUrl({pathVar}), System.UriKind.Relative)){contentRefernce};

            if(handleRequest != null)
                await handleRequest(request);

            var response = await _agent.SendAsync(request, {cancelToken});
            
            if(handleResponse != null)
                await handleResponse(response);";

        }

        string BuildQueryParams(string queryTypeName, out string pathVar)
        {
            pathVar = "_path";

            if (contentDesc is { QueryType: { } queryType, QueryParamName: { } queryParameterName })
            {
                var requestSyntax = "";

                pathVar = "path";
                requestSyntax += $@"var path = $""{{_path}}";

                if (queryType is { IsValueType: true, IsTupleType: false })
                {
                    string
                        paramName = agentOptions.QueryPropCasingFn(queryParameterName)!;

                    requestSyntax += $@"?{paramName}={{{queryParameterName}}}";
                }
                else
                {
                    requestSyntax += $@"{{_agent.BuildQuery({queryParameterName})}}";

                    var signature = $@"string BuildQuery({queryTypeName} query)";

                    if (!agentOptions.HelperMethods.ContainsKey(signature))
                        RegisterQueryBuilder(queryType, signature);
                }
                requestSyntax += @""";
            ";

                return requestSyntax;
            }

            return "";
        }

        string RegisterQueryBuilder(ITypeSymbol queryType, string signature)
        {
            return agentOptions.HelperMethods[signature] = $@"

        internal {signature}
        {{
            {BuildQueryBuilderBody(agentOptions, queryType)}
        }}";
        }
    }

    //private static string GetFormatterExpression(string value, ITypeSymbol type, Casing propCasing, bool insideInterpolation = true)
    //{
    //    if (insideInterpolation && (propCasing == Casing.None))
    //        return value;
    //    if (IsNullable(type))
    //        value += '?';
    //    var isEnum = type is INamedTypeSymbol { EnumUnderlyingType: { } };
    //    var isNumberBased = IsNumberBased(type);
    //    if (!isEnum && !isNumberBased && !insideInterpolation && type.SpecialType is not (SpecialType.System_String or SpecialType.System_Object))
    //        value += ".ToString()";

    //    if (!isNumberBased && propCasing != Casing.None)
    //        value += propCasing switch
    //        {
    //            Casing.Digit when isEnum => @".ToString(""D"")",
    //            Casing.CamelCase => $".{nameof(HttpHelpers.ToCamel)}()",
    //            Casing.PascalCase => $".{nameof(HttpHelpers.ToPascal)}()",
    //            Casing.LowerCase => $".ToLower()",
    //            Casing.UpperCase => $".ToUpper()",
    //            Casing.LowerSnakeCase => $".{nameof(HttpHelpers.ToSnakeLower)}()",
    //            Casing.UpperSnakeCase => $".{nameof(HttpHelpers.ToSnakeUpper)}()",
    //            _ => ""
    //        };
        
    //    return value;

    //}

    private static bool IsNumberBased(ITypeSymbol type)
    {
        return type.SpecialType is SpecialType.System_Int16 or 
            SpecialType.System_Int32 or 
            SpecialType.System_Int64 or 
            SpecialType.System_UInt16 or 
            SpecialType.System_UInt32 or 
            SpecialType.System_UInt64 or 
            SpecialType.System_Decimal or 
            SpecialType.System_Single or 
            SpecialType.System_Double;
    }

    private static string BuildQueryBuilderBody(AgentOptions agentOptions, ITypeSymbol queryType)
    {
        var isNullableParam = IsNullable(queryType);
        var items = GetQueryProperties(queryType);
        var body = "return QueryBuilder.With(query)";

        if (isNullableParam) body = $@"if(query == null) return """";

            {body}";

        int indent = isNullableParam ? 4 : 3, itemsLen = items.Length;

        foreach (var (i, memberName, type, isNullable) in items)
        {
            string
                queryParamName = agentOptions.QueryPropCasingFn(memberName)!,
                queryParamValue = $"query.{memberName}";

            body += $@"
                .Add(""{queryParamName}"", {(isNullable ? $"_query => _{queryParamValue}" : queryParamValue)})";

        }

        body += @"
                .ToString();";

        return body;

        static (int i, string, ITypeSymbol Type, bool)[] GetQueryProperties(ITypeSymbol queryType)
        {
            return (queryType is INamedTypeSymbol { IsTupleType: true, TupleElements: { } els }
                        ? els
                            .Select((e, i) => (i, e.IsExplicitlyNamedTupleElement ? e.Name : $"Item{i + 1}", e.Type, IsNullable(e.Type)))
                        : queryType
                            .GetMembers()
                            .Cast<IPropertySymbol>()
                            .Where(e => !e.IsIndexer)
                            .Select((e, i) => (i, e.Name, e.Type, IsNullable(e.Type)))).ToArray();
        }
    }

    private static string Concat(params string[] values)
    {
        return values.Where(e => e is { Length: > 0 }).Join(", ");
    }

    private static string BuildMultipartItems(HashSet<string> usings, AgentOptions agentOptions, INamedTypeSymbol contentType, bool isFile, string headers, string contentParamName = "content")
    {
        if (isFile)
            return @$"({contentParamName}.ToByteArrayContent(), ""{contentParamName}"", {contentParamName}.Name)";

        return contentType switch
        {
            { IsValueType: true } =>
                @$"({BuildHttpContent(usings, agentOptions, contentType, isFile, headers)}, ""{contentParamName}"", null)",

            { IsTupleType: true, TupleElements: { } els } =>
                els
                .Select(el => BuildMultiPartTuple(usings, agentOptions, (INamedTypeSymbol)el.Type, el.IsExplicitlyNamedTupleElement ? $@"""{el.Name}""" : "null", headers, isFile))
                .Join(", "),

            _ => contentType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsIndexer)
                .Select(p => BuildMultiPartTuple(usings, agentOptions, (INamedTypeSymbol)p.Type, $@"""{p.Name}""", headers, isFile))
                .Join(", ")
        };
    }

    private static string BuildFormUrlEncodedItems(HashSet<string> usings, AgentOptions agentOptions, INamedTypeSymbol contentType, string headers)
    {
        return contentType switch
        {
            { NullableAnnotation: { } nullability, IsValueType: true } =>
                @$"{{ ""content"": content{(nullability == NullableAnnotation.Annotated ? "?" : "")}.ToString() }}",

            { NullableAnnotation: { } nullability, IsTupleType: true, TupleElements: { } els } =>
                els
                .Select(el => @$"{{ ""{agentOptions.QueryPropCasingFn(el.Name)}"": content{(nullability == NullableAnnotation.Annotated ? "?" : "")}.{{{$"{el.Name}{(el.Type.IsValueType ? "" : "?")}"}}}.ToString() }}")
                .Join(@", 
                        "),

            _ => contentType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsIndexer)
                .Select(p => BuildMultiPartTuple(usings, agentOptions, (INamedTypeSymbol)p.Type, $@"""{p.Name}""", headers, false)).Join(@", 
                        ")
        };
    }

    private static string BuildMultiPartTuple(HashSet<string> usings, AgentOptions agentOptions, INamedTypeSymbol type, string fieldName, string headers, bool isFile)
    {
        return $@"({BuildHttpContent(usings, agentOptions, type, isFile, headers)}, {fieldName}, {isFile}{(isFile ? ", content.Name" : "")})";
    }

    private static string BuildHttpContent(HashSet<string> usings, AgentOptions agentOptions, INamedTypeSymbol contentType, bool isFile, string headers)
    {
        return isFile ? "content.ToByteArrayContent()" : $@"CreateFormUrlEncoded(new Dictionary<string, string> {{
                        {BuildFormUrlEncodedItems(usings, agentOptions, contentType, headers)}
                    }}";
    }

    static string GetType(ITypeSymbol type, bool useInterfaceImplName = false)
    {
        return type switch
        {

            IArrayTypeSymbol { ElementType: { } arrayType }
                => GetType(arrayType) + "[]",

            INamedTypeSymbol { Name: "Nullable", TypeArguments: [{ } underlyingType] }
                => $"{GetType(underlyingType)}?",

            INamedTypeSymbol { IsTupleType: true, TupleElements: var elements }
                => $"({elements.Join(f => $"{GetType(f.Type)}{(f.IsExplicitlyNamedTupleElement ? $" {f.Name}" : "")}", ", ")})",

            INamedTypeSymbol { Name: var name, TypeArguments: { Length: > 0 } generics }
                => (GetPossibleNamsepace(type) + $".{name}<{generics.Join(g => GetType(g), ", ")}>").TrimStart('.'),
            _
                => GetTypeName(type, useInterfaceImplName)
        };
    }

    private static string GetPossibleNamsepace(ITypeSymbol type)
    {
        return type.ContainingNamespace?.ToDisplayString()?.Replace("<global namespace>", "") ?? "";
    }

    private static string GetTypeName(ITypeSymbol type, bool useInterfaceImplName)
    {
        var isPrimitive = IsPrimitive((INamedTypeSymbol)type);
        var typeName = isPrimitive ? type.ToDisplayString() : type.Name;

        if (useInterfaceImplName && typeName[0] == 'I' && type.TypeKind == TypeKind.Interface)
            typeName = typeName[1..];

        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            typeName += "?";

        return isPrimitive ? typeName : (GetPossibleNamsepace(type) + '.' + typeName).TrimStart('.');
    }

    private static string GetImplementationClassName(ITypeSymbol type)
    {
        var isPrimitive = IsPrimitive((INamedTypeSymbol)type);
        var typeName = isPrimitive ? type.ToDisplayString() : type.Name;

        return (typeName[0] == 'I' && type.TypeKind == TypeKind.Interface)
            ? typeName[1..]
            : typeName;
    }

    private static string GetSimpleTypeName(ITypeSymbol type)
    {
        var typeName = IsPrimitive((INamedTypeSymbol)type) ? type.ToDisplayString() : type.Name;

        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            typeName += "?";

        return typeName;
    }


    private static bool IsNullable(ITypeSymbol type)
    {
        return (type is INamedTypeSymbol { Name: "Nullable" } or INamedTypeSymbol { NullableAnnotation: NullableAnnotation.Annotated });
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
