using Arch.Core;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Entities.Shared.Data;

namespace GameECS.Modules.Entities.Server.Core;

/// <summary>
/// Gerencia parties/grupos de players.
/// </summary>
public sealed class PartyManager
{
    private readonly Dictionary<int, Party> _parties = new();
    private int _nextPartyId;

    /// <summary>
    /// Cria uma nova party.
    /// </summary>
    public int CreateParty(Entity leader, PartyConfig? config = null)
    {
        int partyId = ++_nextPartyId;
        var party = new Party(partyId, config ?? PartyConfig.Default);
        party.AddMember(leader);
        _parties[partyId] = party;
        return partyId;
    }

    /// <summary>
    /// Adiciona membro à party.
    /// </summary>
    public bool AddMember(int partyId, Entity entity)
    {
        if (!_parties.TryGetValue(partyId, out var party))
            return false;

        return party.AddMember(entity);
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
    /// Dissolve a party.
    /// </summary>
    public bool DissolveParty(int partyId)
        => _parties.Remove(partyId);

    /// <summary>
    /// Quantidade total de parties ativas.
    /// </summary>
    public int PartyCount => _parties.Count;
}

/// <summary>
/// Representa uma party.
/// </summary>
public sealed class Party
{
    private readonly List<Entity> _members = new();
    private readonly PartyConfig _config;

    public int Id { get; }
    public Entity Leader { get; private set; }
    public PartyConfig Config => _config;
    public IReadOnlyList<Entity> Members => _members;
    public int MemberCount => _members.Count;
    public bool IsFull => _members.Count >= _config.MaxMembers;

    public Party(int id, PartyConfig config)
    {
        Id = id;
        _config = config;
    }

    public bool AddMember(Entity entity)
    {
        if (IsFull || _members.Contains(entity))
            return false;

        if (_members.Count == 0)
            Leader = entity;

        _members.Add(entity);
        return true;
    }

    public bool RemoveMember(Entity entity)
    {
        bool removed = _members.Remove(entity);
        if (removed && entity.Equals(Leader) && _members.Count > 0)
            Leader = _members[0];
        return removed;
    }

    public bool SetLeader(Entity entity)
    {
        if (!_members.Contains(entity))
            return false;
        Leader = entity;
        return true;
    }
}
