using Simulation.Core.ECS.Components.Data;

namespace Simulation.Core.ECS.Components;

public readonly record struct SpawnPlayerRequest(PlayerData Player);
public readonly record struct DespawnPlayerRequest(int PlayerId);