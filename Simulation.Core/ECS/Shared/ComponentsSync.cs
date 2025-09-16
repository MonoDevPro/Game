using MemoryPack;
using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.ECS.Shared.Systems.Network;

public readonly record struct InputComponent(IntentFlags Intent, InputFlags Input);
public readonly record struct ActionComponent(StateFlags Value);
public readonly record struct Position(int X, int Y);
public readonly record struct Direction(int X, int Y);
public readonly record struct Health(int Current, int Max);


public readonly record struct FullPlayerData (int PlayerId, MapData CurrentMap, PlayerData[] OtherPlayers);
public readonly record struct PlayerSpawn (int PlayerId, PlayerData Data);
public readonly record struct PlayerDespawn (int PlayerId);