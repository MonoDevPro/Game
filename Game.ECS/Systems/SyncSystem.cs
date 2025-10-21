using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por coletar estados que precisam ser sincronizados pela rede.
/// Coleta snapshots de entidades que estão marcadas como NetworkDirty.
/// </summary>
public sealed partial class SyncSystem(World world) : GameSystem(world)
{
    /// <summary>
    /// Coleta snapshots de entrada (input) de jogadores locais que estão dirty.
    /// </summary>
    [Query]
    [All<LocalPlayerTag, PlayerInput, NetworkDirty>]
    private void CollectPlayerInputSnapshots(in Entity e, in PlayerInput input, [Data] float _)
    {
        if (!World.TryGet(e, out NetworkId netId))
            return;

        var snapshot = new PlayerInputSnapshot(
            NetworkId: netId.Value,
            InputX: input.InputX,
            InputY: input.InputY,
            Flags: input.Flags);

        // Nota: Este sistema apenas coleta os dados.
        // A transmissão é responsabilidade de um serializer/network layer separado.
        OnPlayerInputSnapshot?.Invoke(snapshot);
    }

    /// <summary>
    /// Coleta snapshots de estado (posição, direção) de entidades que estão dirty.
    /// </summary>
    [Query]
    [All<Position, Facing, Velocity, NetworkDirty>]
    private void CollectPlayerStateSnapshots(in Entity e, in Position pos, in Facing facing, in Velocity vel, [Data] float _)
    {
        if (!World.TryGet(e, out NetworkId netId))
            return;

        if (!World.TryGet(e, out Walkable speed))
            return;

        var snapshot = new PlayerStateSnapshot(
            NetworkId: netId.Value,
            PositionX: pos.X,
            PositionY: pos.Y,
            PositionZ: pos.Z,
            FacingX: facing.DirectionX,
            FacingY: facing.DirectionY,
            Speed: speed.BaseSpeed * speed.CurrentModifier);

        OnPlayerStateSnapshot?.Invoke(snapshot);
    }

    /// <summary>
    /// Coleta snapshots de vitalidade (HP, MP) de entidades que estão dirty.
    /// </summary>
    [Query]
    [All<Health, Mana, NetworkDirty>]
    private void CollectPlayerVitalsSnapshots(in Entity e, in Health health, in Mana mana, [Data] float _)
    {
        if (!World.TryGet(e, out NetworkId netId))
            return;

        var snapshot = new PlayerVitalsSnapshot(
            NetworkId: netId.Value,
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max);

        OnPlayerVitalsSnapshot?.Invoke(snapshot);
    }

    /// <summary>
    /// Limpa as flags de dirty após sincronização.
    /// Normalmente chamado após o network layer transmitir os dados.
    /// </summary>
    public void ClearDirtyFlags(Entity entity, SyncFlags flags)
    {
        if (World.IsAlive(entity))
        {
            World.ClearNetworkDirty(entity, flags);
        }
    }

    /// <summary>
    /// Limpa todos os flags dirty de uma entidade.
    /// </summary>
    public void ClearAllDirtyFlags(Entity entity)
    {
        ClearDirtyFlags(entity, SyncFlags.All);
    }

    // Callbacks para o network layer processar snapshots
    public event Action<PlayerInputSnapshot>? OnPlayerInputSnapshot;
    public event Action<PlayerStateSnapshot>? OnPlayerStateSnapshot;
    public event Action<PlayerVitalsSnapshot>? OnPlayerVitalsSnapshot;
}
