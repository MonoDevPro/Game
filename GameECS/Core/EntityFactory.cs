using Arch.Core;
using Game.Domain.AI.Enums;
using Game.Domain.AI.Interfaces;
using Game.Domain.AI.ValueObjects;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Commons.Entities.Interfaces;
using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Character;
using Game.Domain.Commons.ValueObjects.Identitys;
using Game.Domain.Commons.ValueObjects.Map;
using Game.Domain.Player;
using GameECS.Shared.Entities.Data;

namespace GameECS.Core;

/// <summary>
/// Factory para criar entidades no mundo ECS.
/// </summary>
public sealed class EntityFactory : IEntityFactory
{
    private readonly World _world;
    private readonly INpcTemplateProvider _npcTemplateProvider;
    private readonly IPetTemplateProvider _petTemplateProvider;
    private int _nextEntityId;

    public EntityFactory(
        World world,
        INpcTemplateProvider? npcTemplateProvider = null,
        IPetTemplateProvider? petTemplateProvider = null)
    {
        _world = world;
        _npcTemplateProvider = npcTemplateProvider ?? new DefaultNpcTemplateProvider();
        _petTemplateProvider = petTemplateProvider ?? new DefaultPetTemplateProvider();
    }

    /// <summary>
    /// Cria um Player e retorna o ID da entidade.
    /// </summary>
    int IEntityFactory.CreatePlayer(PlayerSimulationAttributes attributes)
    {
        var entity = CreatePlayer(attributes);
        return _world.Get<Identity>(entity).UniqueId;
    }

    /// <summary>
    /// Cria um Player e retorna a Entity.
    /// </summary>
    public Entity CreatePlayer(PlayerSimulationAttributes attributes)
    {
        int entityId = ++_nextEntityId;

        // Componentes básicos
        var entity = _world.Create(
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
    /// Cria um NPC a partir de template e retorna o ID.
    /// </summary>
    int IEntityFactory.CreateNpc(string templateId, int x, int y)
    {
        var entity = CreateNpc(templateId, x, y);
        return _world.Get<Identity>(entity).UniqueId;
    }

    /// <summary>
    /// Cria um NPC a partir de template e retorna a Entity.
    /// </summary>
    public Entity CreateNpc(string templateId, int x, int y)
    {
        var template = _npcTemplateProvider.Get(templateId);
        if (template == null)
            throw new ArgumentException($"NPC template '{templateId}' not found");

        int entityId = ++_nextEntityId;

        // Criação em etapas para não exceder limite
        var entity = _world.Create(
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
    /// Cria um Pet para um owner (interface).
    /// </summary>
    int IEntityFactory.CreatePet(string templateId, int ownerEntityId, int x, int y)
    {
        var entity = CreatePet(templateId, ownerEntityId, x, y);
        return _world.Get<Identity>(entity).UniqueId;
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

        var entity = _world.Create(
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
    /// Cria um item no chão (interface).
    /// </summary>
    int IEntityFactory.CreateDroppedItem(int itemId, int quantity, int x, int y, int? ownerEntityId)
    {
        var entity = CreateDroppedItem(itemId, quantity, x, y, ownerEntityId);
        return _world.Get<Identity>(entity).UniqueId;
    }

    /// <summary>
    /// Cria um item no chão.
    /// </summary>
    public Entity CreateDroppedItem(int itemId, int quantity, int x, int y, int? ownerEntityId = null)
    {
        int entityId = ++_nextEntityId;

        return _world.Create(
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
