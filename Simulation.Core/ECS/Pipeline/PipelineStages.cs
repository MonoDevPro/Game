using Arch.System;

namespace Simulation.Core.ECS.Pipeline;

/// <summary>
/// Define a ordem macro de execução dos sistemas dentro do pipeline.
/// </summary>
public enum SystemStage
{
    PreNet = 0,
    Net = 10,
    Staging = 20,
    Logic = 30,
    Movement = 40,
    Spatial = 50,
    Save = 60,
    Destruction = 70,
    Rendering = 80,
    Post = 90
}

/// <summary>
/// Atributo aplicado a classes de sistemas para permitir registro automático por estágio.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PipelineSystemAttribute : Attribute
{
    public SystemStage Stage { get; }
    public int OrderOffset { get; }
    public bool Server { get; }
    public bool Client { get; }
    public PipelineSystemAttribute(SystemStage stage, int orderOffset = 0, bool server = true, bool client = true)
    {
        Stage = stage;
        OrderOffset = orderOffset;
        Server = server;
        Client = client;
    }
}

/// <summary>
/// Declara que o sistema atual depende da execução anterior de um ou mais outros sistemas.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DependsOnAttribute : Attribute
{
    public Type[] Dependencies { get; }
    public DependsOnAttribute(params Type[] dependencies)
    {
        Dependencies = dependencies ?? Array.Empty<Type>();
    }
}
