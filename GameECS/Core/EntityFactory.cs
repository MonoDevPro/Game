using Arch.Core;
using Game.Domain.AI.Enums;
using Game.Domain.AI.Interfaces;
using Game.Domain.AI.ValueObjects;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Enums;
using Game.Domain.Player;
using Game.Domain.ValueObjects.Character;
using Game.Domain.ValueObjects.Identitys;
using Game.Domain.ValueObjects.Map;
using GameECS.Shared.Entities.Data;

namespace GameECS.Server.Entities.Core;

/// <summary>
/// Factory para criar entidades no mundo.
/// </summary>
public sealed class EntityFactory(
    World world,
    INpcTemplateProvider? npcTemplateProvider = null,
    IPetTemplateProvider? petTemplateProvider = null)
{
    private readonly INpcTemplateProvider _npcTemplateProvider = npcTemplateProvider ?? new DefaultNpcTemplateProvider();
    private readonly IPetTemplateProvider _petTemplateProvider = petTemplateProvider ?? new DefaultPetTemplateProvider();
    private int _nextEntityId;

    /// <summary>
    /// Cria um Player.
    /// </summary>
    public Entity CreatePlayer(PlayerSimulationAttributes attributes)
    {
        int entityId = ++_nextEntityId;

        // Componentes básicos
        var entity = world.Create(
            new Identity
            {
                UniqueId = entityId,
                Type = EntityType.Player,
            },
            attributes.Ownership,
            attributes.Name,
            attributes.Vocation,
            attributes.Progress,
            attributes.Equipment,
            attributes.Stats,
            attributes.CombatStats,
            attributes.Hp,
            attributes.Mp,
            attributes.Position,
            new PartyMember
            {
                PartyId = 0,
                IsLeader = false
            },
            attributes.VisibilityConfig
        );

        return entity;
    }

    /// <summary>
    /// Cria um NPC a partir de template.
    /// </summary>
    public Entity CreateNpc(string templateId, int x, int y)
    {
        var template = _npcTemplateProvider.Get(templateId);
        if (template == null)
            throw new ArgumentException($"NPC template '{templateId}' not found");

        int entityId = ++_nextEntityId;

        // Criação em etapas para não exceder limite
        var entity = world.Create(
            new Identity
            {
                UniqueId = entityId,
                Type = EntityType.Npc,
            },
            Name.Create(template.Name),
            Progress.Create(template.Level, 0),
            new GridPosition(x, y),
            new NpcBehavior
            {
                Type = template.DefaultBehavior,
                SubType = template.SubType,
                WanderRadius = template.WanderRadius,
                AggroRange = template.AggroRange,
                LeashRange = template.AggroRange * 2 // Default leash is 2x aggro
            },
            new NpcAI
            {
                State = NpcAIState.Idle,
                TargetEntityId = 0,
                StateChangeTick = 0,
                NextActionTick = 0
            },
            new SpawnInfo
            {
                SpawnX = x,
                SpawnY = y,
                RespawnDelayTicks = template.RespawnDelayTicks
            },
            new AggroTable(),
            VisibilityConfig.ForNpc
        );

        return entity;
    }

    /// <summary>
    /// Cria um Pet para um owner.
    /// </summary>
    public Entity CreatePet(string templateId, int ownerEntityId, int x, int y)
    {
        var template = _petTemplateProvider.Get(templateId);
        if (template == null)
            throw new ArgumentException($"Pet template '{templateId}' not found");

        int entityId = ++_nextEntityId;

        var entity = world.Create(
            new Identity
            {
                UniqueId = entityId,
                Type = EntityType.Pet,
            },
            Name.Create(template.Name),
            Progress.Create(1, 0),
            new GridPosition(x, y),
            new PetOwnership
            {
                OwnerEntityId = ownerEntityId,
                IsActive = true
            },
            new PetBehavior
            {
                Mode = template.DefaultMode,
                FollowDistance = template.FollowDistance,
                AttackRange = template.AttackRange
            },
            new PetState(),
            VisibilityConfig.ForPet
        );

        return entity;
    }

    /// <summary>
    /// Cria um item no chão.
    /// </summary>
    public Entity CreateDroppedItem(int itemId, int quantity, int x, int y, int? ownerEntityId = null)
    {
        int entityId = ++_nextEntityId;

        return world.Create(
            new Identity
            {
                UniqueId = entityId,
                Type = EntityType.Item,
            },
            new GridPosition(x, y)
        );
    }

    /// <summary>
    /// Obtém o próximo ID disponível.
    /// </summary>
    public int PeekNextId() => _nextEntityId + 1;

    /// <summary>
    /// Define o próximo ID (para carregar de persistência).
    /// </summary>
    public void SetNextId(int nextId) => _nextEntityId = nextId - 1;
}
