using Game.Contracts;

namespace Game.Application;

public sealed class WorldUseCases(ICharacterRepository characters, IEnterTicketService tickets)
{
    public async Task<Result<EnterWorldResponse>> EnterWorldAsync(EnterWorldRequest request, CancellationToken ct = default)
    {
        if (!tickets.TryConsumeTicket(request.EnterTicket, out var characterId))
            return Result<EnterWorldResponse>.Fail("Invalid ticket");

        var character = await characters.FindByIdAsync(characterId, ct);
        if (character is null)
            return Result<EnterWorldResponse>.Fail("Character not found");

        var spawn = new WorldSpawnInfo(character.Id, character.Name, character.X, character.Y, character.Floor, character.DirX, character.DirY);
        return Result<EnterWorldResponse>.Ok(new EnterWorldResponse(true, null, spawn));
    }
}
