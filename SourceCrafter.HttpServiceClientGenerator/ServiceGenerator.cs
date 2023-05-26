#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient.Enums;
using SourceCrafter.HttpServiceClient.Internals;
using SourceCrafter.HttpServiceClient.Operations;

[assembly: InternalsVisibleTo("SourceCrafter.HttpServiceClientGenerator.UnitTests")]
// ReSharper disable once CheckNamespace
namespace SourceCrafter;
[Generator]
internal class ServiceGenerator : IIncrementalGenerator
{
    private static readonly object Lock = new();
    private const string
        Namespace = "SourceCrafter.HttpServiceClient.Operations",
        QueryType = nameof(Query<object>),
        Body = nameof(Body<object>),
        JsonBody = nameof(JsonBody<object>),
        XmlBody = nameof(XmlBody<object>),
        FormBody = nameof(FormBody<object>),
        FormUrlBody = nameof(FormUrlEncodedBody<object>),
        Result = nameof(Result<object>),
        JsonResult = nameof(JsonResult<object>),
        XmlResult = nameof(XmlResult<object>);

    internal static SymbolDisplayFormat GlobalizedNamespace = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var interfaceDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                $"SourceCrafter.HttpServiceClient.Attributes.{nameof(HttpServiceAttribute)}",
                static (n, _) => n is InterfaceDeclarationSyntax,
                static (ctx, _) => (Attr: ctx.Attributes[0], semanticModel: ctx.SemanticModel, type: (ITypeSymbol)ctx.TargetSymbol)
            );

        context.RegisterSourceOutput(
            interfaceDeclarations,
            static (sourceProducer, gen) =>
            {
                lock (Lock)
                {
                    try
                    {
                        CreateFiles(sourceProducer.AddSource, gen.semanticModel, gen.Attr, gen.type);
                    }
                    catch (Exception e)
                    {
                        sourceProducer.AddSource($"{gen.type}.Error.txt", $"/*{e}*/");
                    }
                }
            });

        interfaceDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                $"SourceCrafter.HttpServiceClient.Attributes.{nameof(HttpJsonServiceAttribute<JsonSerializerContext>)}`1",
                static (n, _) => n is InterfaceDeclarationSyntax,
                static (ctx, _) => (Attr: ctx.Attributes[0], semanticModel: ctx.SemanticModel, type: (ITypeSymbol)ctx.TargetSymbol)
            );

        context.RegisterSourceOutput(
            interfaceDeclarations,
            static (sourceProducer, gen) =>
            {
                lock (Lock)
                {
                    try
                    {
                        CreateFiles(sourceProducer.AddSource, gen.semanticModel, gen.Attr, gen.type);
                    }
                    catch (Exception e)
                    {
                        sourceProducer.AddSource($"{gen.type}.Error.txt", $"/*{e}*/");
                    }
                }
            });

    }

    internal static void CreateFiles(Action<string, string> addFile, SemanticModel semanticModel, AttributeData attr, ITypeSymbol type)
    {
        string
            interfaceName = type.Name,
            name = interfaceName[1..].Replace("Api", "").Replace("Service", ""),
            clientName = $"{name}Client",
            agentName = $"{name}Agent";
        AgentOptions agentOptions = new(
            attr,
            type.ContainingNamespace.ToDisplayString(GlobalizedNamespace),
            agentName);

        try
        {

            HashSet<string> fileNames = new();

            var ctor = $@"public {clientName}(){{
            _path = ""{agentOptions.BaseUrlAbsolutePath}"";            
        }}";

            StringBuilder extraInfo = new();
            void AddInfo(string s)
                => extraInfo.Append(s);

            AddInfo($@"
DefaultBodyType: {agentOptions.DefaultBodyFormat},
DefaultResponseType: {agentOptions.DefaultResultFormat},
DefaultFormat: {agentOptions.DefaultFormat}");


            CreateType(
                agentOptions,
                semanticModel,
                $"{agentOptions.AgentNamespace.Replace("global::", "")}.{clientName}.http.cs".TrimStart('.'),
                type.ContainingNamespace.ToDisplayString(GlobalizedNamespace),
                fileNames,
                type,
                clientName,
                ref interfaceName,
                ctor,
                ImmutableArray<PathSegment>.Empty,
                addFile,
                null
#if DEBUG
                , AddInfo
#endif
                );

            var agentClass = $@"namespace {agentOptions.AgentNamespace.Replace("global::", "")}
{{
    internal sealed class {agentOptions.AgentTypeName} 
    {{
        internal global::System.Collections.Generic.Dictionary<string, object> DefaultQueryValues = new();
        internal global::System.Func<global::System.Net.Http.HttpRequestMessage, global::System.Threading.Tasks.Task>? DefaultRequestHandler = null;
        internal global::System.Func<System.Net.Http.HttpResponseMessage, global::System.Threading.Tasks.Task>? DefaultResponseHandler = null;
        internal global::System.Net.Http.HttpClient Client;
        
        internal static {agentOptions.AgentTypeName} Default {{ get; }} = new();";

            if (agentOptions.NeedsJsonOptions || agentOptions.DefaultFormat == ResultFormat.Json)
                agentClass += @$"

        internal static global::System.Text.Json.JsonSerializerOptions JsonOptions {{ get; }} = InitializeJsonOptions();
        
        private static global::System.Text.Json.JsonSerializerOptions InitializeJsonOptions(){{
            var options = new global::System.Text.Json.JsonSerializerOptions({agentOptions.DefaultJsonContext}) {{                
                ReferenceHandler = global::System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles{GenerateCaseConverter(agentOptions, "PropertyNamingPolicy")}                
            }};
            options.Converters.Insert(0, new global::SourceCrafter.HttpServiceClient.Converters.EnumJsonConverter(global::SourceCrafter.HttpServiceClient.Enums.Casing.{agentOptions.EnumSerializationCasing}));
            return options;
        }}";

            agentClass += $@"

        private {agentOptions.AgentTypeName}()
        {{
            Client ??= new () {{ 
                BaseAddress = new global::System.Uri(""{agentOptions.BaseAddress}"")
            }};
        }}{agentOptions.HelperMethods.Values.Join(@"
                
        ")}

        internal static global::System.Net.Http.HttpRequestMessage CreateRequest(
            global::System.Net.Http.HttpMethod method, 
            string path, 
            global::System.UriKind uriKind, 
            global::System.Net.Http.HttpContent? content = null
        ) =>
            new global::System.Net.Http.HttpRequestMessage(
                method, 
                new global::System.Uri(GetUrl(path), uriKind)
            ) 
                {{ Content = content }};

        internal static async global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> SendAsync(
            global::System.Net.Http.HttpRequestMessage request, 
            global::System.Threading.CancellationToken cancelToken
        ) 
        {{
            if (Default.DefaultRequestHandler is {{}} requestHandler)
                await requestHandler(request);

            var response = await Default.Client.SendAsync(request, cancelToken);

            if (Default.DefaultResponseHandler is {{}} responseHandler)
                await responseHandler(response);

            return response;
        }}

        internal static string GetUrl(string baseUrl) 
        {{
            var append = string.Join(""&"", Default.DefaultQueryValues.Select(kv => kv.Key + '=' + global::System.Uri.EscapeDataString($""{{kv.Value}}"")));
            if(append.Length > 0)
                baseUrl += baseUrl.StartsWith(""?"") ? '&' : '?';
            return baseUrl + append;
        }}
    }}
}}";
#if DEBUG
            agentClass = $@"using static global::SourceCrafter.HttpServiceClient.GeneratorHelpers;
/* Extra Info
{extraInfo}
*/
{agentClass}";
#else
            agentClass = $@"using static global::SourceCrafter.HttpServiceClient.GeneratorHelpers;

{agentClass}";
#endif
            addFile($"{agentOptions.AgentFullTypeName.Replace("global::", "")}.http.cs", $@"//<auto generated>
{agentClass}");
        }
        catch (Exception e)
        {
            addFile($"{agentOptions.AgentFullTypeName.Replace("global::", "")}.http.cs", $"/*{e}*/");
        }
    }

    internal static string GenerateCaseConverter(AgentOptions agentOptions, string prop, string? fallBackConverter = "value => value")
    {
        return agentOptions.PropertyCasing != Casing.None
                            ? $@",
                {prop} = global::SourceCrafter.HttpServiceClient.Policies.CasingPolicy.Create(global::SourceCrafter.HttpServiceClient.Enums.Casing.{agentOptions.PropertyCasing})"
                            : fallBackConverter != null
                                ? $@",
                {prop} = {fallBackConverter}"
                                : "";
    }

    internal static void CreateType(
        AgentOptions agentOptions,
        SemanticModel semanticModel,
        string fileName,
        string containingNamespace,
        HashSet<string> fileNames,
        ITypeSymbol typeSymbol,
        string typeName,
        ref string underlyingInterface,
        string ctor,
        ImmutableArray<PathSegment> dynamicalSegments,
        Action<string, string> addSource,
        string? members
#if DEBUG
        , Action<string> addInfo
#endif
    )
    {
        try
        {
            string
                fullInterfaceName = $"{containingNamespace}.{underlyingInterface}",
                interfaceMethods = "",
                propOverride = "",
                extraTypes = "";

            if (members == null)
            {
                members = "";
                foreach (var member in typeSymbol.GetMembers())
                    if (member is IPropertySymbol prop && TryBuildProperty(
                        agentOptions,
                        semanticModel,
                        fileNames,
                        prop,
                        fullInterfaceName,
                        dynamicalSegments,
                        addSource,
                        out var propSyntax,
                        ref propOverride,
                        ref extraTypes
#if DEBUG
                        , addInfo
#endif
                    ))
                    {
                        members += propSyntax;
                    }

                if (BuildMethods(typeSymbol, agentOptions

#if DEBUG
                    , addInfo
#endif
            ).ToImmutableArray() is { Length: > 0 } methods)
                {
                    foreach (var method in methods)
                    {
                        method.OwingnService = typeName;
                        method.ReturnTypeNamespace = containingNamespace;
                        members += method.ToString();
                        extraTypes += method.ReturnTypeClass;
                        interfaceMethods += $@"
        {method.Signature};
";
                    }

                }
                if (interfaceMethods.Length > 0)
                {
                    interfaceMethods = $@"

    partial interface {underlyingInterface}
    {{{interfaceMethods}
    }}";
                }
                if (propOverride.Length > 0)
                {
                    underlyingInterface = $"_{underlyingInterface}";
                    propOverride = $@"

    public partial interface {underlyingInterface} : {fullInterfaceName}
    {{{propOverride}
    }}";
                    fullInterfaceName = $"{containingNamespace}.{underlyingInterface}";
                }
            }

            var classSyntax = $@"using static global::SourceCrafter.HttpServiceClient.GeneratorHelpers;
using {agentOptions.AgentTypeName} = {agentOptions.AgentFullTypeName};

namespace {containingNamespace.Replace("global::", "")}
{{
    public sealed partial class {typeName} : {fullInterfaceName} 
    {{        
        private string _path;        
        {ctor}{members}
    }}{propOverride + interfaceMethods + extraTypes}
}}";
            if (agentOptions.NeedsJsonOptions)
                classSyntax = @$"using global::System.Net.Http.Json;
{classSyntax}";

            addSource(fileName, classSyntax);
        }
        catch (Exception e)
        {
            addSource(fileName, $"/*{e}*/");
        }

    }

    private static void TryGetServiceDescriptor(IPropertySymbol prop, out string? serviceName, out string? segmentName)
    {
        foreach (var a in prop.GetAttributes())
            if (a is {
                AttributeClass.Name: nameof(ServiceDescriptionAttribute),
                ConstructorArguments: [{ Value: var value }, { Value: var value2 }]
            })
            {
                (serviceName, segmentName) = (value as string, value2 as string);
                return;
            }
        (segmentName, serviceName) = (null, null);
    }

    private static bool TryBuildProperty(
        AgentOptions agentOptions,
        SemanticModel semanticModel,
        HashSet<string> fileNames,
        IPropertySymbol prop,
        string parentType,
        ImmutableArray<PathSegment> segments,
        Action<string, string> addSource,
        out string syntax,
        ref string overrideProp,
        ref string extraTypes
#if DEBUG
        , Action<string> appendExtraText
#endif
        )
    {
        #region Property initializer

        syntax = "";

        string oldFullInterfaceTypeName = prop.Type.ToDisplayString(GlobalizedNamespace);

        GetTypeParts(oldFullInterfaceTypeName, out var nameSpace, out var interfaceName);

        string
            fullInterfaceTypeName = oldFullInterfaceTypeName,
            serviceName = interfaceName[(prop.Type.TypeKind == TypeKind.Interface ? 1 : 0)..],
            fullImplTypeName = $"{nameSpace}.{serviceName}".TrimStart('.');

        string? singleMethod = null, paramsComma = null;
        string
            fullPath,
            allParamsDef,
            indexerParamsRef,
            indexerPath,
            indexerParamsDef = fullPath = allParamsDef = indexerParamsRef = indexerPath = "";

        foreach (var pathSegmentItem in segments)
        {
            if (!string.IsNullOrEmpty(pathSegmentItem.Name))
            {
                var paramDef = $"{paramsComma}{pathSegmentItem}";
                allParamsDef += paramDef;
                paramsComma += ", ";
            }
            fullPath += pathSegmentItem.Value;
        }

        PathSegment pElement = default;
        string propDef;

        TryGetServiceDescriptor(prop, out var newServiceName, out var pathSegment);


        if (newServiceName != null)
        {
            interfaceName = $"I{serviceName = newServiceName}";
            fullImplTypeName = $"{nameSpace}.{serviceName}";
        }

        if (prop.IsIndexer)
        {
            List<PathSegment> newSegments = new();
            string? indexerParamsComma = null;

            foreach (var ip in prop.Parameters)
            {
                var paramName = ip.Name;

                pElement = new($"/{{{paramName}}}", ip.Name, ip.Type.ToDisplayString(GlobalizedNamespace));

                newSegments.Add(pElement);

                fullPath += pElement.Value;
                indexerPath += pElement.Value;

                var append = $"{paramsComma}{pElement}";

                allParamsDef += append;
                indexerParamsRef += $"{indexerParamsComma}{ip.Name}";
                indexerParamsDef += $"{indexerParamsComma}{pElement}";
                paramsComma ??= ", ";
            }

            segments = segments.Concat(newSegments).ToImmutableArray();
            propDef = $@"this[{indexerParamsDef}]";
        }
        else
        {
            propDef = prop.Name;
            pElement = new("/" + (pathSegment ?? agentOptions.PathCasingFn(newServiceName ?? prop.Name)!));

            segments = segments.Append(pElement).ToImmutableArray();

            fullPath += pElement.Value;
            indexerPath += pElement.Value;
        }

        //For container and new class
        var hasExplicitOpType = TryGetMethodDescriptor(agentOptions, prop.Type as INamedTypeSymbol, out var descriptor
#if DEBUG
            , appendExtraText
#endif
            );
        if (hasExplicitOpType)
        {
            descriptor.Type = prop.ContainingType;
            string computedShallowProp = prop.IsIndexer ? $"this[{indexerParamsRef}]" : prop.Name;
            syntax = $@"

        {fullInterfaceTypeName} {parentType}.{propDef} => {computedShallowProp};";


            if (newServiceName == null)
            {
                serviceName = descriptor.OwingnService = prop.IsIndexer
                                ? GetServiceNameFromParams(segments, prop.ContainingType) ?? agentOptions.AgentTypeName[..^5]
                                : prop.Name;
            }

            fullImplTypeName = (descriptor.ReturnTypeNamespace = nameSpace = agentOptions.AgentNamespace) + "." + serviceName;

            var oldInterface = fullInterfaceTypeName;

            interfaceName = "I" + serviceName;

            fullInterfaceTypeName = nameSpace + "." + interfaceName;

            overrideProp += $@"
        new {fullInterfaceTypeName} {propDef} {{ get; }}";

            singleMethod = descriptor.ToString();

            extraTypes += descriptor.ReturnTypeClass;

            singleMethod += $@"
    }}
}}

namespace {nameSpace.Replace("global::", "")}
{{
    public partial interface {interfaceName} : {oldInterface}
    {{
        {descriptor.Signature};
";
        }
        #endregion
        var fileName = fullImplTypeName.Replace("global::", "") + "Service.http.cs";
        if (!fileNames.Add(fullImplTypeName))
            return false;

        #region Property builder

        var ctor = $@"internal static {serviceName}Service Create(string path) => new() {{ _path = path }};";

        if (allParamsDef.Length > 0)
            ctor += $@"

        public {serviceName}Service(){{}}";
        ctor += $@"

        public {serviceName}Service({allParamsDef}) => _path = {(allParamsDef.Length > 0 ? "$\"" : "\"")}{agentOptions.BaseUrlAbsolutePath}{fullPath}"";";

        #endregion

        CreateType(
            agentOptions,
            semanticModel,
            fileName,
            nameSpace,
            fileNames,
            prop.Type,
            serviceName + "Service",
            ref interfaceName,
            ctor,
            segments,
            addSource,
            singleMethod
#if DEBUG
            , appendExtraText
#endif
        );

        #region Parent type modifier

        if (interfaceName.StartsWith("_") && syntax == "")
        {
            syntax = $@"

        {oldFullInterfaceTypeName} {parentType}.{propDef} => {(prop.IsIndexer ? $"this[{indexerParamsRef}]" : prop.Name)};";

            overrideProp += $@"
        new {nameSpace}.{interfaceName} {propDef} {{ get; }}";
        }
        syntax = $@"

        public {nameSpace}.{interfaceName} {propDef} => 
            {fullImplTypeName}Service.Create($""{{_path}}{indexerPath}"");{syntax}";

        #endregion

        return true;
    }

    private static string? GetServiceNameFromParams(ImmutableArray<PathSegment> segments, ITypeSymbol type) =>
        segments
            .Where(s => s.Name != null)
            .Select(r => r.Name!.ToPascal())
            .ToArray() switch
        {
            { Length: { } l and > 0 } names =>
                type.Name[(type is { Name: ['I',..], TypeKind: TypeKind.Interface }  ? 1 : 0)..] + "By" + (l == 1
                    ? names[0]
                    : string.Join("", names.Take(l - 1)) + "And" + names[^1]),
            _ => null
        };

    static void GetTypeParts(string type, out string nameSpace, out string typeName)
    {
        nameSpace = "";
        var _typeMame = typeName = type.Replace("<global namespace>", "").TrimStart('.');

        if (typeName.IndexOf('<') is { } lt and > -1)
            typeName = typeName[..lt];

        if (typeName.LastIndexOf('.') is { } dot and > -1)
        {
            nameSpace = typeName[..dot];
            typeName = _typeMame[(dot + 1)..];
        }

    }

    private static IEnumerable<MethodDescriptor> BuildMethods(
        ITypeSymbol cls,
        AgentOptions agentOptions
#if DEBUG
        , Action<string> appendInfo
#endif
        )
    {
        List<MethodDescriptor> methods = new();

        foreach (var iFace in cls.Interfaces)
        {
            if (TryGetMethodDescriptor(agentOptions, iFace, out var opDescriptor
#if DEBUG
                    , appendInfo
#endif
            ))
                methods.Add(opDescriptor);
        }
        return methods;
    }

    private static bool TryGetMethodDescriptor(
        AgentOptions agentOptions,
        INamedTypeSymbol? iFace,
        out MethodDescriptor opDescriptor
#if DEBUG
        , Action<string> appendInfo
#endif
        )
    {
        opDescriptor = null!;

        if (iFace is not { Name: ['I', 'H', 't', 't', 'p', ..] name })
            return false;

        if (iFace.TypeArguments is [INamedTypeSymbol { IsTupleType: true, TupleElements: { } descriptors }])
        {
            opDescriptor = new(agentOptions) { HttpMethod = name[5..] };

            foreach (var descriptor in descriptors)
            {
                if (descriptor.Type as INamedTypeSymbol is { } type)
                    FromGenericDescriptor(
                        opDescriptor,
                        descriptor.IsExplicitlyNamedTupleElement ? descriptor.Name : null,
                        type
#if DEBUG
                        , appendInfo
#endif
                    );
            }

            opDescriptor.HttpMethod = name[5..];
        }
        else if (iFace.TypeArguments.FirstOrDefault() is INamedTypeSymbol { } first)
        {
            opDescriptor = new(agentOptions) { HttpMethod = name[5..] };

            FromGenericDescriptor(
                opDescriptor,
                null,
                first
#if DEBUG
                , appendInfo
#endif
                );
        }
        return opDescriptor.IsValid;
    }

    private static void FromGenericDescriptor(MethodDescriptor opDescriptor, string? name, ITypeSymbol type
#if DEBUG
        , Action<string> appendInfo
#endif
        )
    {
        var (isOpNs, resName) = (type.ContainingNamespace.ToString() == Namespace, type.Name);

        if (isOpNs && type is INamedTypeSymbol { TypeArguments: [{ } innerType] })
            type = innerType;

        var realTypeName = type.ToDisplayString(GlobalizedNamespace);

        var isValidCode = IsValidCode(name, out var code);

        if (!isValidCode && ((isOpNs && resName.Contains(QueryType)) || name is not (null or "body")))
        {
            opDescriptor.QueryParamName = name ?? "query";
            opDescriptor.QueryType = type;
            opDescriptor.QueryTypeName = realTypeName;
        }
        else if (!isValidCode && ((isOpNs && type.Name.Contains(Body)) || name == "body"))
        {
            opDescriptor.BodyParamName = name ?? "body";
            opDescriptor.BodyType = type;
            opDescriptor.BodyTypeName = realTypeName;
            if (isOpNs && resName.IndexOf(Body) is { } i and > 0 && resName[..i] is { Length: > 0 } format)
                opDescriptor.BodyFormatType = format;
        }
        else
        {
            string status = SourceCrafter.HttpExtensions.HttpStatuses.TryGetValue(code, out var _status) ? _status : "OK";
            opDescriptor.Responses[status] =
                (
                    realTypeName,
                    TryGetResultFormat(isOpNs, resName, out var result)
                        ? result
                        : opDescriptor.DefaultResultFormat,
                    type.IsNullable(),
                    type.AllowsNull()
                );

        }

        static bool IsValidCode(string? name, out int code)
        {
            code = 200;
            return name is { Length: 4 } and ['_', .. var a] && int.TryParse(a, out code);
        }

        static bool TryGetResultFormat(bool isOpNs, string resName, out ResultFormat result)
        {
            result = default;
            return isOpNs && resName.IndexOf(Result) is { } i and > 0 && resName[..i] is { Length: > 0 } format && Enum.TryParse(format, out result);
        }
    }

    private static string UnwrapTypeName(string returnTypeName, string token)
    {
        if (returnTypeName.StartsWith(token))
            return returnTypeName[token.Length..^1];
        else
            return returnTypeName;
    }

    private static bool IsPrimitive(INamedTypeSymbol type)
    {
        return type?.SpecialType is
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
            SpecialType.System_String;
    }
}

internal record struct PathSegment(
    string Value,
    string? Name = null,
    string? Type = null)
{
    private string _string;
    public override string ToString() =>
        _string ??= Name is { Length: > 0 } ? $"{Type} {Name}" : Value;
}

internal record struct ServiceDescriptor(
    string Namespace,
    string Name,
    string UnderlyingInterface,
    string Members)
{
    private string _string;
    public override string ToString() => _string ??= $"";
}



internal record struct CommaSeparatedSyntax(ImmutableList<string> Expressions, string WrapperSymbol = "()")
{
    public CommaSeparatedSyntax() :
        this(ImmutableList<string>.Empty)
    { }
    public override string ToString() =>
        WrapperSymbol.Insert(1, string.Join(", ", Expressions));
}

