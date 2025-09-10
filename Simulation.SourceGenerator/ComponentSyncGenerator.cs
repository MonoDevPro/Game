using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Simulation.Attributes;

namespace Simulation.SourceGenerator
{
    [Generator]
    public class ComponentSyncGenerator : IIncrementalGenerator
    {
        private const string AttributeName = "Simulation.Core.Shared.Network.Attributes.SynchronizedComponentAttribute";
        
        // Código-fonte do atributo a ser injetado
        private const string AttributeSourceCode = @"
namespace Simulation.Core.Shared.Network.Attributes
{
    public enum Authority { Server, Client }
    public enum SyncTrigger { OnChange, OnTick }

    [System.AttributeUsage(System.AttributeTargets.Struct)]
    public class SynchronizedComponentAttribute : System.Attribute
    {
        public Authority Authority { get; }
        public SyncTrigger Trigger { get; }
        public ushort SyncRateTicks { get; }

        public SynchronizedComponentAttribute(
            Authority authority,
            SyncTrigger trigger = SyncTrigger.OnChange,
            ushort syncRateTicks = 0)
        {
            Authority = authority;
            Trigger = trigger;
            SyncRateTicks = syncRateTicks;
        }
    }
}";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Etapa 1: Injeta o código do atributo na compilação do usuário.
            context.RegisterPostInitializationOutput(ctx => 
                ctx.AddSource("SynchronizedComponentAttribute.g.cs", AttributeSourceCode));

            // Etapa 2: Procura por structs que usam o atributo injetado.
            var provider = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    (node, _) => node is StructDeclarationSyntax,
                    GetStructInfo)
                .Collect();
            
            // Etapa 3: Gera os sistemas de rede com base nas structs encontradas.
            context.RegisterSourceOutput(provider, Execute);
        }

        private static StructInfo GetStructInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
        {
            var structSyntax = (StructDeclarationSyntax)ctx.TargetNode;
            var attribute = ctx.Attributes.First();
            
            var authority = attribute.ConstructorArguments[0].Value?.ToString() == "0" ? "Server" : "Client";
            
            var trigger = SyncTrigger.OnChange;
            if (attribute.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value is int triggerValue)
            {
                trigger = (SyncTrigger)triggerValue;
            }

            return new StructInfo(
                structSyntax.Identifier.Text,
                structSyntax.GetNamespace(),
                authority,
                trigger
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