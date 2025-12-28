namespace Game.Domain.ValueObjects.Character;

/// <summary>
/// Membro de party.
/// </summary>
public struct PartyMember
{
    public int PartyId;
    public bool IsLeader;

    public readonly bool IsInParty => PartyId > 0;
}
