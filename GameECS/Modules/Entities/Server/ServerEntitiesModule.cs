using Arch.Core;
using GameECS.Modules.Entities.Server.Core;
using GameECS.Modules.Entities.Server.Persistence;
using GameECS.Modules.Entities.Server.Systems;
using GameECS.Modules.Entities.Shared.Data;

namespace GameECS.Modules.Entities.Server;

/// <summary>
/// Módulo de entidades server-side.
/// Gerencia players, NPCs, pets, parties e AOI.
/// </summary>
public sealed class ServerEntitiesModule : IDisposable
{
    private readonly World _world;

    // Core managers
    public EntityFactory EntityFactory { get; }
    public PartyManager PartyManager { get; }
    public AOIManager AOIManager { get; }

    // Persistence
    public PlayerPersistenceService? Persistence { get; }

    // Systems
    public NpcAISystem NpcAISystem { get; }
    public NpcRespawnSystem NpcRespawnSystem { get; }
    public AggroSystem AggroSystem { get; }
    public AOIUpdateSystem AOIUpdateSystem { get; }
    public PetBehaviorSystem PetBehaviorSystem { get; }

    public ServerEntitiesModule(
        World world,
        INpcTemplateProvider? npcTemplateProvider = null,
        IPetTemplateProvider? petTemplateProvider = null,
        IPlayerPersistence? playerPersistence = null,
        IPetPersistence? petPersistence = null)
    {
        _world = world;

        // Inicializa managers
        EntityFactory = new EntityFactory(world, npcTemplateProvider, petTemplateProvider);
        PartyManager = new PartyManager();
        AOIManager = new AOIManager(world);

        // Persistence (opcional)
        if (playerPersistence != null)
        {
            Persistence = new PlayerPersistenceService(world, EntityFactory, playerPersistence, petPersistence);
        }

        // Inicializa sistemas
        NpcAISystem = new NpcAISystem(world);
        NpcRespawnSystem = new NpcRespawnSystem(world);
        AggroSystem = new AggroSystem(world);
        AOIUpdateSystem = new AOIUpdateSystem(world, AOIManager);
        PetBehaviorSystem = new PetBehaviorSystem(world);
    }

    /// <summary>
    /// Atualiza todos os sistemas.
    /// </summary>
    public void Update(long tick)
    {
        // Ordem importa: AI -> Aggro -> Respawn -> Pet -> AOI
        NpcAISystem.Update(tick);
        AggroSystem.Update(tick);
        NpcRespawnSystem.Update(tick);
        PetBehaviorSystem.Update(tick);
        AOIUpdateSystem.Update(tick);
    }

    /// <summary>
    /// Cria um player.
    /// </summary>
    public Arch.Core.Entity CreatePlayer(
        int accountId,
        int characterId,
        string name,
        int level,
        int x,
        int y)
        => EntityFactory.CreatePlayer(accountId, characterId, name, level, x, y);

    /// <summary>
    /// Cria um NPC.
    /// </summary>
    public Arch.Core.Entity CreateNpc(string templateId, int x, int y)
        => EntityFactory.CreateNpc(templateId, x, y);

    /// <summary>
    /// Cria um Pet.
    /// </summary>
    public Arch.Core.Entity CreatePet(string templateId, int ownerEntityId, int x, int y)
        => EntityFactory.CreatePet(templateId, ownerEntityId, x, y);

    /// <summary>
    /// Cria uma party.
    /// </summary>
    public int CreateParty(Arch.Core.Entity leader, PartyConfig? config = null)
        => PartyManager.CreateParty(leader, config);

    /// <summary>
    /// Adiciona membro a uma party.
    /// </summary>
    public bool AddToParty(int partyId, Arch.Core.Entity member)
        => PartyManager.AddMember(partyId, member);

    /// <summary>
    /// Remove membro de uma party.
    /// </summary>
    public bool RemoveFromParty(int partyId, Arch.Core.Entity member)
        => PartyManager.RemoveMember(partyId, member);

    public void Dispose()
    {
        NpcAISystem.Dispose();
        NpcRespawnSystem.Dispose();
        AggroSystem.Dispose();
        AOIUpdateSystem.Dispose();
        PetBehaviorSystem.Dispose();
    }
}
