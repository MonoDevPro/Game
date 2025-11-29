using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por processar mortes de entidades.
/// Verifica entidades com Health e marca como Dead se necessário.
/// </summary>
public sealed partial class DeathSystem(World world, ILogger<DeathSystem>? logger = null)
    : GameSystem(world)
{
    [Query]
    [All<Health>]
    [None<Dead>]
    private void ProcessDeath(
        in Entity entity,
        in Health health)
    {
        if (health.Current > 0) 
            return;
        
        World.Add<Dead>(entity);
        logger?.LogInformation("[DeathSystem] Entity {Entity} has died.", entity);
    }
}