using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Client.Combat.Components;
using GameECS.Shared.Combat.Data;

namespace GameECS.Client.Combat.Systems;

/// <summary>
/// Sistema que sincroniza dados de combate do servidor com o cliente.
/// </summary>
public sealed partial class ClientCombatSyncSystem : BaseSystem<World, float>
{
    private readonly Dictionary<int, Entity> _serverEntityMap;

    public ClientCombatSyncSystem(World world) : base(world)
    {
        _serverEntityMap = new Dictionary<int, Entity>(256);
    }

    [Query]
    [All<SyncedHealth, HealthBar>]
    private void SyncHealthBar(ref SyncedHealth synced, ref HealthBar bar)
    {
        bar.SetTarget(synced.Percentage);
    }

    [Query]
    [All<SyncedMana, ManaBar>]
    private void SyncManaBar(ref SyncedMana synced, ref ManaBar bar)
    {
        bar.SetTarget(synced.Percentage);
    }

    /// <summary>
    /// Registra mapeamento de entidade do servidor para cliente.
    /// </summary>
    public void RegisterEntity(int serverId, Entity clientEntity)
    {
        _serverEntityMap[serverId] = clientEntity;
    }

    /// <summary>
    /// Remove mapeamento de entidade.
    /// </summary>
    public void UnregisterEntity(int serverId)
    {
        _serverEntityMap.Remove(serverId);
    }

    /// <summary>
    /// Obtém entidade do cliente pelo ID do servidor.
    /// </summary>
    public bool TryGetClientEntity(int serverId, out Entity entity)
    {
        return _serverEntityMap.TryGetValue(serverId, out entity);
    }

    /// <summary>
    /// Processa mensagem de dano do servidor.
    /// </summary>
    public void ProcessDamageMessage(int targetServerId, int damage, bool isCritical, 
        DamageType type, float targetX, float targetY)
    {
        if (!TryGetClientEntity(targetServerId, out var entity))
            return;

        // Adiciona texto flutuante de dano
        if (World.Has<FloatingDamageBuffer>(entity))
        {
            ref var buffer = ref World.Get<FloatingDamageBuffer>(entity);
            buffer.Add(FloatingDamageText.Create(damage, targetX, targetY, isCritical, type));
        }
    }

    /// <summary>
    /// Processa atualização de vida do servidor.
    /// </summary>
    public void ProcessHealthUpdate(int serverId, int currentHealth, int maxHealth, long serverTick)
    {
        if (!TryGetClientEntity(serverId, out var entity))
            return;

        if (World.Has<SyncedHealth>(entity))
        {
            ref var health = ref World.Get<SyncedHealth>(entity);
            health.Sync(currentHealth, maxHealth, serverTick);
        }
    }

    /// <summary>
    /// Processa atualização de mana do servidor.
    /// </summary>
    public void ProcessManaUpdate(int serverId, int currentMana, int maxMana, long serverTick)
    {
        if (!TryGetClientEntity(serverId, out var entity))
            return;

        if (World.Has<SyncedMana>(entity))
        {
            ref var mana = ref World.Get<SyncedMana>(entity);
            mana.Sync(currentMana, maxMana, serverTick);
        }
    }

    /// <summary>
    /// Processa morte de entidade do servidor.
    /// </summary>
    public void ProcessDeathMessage(int serverId)
    {
        if (!TryGetClientEntity(serverId, out var entity))
            return;

        if (!World.Has<VisuallyDead>(entity))
            World.Add<VisuallyDead>(entity);
    }
}
