using MemoryPack;

namespace Game.Contracts;

/// <summary>
/// Snapshot delta - contém apenas jogadores que mudaram desde o último snapshot.
/// Usado para reduzir banda quando há muitos jogadores estáticos.
/// </summary>
/// <param name="ServerTick">Tick do servidor.</param>
/// <param name="Timestamp">Timestamp UTC em milissegundos.</param>
/// <param name="BaseTick">Tick do snapshot base (para delta).</param>
/// <param name="Added">Jogadores novos ou que mudaram.</param>
/// <param name="Removed">IDs de jogadores removidos.</param>
[MemoryPackable]
public readonly partial record struct WorldSnapshotDelta(
    long ServerTick,
    long Timestamp,
    long BaseTick,
    List<PlayerState> Added,
    List<int> Removed) : IEnvelopePayload
{
    /// <summary>
    /// Indica se este delta tem mudanças.
    /// </summary>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0;
    
    /// <summary>
    /// Cria um delta vazio.
    /// </summary>
    public static WorldSnapshotDelta Empty(long serverTick, long baseTick) => new(
        serverTick,
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        baseTick,
        new List<PlayerState>(),
        new List<int>());
}

public sealed class SnapshotDeltaBuffers
{
    public Dictionary<int, PlayerState> PreviousById { get; } = new();
    public HashSet<int> CurrentIds { get; } = new();

    public void Reset(int previousCount, int currentCount)
    {
        PreviousById.Clear();
        CurrentIds.Clear();
        PreviousById.EnsureCapacity(previousCount);
        CurrentIds.EnsureCapacity(currentCount);
    }
}

/// <summary>
/// Utilitário para calcular deltas entre snapshots.
/// </summary>
public static class SnapshotDeltaCalculator
{
    /// <summary>
    /// Calcula o delta entre dois snapshots.
    /// </summary>
    /// <param name="previous">Snapshot anterior (base).</param>
    /// <param name="current">Snapshot atual.</param>
    /// <returns>Delta contendo apenas as mudanças.</returns>
    public static WorldSnapshotDelta Calculate(WorldSnapshot previous, WorldSnapshot current)
    {
        return Calculate(previous, current, new SnapshotDeltaBuffers());
    }

    public static WorldSnapshotDelta Calculate(WorldSnapshot previous, WorldSnapshot current, SnapshotDeltaBuffers buffers)
    {
        buffers.Reset(previous.Players.Count, current.Players.Count);

        var previousDict = buffers.PreviousById;
        foreach (var player in previous.Players)
        {
            previousDict[player.CharacterId] = player;
        }

        var added = new List<PlayerState>();
        var currentIds = buffers.CurrentIds;

        foreach (var player in current.Players)
        {
            currentIds.Add(player.CharacterId);

            // Verifica se é novo ou mudou
            if (!previousDict.TryGetValue(player.CharacterId, out var prev))
            {
                // Novo jogador
                added.Add(player);
            }
            else if (HasChanged(prev, player))
            {
                // Jogador mudou
                added.Add(player);
            }
        }

        // Encontra removidos
        var removed = new List<int>();
        foreach (var id in previousDict.Keys)
        {
            if (!currentIds.Contains(id))
            {
                removed.Add(id);
            }
        }

        return new WorldSnapshotDelta(
            current.ServerTick,
            current.Timestamp,
            previous.ServerTick,
            added,
            removed);
    }
    
    /// <summary>
    /// Verifica se um jogador mudou significativamente.
    /// </summary>
    private static bool HasChanged(PlayerState prev, PlayerState curr)
    {
        // Mudou posição
        if (prev.X != curr.X || prev.Y != curr.Y)
            return true;
            
        // Mudou andar
        if (prev.Floor != curr.Floor)
            return true;
            
        // Mudou estado de movimento
        if (prev.IsMoving != curr.IsMoving)
            return true;
            
        // Mudou direção (enquanto em movimento)
        if (curr.IsMoving && (prev.DirX != curr.DirX || prev.DirY != curr.DirY))
            return true;
            
        // Mudou alvo (enquanto em movimento)
        if (curr.IsMoving && (prev.TargetX != curr.TargetX || prev.TargetY != curr.TargetY))
            return true;

        // Mudou HP/MP
        if (prev.CurrentHp != curr.CurrentHp || prev.MaxHp != curr.MaxHp ||
            prev.CurrentMp != curr.CurrentMp || prev.MaxMp != curr.MaxMp)
            return true;
            
        return false;
    }
    
    /// <summary>
    /// Aplica um delta a um snapshot para reconstruir o estado atual.
    /// </summary>
    /// <param name="baseSnapshot">Snapshot base.</param>
    /// <param name="delta">Delta a aplicar.</param>
    /// <returns>Snapshot reconstruído.</returns>
    public static WorldSnapshot ApplyDelta(WorldSnapshot baseSnapshot, WorldSnapshotDelta delta)
    {
        var players = new Dictionary<int, PlayerState>(baseSnapshot.Players.Count);
        
        // Copia jogadores do base
        foreach (var player in baseSnapshot.Players)
        {
            players[player.CharacterId] = player;
        }
        
        // Remove jogadores
        foreach (var id in delta.Removed)
        {
            players.Remove(id);
        }
        
        // Adiciona/atualiza jogadores
        foreach (var player in delta.Added)
        {
            players[player.CharacterId] = player;
        }
        
        return new WorldSnapshot(delta.ServerTick, delta.Timestamp, [.. players.Values]);
    }
}
