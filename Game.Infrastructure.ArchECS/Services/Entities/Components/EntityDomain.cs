namespace Game.Infrastructure.ArchECS.Services.Entities.Components;

/// <summary>
/// Domínios de entidades no sistema ECS.
/// Usa Flags para permitir entidades multi-domínio.
/// </summary>
[Flags]
public enum EntityDomain
{
    None = 0,
    
    /// <summary>Entidades de combate</summary>
    Combat = 1 << 0,
    
    /// <summary>Entidades de navegação (pathfinding, movement)</summary>
    Navigation = 1 << 1,
    
    /// <summary>Entidades de IA (NPCs com comportamento autônomo)</summary>
    AI = 1 << 2,
    
    /// <summary>Entidades de inventário/items</summary>
    Inventory = 1 << 3,
    
    /// <summary>Entidades de quests/missões</summary>
    Quest = 1 << 4,
    
    /// <summary>Entidades de social (guilds, parties, friends)</summary>
    Social = 1 << 5,
    
    /// <summary>Entidades de ambiente (portas, baús, objetos interativos)</summary>
    Environment = 1 << 6,
    
    /// <summary>Entidades de efeitos (buffs, debuffs, AoE)</summary>
    Effects = 1 << 7,
    
    /// <summary>Entidades de network (replicação, sincronização)</summary>
    Network = 1 << 8,
    
    /// <summary>Todas as entidades</summary>
    All = ~0
}