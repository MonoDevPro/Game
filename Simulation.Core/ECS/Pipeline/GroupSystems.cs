using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Resources;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Pipeline;

/// <summary>
/// Main ECS pipeline that orchestrates system execution.
/// Focuses purely on execution logic, delegating registration to SystemRegistry.
/// </summary>
public class GroupSystems : ISystem<float>
{
    private readonly World _world;
    private readonly Group<float> _groupSystems;
    private readonly ResourceContext _resources;

    public GroupSystems(World world, ResourceContext resources, AuthorityOptions authorityOptions)
    {
        _world = world;
        _resources = resources;
        
        bool isServer = authorityOptions.Authority == Authority.Server;
        var groupName = isServer ? "ServerSystems" : "ClientSystems";
        _groupSystems = new Group<float>(groupName);
        
        // Use SystemRegistry to handle all system registration logic
        var systemRegistry = new SystemRegistry(_world, _resources, _groupSystems);
        if (isServer)
            systemRegistry.RegisterServerSystems();
        else
            systemRegistry.RegisterClientSystems();
    }

    /// <summary>
    /// Inicializa todos os sistemas registados no grupo.
    /// </summary>
    public void Initialize()
    {
        _groupSystems.Initialize();
    }

    /// <summary>
    /// O método BeforeUpdate é delegado para o grupo interno.
    /// </summary>
    public void BeforeUpdate(in float t)
    {
        _groupSystems.BeforeUpdate(in t);
    }

    /// <summary>
    /// O método Update agora orquestra o ciclo de vida completo para todos os sistemas.
    /// Chama BeforeUpdate, Update e AfterUpdate em sequência.
    /// </summary>
    public void Update(in float t)
    {
        // 1. Chama BeforeUpdate para todos os sistemas.
        _groupSystems.BeforeUpdate(in t);
        
        // 2. Chama Update para todos os sistemas.
        _groupSystems.Update(in t);
        
        // 3. Chama AfterUpdate para todos os sistemas.
        _groupSystems.AfterUpdate(in t);
    }
    
    /// <summary>
    /// O método AfterUpdate é delegado para o grupo interno.
    /// </summary>
    public void AfterUpdate(in float t)
    {
        _groupSystems.AfterUpdate(in t);
    }

    /// <summary>
    /// Liberta os recursos de todos os sistemas registados.
    /// </summary>
    public void Dispose()
    {
        _groupSystems.Dispose();
    }
}