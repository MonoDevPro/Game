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
            var trigger = attribute.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value != null
                ? attribute.ConstructorArguments[1].Value.ToString()
                : "0"; // Default to OnChange

            var syncRateTicks = attribute.ConstructorArguments.Length > 2 && attribute.ConstructorArguments[2].Value != null 
                ? (ushort)attribute.ConstructorArguments[2].Value
                : (ushort)0;

            return new StructInfo(
                structSyntax.Identifier.Text,
                structSyntax.GetNamespace(),
                authority == "0" ? "Server" : "Client",
                trigger == "0" ? "OnChange" : "OnTick", // Now correctly parsed
                syncRateTicks // Now correctly parsed
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