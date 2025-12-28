namespace Game.Domain.Party.Interfaces;

/// <summary>
/// Interface para gerenciamento de parties/grupos.
/// Implementado na camada ECS.
/// </summary>
public interface IPartyManager
{
    /// <summary>
    /// Cria uma nova party com um líder.
    /// </summary>
    int CreateParty(int leaderEntityId);

    /// <summary>
    /// Adiciona membro à party.
    /// </summary>
    bool AddMember(int partyId, int entityId);

    /// <summary>
    /// Remove membro da party.
    /// </summary>
    bool RemoveMember(int partyId, int entityId);

    /// <summary>
    /// Obtém os IDs dos membros da party.
    /// </summary>
    IReadOnlyList<int> GetMemberIds(int partyId);

    /// <summary>
    /// Dissolve a party.
    /// </summary>
    bool DissolveParty(int partyId);

    /// <summary>
    /// Quantidade total de parties ativas.
    /// </summary>
    int PartyCount { get; }
}
