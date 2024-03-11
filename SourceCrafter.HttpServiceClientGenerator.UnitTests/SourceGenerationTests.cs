#if DEBUG
using SmartBear.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceCrafter.HttpServiceClient.Attributes;
using System.Collections.Immutable;
using System.Net.Http.Json;
using Generator = SourceCrafter.ServiceGenerator;

namespace SourceCrafter.HttpServiceClient.UnitTests
{
    public class SourceGenerationTests
    {

        [Fact]
        public static /*async*/ void TestGeneratedTextForJsonService()
        {
            GetRootAndModel(@"using SourceCrafter.HttpServiceClient.Attributes;
using SourceCrafter.HttpServiceClient.Enums; 
using SmartBear.Json; 


namespace SmartBear.Json;

[ServiceOptions(""https://petstore.swagger.io/v2/"")]
public interface IPetStoreService
{ 
    IStore Store { get; }
    IPet Pet { get; }
    IUser User { get; }
}",
                out _,
                out var root,
                out var semanticModel,
                typeof(IPetStoreApi),
                typeof(HttpServiceAttribute),
                typeof(ValueTuple<>),
                typeof(HttpClient));

            GenerateCode(root, semanticModel);

        }
//        [Fact]
//        public static /*async*/ void TestGeneratedTextForXmlService()
//        {
//            GetRootAndModel(@"using SourceCrafter.HttpServiceClient.Attributes;
//using SourceCrafter.HttpServiceClient.Enums;
//namespace AppsLoveWorld.Xml;

//[ServiceOptions(""http://restapi.adequateshop.com/api/"", DefaultFormat = ResponseType.Xml)]
//public interface ITravellerApi
//{
//    ITravellerActions Traveler { get; }
//}",
//                out _,
//                out var root,
//                out var semanticModel,
//                typeof(ITravellerApi),
//                typeof(HttpServiceAttribute),
//                typeof(ValueTuple<>),
//                typeof(HttpClient));
//            //compilation.roo
//            GenerateCode(root, semanticModel);

//        }

        private static void GenerateCode(SyntaxNode root, SemanticModel semanticModel)
        {
            foreach (var cls in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(cls) is not ITypeSymbol typeSymbol)
                    continue;
                string
                    interfaceName = typeSymbol.Name,
                    name = interfaceName[1..],
                    clientName = $"{name}Client",
                    agentName = $"{name}Agent";

                Dictionary<string, string> fileAndContent = new();

                var attr = typeSymbol.GetAttributes().First(a => a.AttributeClass.Name.StartsWith("ServiceOptions"));
                //TODO: Usar parametros contextuales para indexers
                Generator.CreateFiles(fileAndContent.Add, semanticModel, attr, typeSymbol, "http");
            }
        }

        private static void GetRootAndModel(string code, out CSharpCompilation compilation, out SyntaxNode root, out SemanticModel model, params Type[] assemblies)
            => GetRootAndModel(code, out compilation, out root, out model, assemblies.Select(a => a.Assembly.Location).Distinct().ToArray());

        private static void GetRootAndModel(string code, out CSharpCompilation compilation, out SyntaxNode root, out SemanticModel model, string[] assemblies)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

            root = tree.GetRoot();

            compilation = CSharpCompilation
                .Create(
                    "Temp",
                    new[] { tree },
                    assemblies
                        .Distinct()
                        .Select(r => MetadataReference.CreateFromFile(r))
                        .ToImmutableArray());

            model = compilation.GetSemanticModel(tree);
        }
    }
}
#endif