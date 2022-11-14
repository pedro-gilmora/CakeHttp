
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;

[Generator]
public class ApiImplementor : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a factory that can create our custom syntax receiver
        context.RegisterForSyntaxNotifications(() => new ApiInterfaceFinder(context));
    }

    private List<InterfaceDeclarationSyntax> classes = new();

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is ApiInterfaceFinder
            {
                ApiClientinterface:
                {
                    Identifier: { ValueText: { } name }
                } _interface
            })
        {
            // add the generated implementation to the compilation
            var sourceText = SourceText.From(GenerateType(_interface, context, _interface.SyntaxTree.GetRoot(), 0), Encoding.UTF8);
            context.AddSource($"{name[1..]}.g.cs", sourceText);
        }
    }

    private string GenerateType(InterfaceDeclarationSyntax iface, GeneratorExecutionContext context, SyntaxNode root, int indent = 0)
    {
        var apiMetadata = new ApiMetadata(iface, context);
        //var name = iface.Identifier.ValueText;
        context. ((PropertyDeclarationSyntax)iface.Members[0]).Type
        return $"/*{context.Compilation.GetSymbolsWithName(((PropertyDeclarationSyntax)iface.Members[0]).Type.ToString(), SymbolFilter.Type).First().DeclaringSyntaxReferences.First().GetSyntax()}*/";
        //        return @$"/*
        //public sealed class {name[1..]} : {name} {{

        //{GenerateMembers(ifaces.Members, apiMetadata, context, root)}

        //}}*/";
    }

    private string GenerateMembers(SyntaxList<MemberDeclarationSyntax> members, ApiMetadata apiMetadata, GeneratorExecutionContext context, SyntaxNode root)
    {
        return string.Join("\n\n",
            from member in members
            select member switch
            {
                PropertyDeclarationSyntax prop => BuildProperty(prop, apiMetadata, context),
                IndexerDeclarationSyntax idxr => BuildIndexer(idxr, apiMetadata, context),
                MethodDeclarationSyntax method => BuildMethod(method, apiMetadata, context),
                _ => $"//Unable to implement {member}"
            });
    }

    private string BuildIndexer(IndexerDeclarationSyntax idxr, ApiMetadata apiMetadata, GeneratorExecutionContext context)
    {
        var typeName = idxr.Type.ToString();
        var paramsDefinition = "";
        var paramsReferences = "";
        string comma = null;

        apiMetadata.RegisterInterface(idxr.Type);

        foreach (var ip in idxr.ParameterList.Parameters)
        {
            apiMetadata.RegisterParameterMetadata(ip);
            paramsReferences += $"{comma}{ip.Identifier.ValueText}";
            paramsDefinition += $"{comma}{ip.Type} {ip.Identifier.ValueText}{ip.Default}";
            comma ??= ", ";
        }

        return $"   public {typeName} this[{string.Join(", ", paramsDefinition)}] => new {typeName[1..]}(_paths, {string.Join(", ", paramsReferences)})";
    }

    private string BuildProperty(PropertyDeclarationSyntax prop, ApiMetadata apiMetadata, GeneratorExecutionContext context)
    {
        var typeName = prop.Type.ToString();
        var identifier = prop.Identifier.ValueText;

        var segment = apiMetadata.PathAndSegmentCasing switch
        {
            "PathAndSegmentCasing.LowerCase" => identifier.ToLowerInvariant(),
            "PathAndSegmentCasing.UpperCase" => identifier.ToLowerInvariant(),
            "PathAndSegmentCasing.CamelCase" => JsonNamingPolicy.CamelCase.ConvertName(identifier),
            _ => identifier
        };

        if (((CompilationUnitSyntax)prop.Type.SyntaxTree.GetRoot())
            .Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .SelectMany(fn => fn.Members.OfType<InterfaceDeclarationSyntax>())
            .FirstOrDefault(p => p.Identifier.ValueText.Equals(typeName)) is { } ifaceDeclaration)
            apiMetadata.RegisterInterface(prop.Type);

        return $"   public {typeName} {identifier} => new {typeName[1..]}(_paths, \"{segment}\")";
    }

    private string BuildMethod(MethodDeclarationSyntax method, ApiMetadata apiMetadata, GeneratorExecutionContext context)
    {
        var typeName = method.ReturnType.ToString();
        var paramsDefinition = "";
        string comma = null;

        foreach (var ip in method.ParameterList.Parameters)
        {
            apiMetadata.RegisterParameterMetadata(ip);
            paramsDefinition += comma + $"{ip.Type} {ip.Identifier}{ip.Default}";
            comma ??= ", ";
        }

        return $@"   public {typeName} {method.Identifier}{method.TypeParameterList}({string.Join(", ", paramsDefinition)}) {{

}}";
    }

    private class ApiInterfaceFinder : ISyntaxReceiver
    {
        public InterfaceDeclarationSyntax ApiClientinterface { get; private set; }
        internal List<InterfaceDeclarationSyntax> Nested { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Business logic to decide what we're interested in goes here
            if (syntaxNode is InterfaceDeclarationSyntax { Identifier: { ValueText: { } identifier } } ids && identifier.EndsWith("Api"))
            {
                ApiClientinterface = ids;
                // Interface
            }
        }
    }
}