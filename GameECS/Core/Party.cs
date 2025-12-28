using Arch.Core;
using Game.Domain.Party.ValueObjects;

namespace GameECS.Core;

/// <summary>
/// Representa uma party/grupo de jogadores.
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
