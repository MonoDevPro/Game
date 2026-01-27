namespace Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;

// Character-related components 
public struct CharacterId               { public int Value; }

/// <summary>
/// Tag component para marcar entidades que precisam de cleanup.
/// </summary>
public struct EntityRegistryCleanup { }
