using Arch.Core;
using Game.Domain.Party.Interfaces;
using Game.Domain.Party.ValueObjects;
using Game.Domain.ValueObjects.Identitys;

namespace GameECS.Core;

/// <summary>
/// Gerencia parties/grupos de players.
/// </summary>
public sealed class PartyManager : IPartyManager
{
    private readonly World _world;
    private readonly Dictionary<int, Party> _parties = new();
    private readonly Dictionary<int, Entity> _entityCache = new();
    private int _nextPartyId;

    public PartyManager(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Cria uma nova party a partir do ID de entidade.
    /// </summary>
    public int CreateParty(int leaderEntityId)
    {
        if (!TryGetEntity(leaderEntityId, out var leader))
            return 0;

        int partyId = ++_nextPartyId;
        var party = new Party(partyId, PartyConfig.Default);
        party.AddMember(leader);
        _parties[partyId] = party;
        return partyId;
    }

    /// <summary>
    /// Cria uma nova party com configuração customizada.
    /// </summary>
    public int CreateParty(Entity leader, PartyConfig? config = null)
    {
        int partyId = ++_nextPartyId;
        var party = new Party(partyId, config ?? PartyConfig.Default);
        party.AddMember(leader);
        _parties[partyId] = party;
        CacheEntity(leader);
        return partyId;
    }

    /// <summary>
    /// Adiciona membro à party por ID.
    /// </summary>
    public bool AddMember(int partyId, int entityId)
    {
        if (!_parties.TryGetValue(partyId, out var party))
            return false;

        if (!TryGetEntity(entityId, out var entity))
            return false;

        return party.AddMember(entity);
    }

    /// <summary>
    /// Adiciona membro à party.
    /// </summary>
    public bool AddMember(int partyId, Entity entity)
    {
        if (!_parties.TryGetValue(partyId, out var party))
            return false;

        CacheEntity(entity);
        return party.AddMember(entity);
    }

    /// <summary>
    /// Remove membro da party por ID.
    /// </summary>
    public bool RemoveMember(int partyId, int entityId)
    {
        if (!_parties.TryGetValue(partyId, out var party))
            return false;

        if (!TryGetEntity(entityId, out var entity))
            return false;

        bool removed = party.RemoveMember(entity);
        if (party.MemberCount == 0)
            _parties.Remove(partyId);

        return removed;
    }

    /// <summary>
    /// Remove membro da party.
    /// </summary>
    public bool RemoveMember(int partyId, Entity entity)
    {
        if (!_parties.TryGetValue(partyId, out var party))
            return false;

        bool removed = party.RemoveMember(entity);

        // Dissolve party vazia
        if (party.MemberCount == 0)
            _parties.Remove(partyId);

        return removed;
    }

    /// <summary>
    /// Obtém a party.
    /// </summary>
    public Party? GetParty(int partyId)
        => _parties.TryGetValue(partyId, out var party) ? party : null;

    /// <summary>
    /// Obtém membros da party.
    /// </summary>
    public IReadOnlyList<Entity> GetMembers(int partyId)
        => _parties.TryGetValue(partyId, out var party)
            ? party.Members
            : Array.Empty<Entity>();

    /// <summary>
    /// Obtém IDs dos membros da party.
    /// </summary>
    public IReadOnlyList<int> GetMemberIds(int partyId)
    {
        if (!_parties.TryGetValue(partyId, out var party))
            return Array.Empty<int>();

        var ids = new List<int>(party.MemberCount);
        foreach (var member in party.Members)
        {
            if (_world.Has<Identity>(member))
            {
                ref var identity = ref _world.Get<Identity>(member);
                ids.Add(identity.UniqueId);
            }
        }
        return ids;
    }

    /// <summary>
    /// Dissolve a party.
    /// </summary>
    public bool DissolveParty(int partyId)
        => _parties.Remove(partyId);

    /// <summary>
    /// Quantidade total de parties ativas.
    /// </summary>
    public int PartyCount => _parties.Count;

    private bool TryGetEntity(int entityId, out Entity entity)
    {
        if (_entityCache.TryGetValue(entityId, out entity))
            return _world.IsAlive(entity);

        // Busca na world
        var query = new QueryDescription().WithAll<Identity>();
        bool found = false;
        Entity foundEntity = default;

        _world.Query(in query, (Entity e, ref Identity id) =>
        {
            if (id.UniqueId == entityId)
            {
                foundEntity = e;
                found = true;
            }
        });

        if (found)
        {
            entity = foundEntity;
            _entityCache[entityId] = entity;
        }

        return found;
    }

    private void CacheEntity(Entity entity)
    {
        if (_world.Has<Identity>(entity))
        {
            ref var identity = ref _world.Get<Identity>(entity);
            _entityCache[identity.UniqueId] = entity;
        }
    }
}
