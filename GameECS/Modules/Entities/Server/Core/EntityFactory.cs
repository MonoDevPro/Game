using Arch.Core;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Modules.Navigation.Shared.Components;

namespace GameECS.Modules.Entities.Server.Core;

/// <summary>
/// Factory para criar entidades no mundo.
/// </summary>
public sealed class EntityFactory
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
    /// Cria um Player.
    /// </summary>
    public Entity CreatePlayer(
        int accountId,
        int characterId,
        string name,
        int level,
        int x,
        int y)
    {
        int entityId = ++_nextEntityId;

        // Componentes básicos
        var entity = _world.Create(
            new EntityIdentity
            {
                UniqueId = entityId,
                Type = EntityType.Player,
                TemplateId = 0
            },
            EntityName.Create(name),
            new EntityLevel(level),
            new PlayerOwnership
            {
                AccountId = accountId,
                CharacterId = characterId
            },
            new GridPosition(x, y),
            new Health(100 + level * 10),
            VisibilityConfig.ForPlayer,
            new AreaOfInterest(18),
            new PartyMember()
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
        var entity = _world.Create(
            new EntityIdentity
            {
                UniqueId = entityId,
                Type = EntityType.Npc,
                TemplateId = 0 // Templates usam string id
            },
            EntityName.Create(template.Name),
            new EntityLevel(template.Level),
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

        var entity = _world.Create(
            new EntityIdentity
            {
                UniqueId = entityId,
                Type = EntityType.Pet,
                TemplateId = 0 // Templates usam string id
            },
            EntityName.Create(template.Name),
            new EntityLevel(1),
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

        return _world.Create(
            new EntityIdentity
            {
                UniqueId = entityId,
                Type = EntityType.Item,
                TemplateId = itemId
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
