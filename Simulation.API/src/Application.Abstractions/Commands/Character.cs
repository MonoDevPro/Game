using Application.Models.Models;
using Application.Models.Security;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Enums;

namespace Application.Models.Commands;

public record CreateCharacterCommand(string Name, Gender Gender, Vocation Vocation) : ICommand<CharacterDto>;
public record DeleteCharacterCommand(int CharacterId) : ICommand<int>;

[Authorize(Policy = Policies.CanPurge)]
public record PurgeCharactersCommand(bool? IsActive = null) : ICommand<int>;
