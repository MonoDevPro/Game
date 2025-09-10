using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Simulation.SourceGenerator
{
    [Generator]
    public class ComponentSyncGenerator : IIncrementalGenerator
    {
        // Ajuste esse nome se o seu attribute tiver namespace/nome diferente.
        private const string AttributeName = "Simulation.Abstractions.Network.SynchronizedComponentAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Procurar por structs que tenham o atributo
            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsStructWithAttributes(node),
                    transform: static (ctx, _) => GetStructInfo(ctx))
                .Where(static n => n is not null)
                .Collect();

            context.RegisterSourceOutput(provider, static (spc, arr) =>
            {
                var structs = arr.Where(i => i is not null).Cast<StructInfo>().ToArray();
                if (structs.Length == 0) return;

                var list = structs.Distinct().ToList();
                var code = CodeGenerator.Generate(list);
                spc.AddSource("Network.g.cs", code);
            });
        }

        private static bool IsStructWithAttributes(SyntaxNode node)
        {
            // rápido filtro sintático
            return node is StructDeclarationSyntax s && s.AttributeLists.Count > 0;
        }

        private static StructInfo? GetStructInfo(GeneratorSyntaxContext ctx)
        {
            var structSyntax = (StructDeclarationSyntax)ctx.Node;

            // Encontrar o atributo por metadata name - faz binding no semantic model
            var semanticModel = ctx.SemanticModel;
            var declaredSymbol = semanticModel.GetDeclaredSymbol(structSyntax) as INamedTypeSymbol;
            if (declaredSymbol is null) return null;

            // Procura atributos do tipo com o nome completo ou apenas pelo nome curto
            var attr = declaredSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttributeName
                                   || a.AttributeClass?.Name == "SynchronizedComponentAttribute");

            if (attr is null) return null;

            // Pegar as infos do atributo com parsing robusto
            var authority = ParseAuthorityFromAttribute(attr);
            var trigger = ParseTriggerFromAttribute(attr);
            var syncRateTicks = ParseSyncRateFromAttribute(attr);

            var ns = GetNamespaceFromNode(structSyntax);
            return new StructInfo(structSyntax.Identifier.Text, ns, authority, trigger, syncRateTicks);
        }

        private static string ParseAuthorityFromAttribute(AttributeData attr)
        {
            // suportar enum (int), string, nome qualificado, nulo -> default Server
            if (attr.ConstructorArguments.Length > 0)
            {
                var tc = attr.ConstructorArguments[0];
                if (tc.Value is null) return "Server";
                if (tc.Value is int i) return i == 0 ? "Server" : "Client";
                var raw = tc.Value.ToString();
                if (string.IsNullOrEmpty(raw)) return "Server";
                return raw.IndexOf("Client", StringComparison.OrdinalIgnoreCase) >= 0 ? "Client" : "Server";
            }
            return "Server";
        }

        private static string ParseTriggerFromAttribute(AttributeData attr)
        {
            // Suporta enum int ou string; default OnChange
            if (attr.ConstructorArguments.Length > 1)
            {
                var tc = attr.ConstructorArguments[1];
                if (tc.Value is null) return "OnChange";
                if (tc.Value is int i) return i == 0 ? "OnChange" : "OnTick";
                var raw = tc.Value.ToString();
                if (string.IsNullOrEmpty(raw)) return "OnChange";
                return raw.IndexOf("OnTick", StringComparison.OrdinalIgnoreCase) >= 0 ? "OnTick" : "OnChange";
            }
            return "OnChange";
        }

        private static ushort ParseSyncRateFromAttribute(AttributeData attr)
        {
            if (attr.ConstructorArguments.Length > 2)
            {
                var tc = attr.ConstructorArguments[2];
                if (tc.Value is int i)
                {
                    return (ushort)Math.Max(0, Math.Min(ushort.MaxValue, i));
                }
                if (ushort.TryParse(tc.Value?.ToString() ?? "0", out var v)) return v;
            }
            return 0;
        }

        // Helper to get namespace (copiado/adaptado)
        private static string GetNamespaceFromNode(BaseTypeDeclarationSyntax syntax)
        {
            var potential = syntax.Parent;
            while (potential != null && !(potential is NamespaceDeclarationSyntax) && !(potential is FileScopedNamespaceDeclarationSyntax))
            {
                potential = potential.Parent;
            }

            return (potential as BaseNamespaceDeclarationSyntax)?.Name.ToString() ?? "Global";
        }

        // A struct para transportar a informação necessária para gerar o código
        internal readonly record struct StructInfo(string Name, string Namespace, string Authority, string Trigger, ushort SyncRateTicks)
        {
            public string Name { get; } = Name;
            public string Namespace { get; } = Namespace;
            public string Authority { get; } = Authority;
            public string Trigger { get; } = Trigger;
            public ushort SyncRateTicks { get; } = SyncRateTicks;
        }
    }
}
