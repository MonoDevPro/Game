using Arch.Core;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Entities.Data;
using GameECS.Shared.Navigation.Components;

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
        var entity = world.Create(
            new Identity
            {
                UniqueId = entityId,
                Type = EntityType.Player,
            },
            Name.Create(name),
            new Level(level),
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
        var entity = world.Create(
            new Identity
            {
                UniqueId = entityId,
                Type = EntityType.Npc,
            },
            Name.Create(template.Name),
            new Level(template.Level),
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
            new Level(1),
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
