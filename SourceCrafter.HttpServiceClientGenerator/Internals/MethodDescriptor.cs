#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Net;
using SourceCrafter.HttpServiceClient.Enums;
using System.Linq;
using System.Collections.Immutable;
using SourceCrafter.HttpServiceClient.Operations;
using System;

namespace SourceCrafter.HttpServiceClient.Internals;

internal sealed class MethodDescriptor
{
    private readonly AgentOptions _opts;

    internal MethodDescriptor(AgentOptions opts)
    {
        BodyFormatType = opts.DefaultBodyFormat.ToString();
        _opts = opts;
    }

    public string? BodyParamName { get; internal set; }
    public ITypeSymbol? QueryType { get; internal set; }
    public string? QueryTypeName { get; internal set; }
    public ITypeSymbol? BodyType { get; internal set; }
    public string? BodyTypeName { get; internal set; }
    public string HttpMethod { get; internal set; } = "Get";
    internal bool IsValid => QueryType != null || BodyType != null || Responses.Any();
    internal Dictionary<string, (string TypeName, ResultFormat Format, bool isNullable, bool allowsNull)> Responses { get; } = new();
    internal ResultFormat DefaultResultFormat => _opts.DefaultResultFormat;
    internal string BodyFormatType { get; set; } = "Json";
    internal bool NeedsJsonOptions { get => _opts.NeedsJsonOptions; set => _opts.NeedsJsonOptions = value; }
    internal string? QueryParamName { get; set; }
    internal List<(string, string)> Headers { get; set; } = new();
    internal string? Signature { get; set; }
    internal string ReturnTypeClass { get; set; } = "";
    public ITypeSymbol Type { get; internal set; } = null!;
    internal string OwingnService { get; set; } = null!;
    internal string ReturnTypeNamespace { get; set; } = null!;

    public override string ToString()
    {
        string
            methodType = HttpMethod,
            name = $"{methodType}Async",
            pathVar = "_path",
            parameters = BuildParameters(out var queryReference, out var contentReference, ref pathVar);

        return @$"

        public async {Signature = $"global::System.Threading.Tasks.Task{BuildReturnStatement(out var responseHandler)} {name}({parameters})"}
        {{
            {BuildRequestSubmission(methodType, pathVar, queryReference, contentReference)}{responseHandler}
        }}";
    }
    private string BuildReturnStatement(out string? handlerSyntax)
    {

        handlerSyntax = "";

        var fullReturnType = "";

        if (Responses.Count == 0)
        {
            handlerSyntax = $@"
            
            if(response.IsSuccessStatusCode) 
                await global::System.Threading.Tasks.Task.CompletedTask;
            throw response.RequestException();";
        }
        else if (Responses.Select(kv => (kv.Key, kv.Value)).ToImmutableArray() is { } responses && responses.Length == 1)
        {
            (_, (fullReturnType, var formatType, var isNullable, _)) = responses[0];

            handlerSyntax = $@" 

            if(response.IsSuccessStatusCode && response.Content?.To{formatType}Async<{fullReturnType}>(";

            if (formatType == ResultFormat.Json)
                handlerSyntax += $@"{_opts.AgentTypeName}.JsonOptions, ";

            handlerSyntax += $@"cancelToken) is {(isNullable
                    ? $@"var requestAsync)
                return requestAsync != null ? (await requestAsync) : null);"
                    : $@"{{}} requestAsync)
                return await requestAsync;")}";
            
            handlerSyntax += $@"
            throw response.RequestException();";

            if (fullReturnType[^1] != '?') 
                fullReturnType += '?';
        }
        else
        {
            string
                resultTypeName = $"{OwingnService}{HttpMethod}Result",
                comma = null!,
                types = "global::System.Net.HttpStatusCode StatusCode, ",
                typesInit = types,
                fieldsList = "value.StatusCode, ";

            fullReturnType = $"{ReturnTypeNamespace}.{resultTypeName}";

            handlerSyntax = @"
            
            switch(response.StatusCode)
            {";

            foreach (var (status, (typeName, formatType, isNullable, allowsNull)) in responses)
            {
                var nullable = typeName[^1] == '?';
                var varName = '_' + status.ToCamel();

                handlerSyntax += $@"
                case global::System.Net.HttpStatusCode.{status} when response.Content?.To{formatType}Async<{typeName}>(";
                
                if (formatType == ResultFormat.Json)
                    handlerSyntax += $@"{_opts.AgentTypeName}.JsonOptions, ";

                handlerSyntax += $@"cancelToken) is ";

                handlerSyntax += nullable 
                    ? $@"var {varName} :
                    return new(response.StatusCode, {status}: {varName} != null ? (await {varName}) : null);"
                    : $@"{{}} {varName} :
                    return new(response.StatusCode, {status}: await {varName});";

                string typeDef = $"{comma}{typeName} {status}";
                
                types += typeDef;
                typesInit += $"{typeDef} = default";
                
                if (allowsNull) typesInit += "!";
                
                fieldsList += $"{comma}value.{status}";
                comma ??= ", ";
            }

            handlerSyntax += @"
            }

            throw response.RequestException();";

            ReturnTypeClass = $@"

    public readonly record struct {resultTypeName}({typesInit})
    {{
        public static implicit operator {resultTypeName}(({types}) value) 
            => new({fieldsList});

        public static implicit operator ({types})({resultTypeName} value) 
            => ({fieldsList});
    }}";
        }


        if (fullReturnType.Length > 0) fullReturnType = '<' + fullReturnType + '>';

        return fullReturnType;

    }
    private string BuildParameters(out string? queryRef, out string? bodyBuilderSyntax, ref string pathVar)
    {
        bodyBuilderSyntax = queryRef = null;
        var paramSkip = 0;
        var parametersSyntax = "";

        if (this is { QueryTypeName: { } queryTypeName, QueryParamName: { } queryParameterName })
        {
            paramSkip++;

            parametersSyntax += $@"
            {queryTypeName} {queryParameterName}";

            queryRef = BuildQueryParams(queryTypeName, out pathVar);
        }

        if (this is
            {
                BodyTypeName: { } bodyTypeName,
                BodyFormatType: { } bodyFormatType,
                BodyParamName: { } bodyParamName,
                BodyType: { } bodyType
            })
        {
            paramSkip++;

            if (paramSkip > 1)
                parametersSyntax += ",";

            var contentHeaders = "";

            var isFile = bodyTypeName == "global::System.IO.FileInfo";

            if (isFile)
            {
                bodyFormatType = "MultipartFormData";
                if (bodyParamName == "body")
                    bodyParamName = "file";
            }

            var content = bodyFormatType switch
            {
                "MultipartFormData" => $"new[] {{ {BuildMultipartItems(bodyType, isFile, contentHeaders, bodyParamName)} }}",

                "FormUrlEncoded" => $@"new global::System.Collections.Generic.Dictionary<string, string> {{
                    {BuildFormUrlEncodedItems(bodyType, contentHeaders)}
                }}",

                _ => bodyParamName
            };

            parametersSyntax += $@"
            {bodyTypeName} {bodyParamName}";

            bodyBuilderSyntax = $@",
                {content}.Create{bodyFormatType}(";

            if (bodyFormatType == "Json")
                bodyBuilderSyntax += $"{_opts.AgentTypeName}.JsonOptions";

            bodyBuilderSyntax += ")";
        }

        if (paramSkip > 0)
            parametersSyntax += ",";

        parametersSyntax += @"
            global::System.Func<global::System.Net.Http.HttpRequestMessage, global::System.Threading.Tasks.Task>? handleRequest = default,
            global::System.Func<global::System.Net.Http.HttpResponseMessage, global::System.Threading.Tasks.Task>? handleResponse = default,
            global::System.Threading.CancellationToken cancelToken = default";

        return parametersSyntax;
    }
    private string BuildQueryParams(string queryTypeName, out string pathVarName)
    {
        pathVarName = "_path";

        if (this is { QueryType: { } queryType, QueryParamName: { } queryParameterName })
        {
            var requestSyntax = "";

            pathVarName = "path";
            requestSyntax += $@"var path = $""{{_path}}";


            if (queryType is { IsValueType: true, IsTupleType: false })
            {
                var paramName = _opts.QueryPropCasingFn(queryParameterName)!;

                var isEnumOrString = queryType.TypeKind == TypeKind.Enum || queryType.SpecialType == SpecialType.System_String;
                var queryCasing = isEnumOrString ? _opts.EnumQueryCasing : _opts.QueryCasing;

                if (isEnumOrString)
                    paramName += GetFormatterExpression(queryCasing, queryType.IsNullable(), isEnumOrString);
                else
                    paramName = $@"global::System.Uri.EscapeDataString($""{{{paramName}}}"")";

                requestSyntax += $@"?{paramName}={{{paramName}}}";
            }
            else
            {
                requestSyntax += $@"{{{_opts.AgentTypeName}.BuildQuery({queryParameterName})}}";

                var signature = $@"string BuildQuery({queryTypeName} query)";

                if (!_opts.HelperMethods.ContainsKey(signature))
                    RegisterQueryBuilder(queryType, signature);
            }
            requestSyntax += @""";
            ";

            return requestSyntax;
        }

        return "";
    }
    private void RegisterQueryBuilder(ITypeSymbol queryType, string signature)
    {
        _opts.HelperMethods[signature] = $@"

        internal static {signature}
        {{
            {BuildQueryBuilderBody(queryType)}
        }}";
    }
    private string BuildQueryBuilderBody(ITypeSymbol queryType)
    {
        var isNullableParam = queryType.IsNullable();
        var items = GetQueryProperties(queryType);
        var body = @"return new global::SourceCrafter.HttpServiceClient.QueryBuilder()";

        if (isNullableParam) body = $@"if(query == null) return string.Empty;

            {body}";

        foreach (var (_, memberName, isNullable, type) in items)
        {
            string
                queryParamName = _opts.QueryPropCasingFn(memberName)!;

            body += $@"
                .Add(""{queryParamName}"", query.{memberName}";

            if (type is { TypeKind: TypeKind.Enum } or { SpecialType: SpecialType.System_String })
                body += GetFormatterExpression(_opts.EnumQueryCasing, isNullable, true, false);

            body += ")";

        }

        body += @"
                .ToString();";

        return body;
    }
    private static IEnumerable<(int i, string name, bool isNullable, ITypeSymbol type)> GetQueryProperties(ITypeSymbol queryType)
    {
        if (queryType is INamedTypeSymbol { IsValueType: true, IsTupleType: true, TupleElements: { } els })
            for (int i = 0; i < els.Length; i++)
            {
                if (els[i] is { Type: { } type, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } tupleItem)
                    yield return (i, tupleItem.Name, type.IsNullable(), type);
            }
        else
        {
            var members = queryType.GetMembers();
            for (int i = 0; i < members.Length; i++)
            {
                switch (members[i])
                {
                    case IPropertySymbol
                    {
                        DeclaredAccessibility: Accessibility.Public or Accessibility.Internal,
                        IsIndexer: false,
                        Type: { } propType,
                        Name: { } name
                    } prop:
                        yield return (i, name, propType.IsNullable(), propType);
                        continue;

                    case IFieldSymbol
                    {
                        DeclaredAccessibility: Accessibility.Public or Accessibility.Internal,
                        Type: { } fldType,
                        Name: { } name
                    }:
                        yield return (i, name, fldType.IsNullable(), fldType);
                        continue;
                }
            }
        }
    }
    private string BuildRequestSubmission(string methodType, string pathVar, string? queryReference, string? contentReference) =>
                $@"{queryReference}var request = {_opts.AgentTypeName}.CreateRequest(
                global::System.Net.Http.HttpMethod.{methodType},
                {pathVar}, 
                global::System.UriKind.Relative{contentReference});

            if(handleRequest != null)
                await handleRequest(request);

            var response = await {_opts.AgentTypeName}.SendAsync(request, cancelToken);
            
            if(handleResponse != null)
                await handleResponse(response);";
    private string BuildMultipartItems(ITypeSymbol contentType, bool isFile, string headers, string contentParamName = "body")
    {
        if (isFile)
            return @$"({contentParamName}.ToByteArrayContent(), ""{contentParamName}"", {contentParamName}.Name)";

        return contentType switch
        {
            { IsValueType: true } =>
                @$"({BuildHttpContent(contentType, isFile, headers)}, ""{contentParamName}"", null)",

            INamedTypeSymbol { IsTupleType: true, TupleElements: { } els } =>
                els
                .Select(el => BuildMultiPartTuple((INamedTypeSymbol)el.Type, el.IsExplicitlyNamedTupleElement ? $@"""{el.Name}""" : "null", headers, isFile))
                .Join(", "),

            _ => contentType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsIndexer && p.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
                .Select(p => BuildMultiPartTuple((INamedTypeSymbol)p.Type, $@"""{p.Name}""", headers, isFile))
                .Join(", ")
        };
    }
    private string BuildFormUrlEncodedItems(ITypeSymbol contentType, string headers)
    {
        return contentType switch
        {
            { NullableAnnotation: { } nullability, IsValueType: true } =>
                @$"{{ ""content"": content{(nullability == NullableAnnotation.Annotated ? "?" : "")}.ToString() }}",

            INamedTypeSymbol { NullableAnnotation: { } nullability, IsTupleType: true, TupleElements: { } els } =>
                els
                .Select(el => @$"{{ ""{_opts.QueryPropCasingFn(el.Name)}"": content{(nullability == NullableAnnotation.Annotated ? "?" : "")}.{{{$"{el.Name}{(el.Type.IsValueType ? "" : "?")}"}}}.ToString() }}")
                .Join(@", 
                        "),

            _ => contentType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsIndexer && p.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
                .Select(p => BuildMultiPartTuple(p.Type, $@"""{_opts.PropertyCasingFn(p.Name)}""", headers, false)).Join(@", 
                        ")
        };
    }
    private string BuildMultiPartTuple(ITypeSymbol type, string fieldName, string headers, bool isFile)
    {
        return $@"({BuildHttpContent(type, isFile, headers)}, {fieldName}, {isFile}{(isFile ? ", content.Name" : "")})";
    }
    private string BuildHttpContent(ITypeSymbol contentType, bool isFile, string headers)
    {
        return isFile ? "content.ToByteArrayContent()" : $@"CreateFormUrlEncoded(new System.Collections.Generic.Dictionary<string, string> {{
                        {BuildFormUrlEncodedItems(contentType, headers)}
                    }}";
    }
    private static string GetFormatterExpression(Casing propCasing, bool isNullable, bool isEnumOrString, bool interpolationOrObject = true)
    {
        var expr = "";
        if (interpolationOrObject && propCasing == Casing.None)
        {
            return expr;
        }
        else if (isEnumOrString)
        {
            if (isNullable) expr += '?';

#pragma warning disable CS8509 // La expresión switch no controla todos los valores posibles de su tipo de entrada (no es exhaustiva).
            return expr + propCasing switch
            {
                Casing.Digit => @".ToString(""D"")",
                Casing.CamelCase => $".{nameof(GeneratorHelpers.ToCamel)}()",
                Casing.PascalCase => $".{nameof(GeneratorHelpers.ToPascal)}()",
                Casing.LowerCase => $".ToLower()",
                Casing.UpperCase => $".ToUpper()",
                Casing.LowerSnakeCase => $".{nameof(GeneratorHelpers.ToSnakeLower)}()",
                Casing.UpperSnakeCase => $".{nameof(GeneratorHelpers.ToSnakeUpper)}()"
            };
#pragma warning restore CS8509 // La expresión switch no controla todos los valores posibles de su tipo de entrada (no es exhaustiva).
        }
        else
        {
            if (isNullable)
                expr += '?';

            if (interpolationOrObject)
                return expr;

            return expr + ".ToString()";
        }
#pragma warning restore CS8509 // La expresión switch no controla todos los valores posibles de su tipo de entrada (no es exhaustiva).



    }

}
