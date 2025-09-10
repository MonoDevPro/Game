using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Simulation.SourceGenerator
{
    [Generator]
    public class ComponentSyncGenerator : IIncrementalGenerator
    {
        private const string AttributeName = "Simulation.Core.Shared.Network.Attributes.SynchronizedComponentAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    (node, _) => node is StructDeclarationSyntax,
                    GetStructInfo)
                .Collect();
            
            context.RegisterSourceOutput(provider, Execute);
        }

        private static StructInfo GetStructInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
        {
            var structSyntax = (StructDeclarationSyntax)ctx.TargetNode;
            var attribute = ctx.Attributes.First();
            var authority = attribute.ConstructorArguments[0].Value?.ToString();
            
            return new StructInfo(
                structSyntax.Identifier.Text,
                structSyntax.GetNamespace(),
                authority == "0" ? "Server" : "Client"
            );
        }

        private void Execute(SourceProductionContext context, ImmutableArray<StructInfo> structs)
        {
            if (structs.IsDefaultOrEmpty) return;

            var distinctStructs = structs.Distinct().ToList();
            var code = CodeGenerator.Generate(distinctStructs);
            context.AddSource("Network.g.cs", code);
        }
    }
}