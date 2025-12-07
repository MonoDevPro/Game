namespace Game.ECS.Entities.Components;

/// <summary>
/// Componentes Tags - Marcadores
/// </summary>
public struct Dead { }
public struct SpawnRequest { }

/// <summary>
/// Ponto de spawn de uma entidade.
/// </summary>
public readonly record struct SpawnPoint(int MapId, int X, int Y, sbyte Floor );

/// <summary>
/// Componente que indica que a entidade está em processo de respawn.
/// </summary>
public struct Respawning { public float TimeRemaining; public float TotalTime; }