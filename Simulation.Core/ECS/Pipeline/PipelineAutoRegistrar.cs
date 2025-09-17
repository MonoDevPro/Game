using System.Reflection;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;

namespace Simulation.Core.ECS.Pipeline;

public static class PipelineAutoRegistrar
{
    private record SystemMeta(Type ImplType, PipelineSystemAttribute Attr);

    // DependencyRules legacy removido; agora usamos DependsOnAttribute dinâmico.

    public static void RegisterAttributedSystems<TGroup>(this TGroup group, IServiceProvider provider, bool isServer, params Assembly[] assemblies)
        where TGroup : Group<float>
    {
        if (assemblies.Length == 0)
            assemblies = [Assembly.GetExecutingAssembly()];

        var systems = new List<SystemMeta>();
        foreach (var asm in assemblies)
        {
            foreach (var t in asm.GetTypes())
            {
                if (t.IsAbstract || !typeof(ISystem<float>).IsAssignableFrom(t))
                    continue;
                var attr = t.GetCustomAttribute<PipelineSystemAttribute>();
                if (attr == null) continue;
                // Filtra por destino
                if (isServer && !attr.Server) continue;
                if (!isServer && !attr.Client) continue;
                systems.Add(new SystemMeta(t, attr));
            }
        }

        // Ordena por Stage e depois OrderOffset
        var ordered = systems
            .OrderBy(s => s.Attr.Stage)
            .ThenBy(s => s.Attr.OrderOffset)
            .ToList();

        ValidateDependencies(ordered);

        foreach (var meta in ordered)
        {
            ISystem<float>? instance = provider.GetService(meta.ImplType) as ISystem<float>;
            if (instance is null)
            {
                // Fallback: tenta construir via ActivatorUtilities (suporta DI em ctor)
                instance = ActivatorUtilities.CreateInstance(provider, meta.ImplType) as ISystem<float>;
            }
            if (instance is null)
                throw new InvalidOperationException($"Sistema {meta.ImplType.Name} não pôde ser instanciado.");
            group.Add(instance);
        }
    }

    private static void ValidateDependencies(List<SystemMeta> ordered)
    {
        var position = new Dictionary<Type, int>();
        for (int i = 0; i < ordered.Count; i++)
            position[ordered[i].ImplType] = i;

        // Coletar dependências a partir de atributos
        foreach (var meta in ordered)
        {
            var deps = meta.ImplType.GetCustomAttributes<DependsOnAttribute>(false)
                .SelectMany(a => a.Dependencies);
            foreach (var dep in deps)
            {
                if (!position.TryGetValue(dep, out var posDep))
                    continue; // dependência não presente no pipeline atual
                var posCurrent = position[meta.ImplType];
                if (posDep > posCurrent)
                    throw new InvalidOperationException($"Dependência inválida: {meta.ImplType.Name} depende de {dep.Name}, mas aparece antes.");
            }
        }
    }
}