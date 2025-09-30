using System.Text.RegularExpressions;
using Application.Abstractions;
using GameWeb.Application.Characters.Services;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Commands.Create;

public record CreateCharacterCommand(string Name, Gender Gender, Vocation Vocation) : ICommand<PlayerData>;

public partial class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled)] 
    private static partial Regex NameRegex();
    private const int MaxCharactersPerUser = 3;
    private const int MinNameLength = 3;
    private const int MaxNameLength = 20;
    
    // Manter as dependências como campos privados para serem acedidas pelos métodos
    private readonly IPlayerRepository _characterRepo;
    private readonly string _userId;

    public CreateCharacterCommandValidator(IPlayerRepository characterRepo, IUser user)
    {
        _userId = user.Id!;
        _characterRepo = characterRepo;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Character name is required.")
            .MinimumLength(MinNameLength).WithMessage($"Character name must be at least {MinNameLength} characters long.")
            .MaximumLength(MaxNameLength).WithMessage($"Character name must not exceed {MaxNameLength} characters.")
            .Matches(NameRegex()).WithMessage("Name must start with a letter and can only contain letters, numbers, and underscores.")
            .MustAsync(BeUniqueNameAsync).WithMessage("Character name already exists.");
        
        RuleFor(x => x.Gender).IsInEnum().NotEqual(Gender.None).WithMessage("A valid gender must be selected.");
        
        RuleFor(x => x.Vocation).IsInEnum().NotEqual(Vocation.None).WithMessage("A valid vocation must be selected.");
        
        RuleFor(x => x)
            .MustAsync(BeWithinCharacterLimitAsync).WithMessage("You have reached the maximum number of characters allowed (3).");
    }

    /// <summary>
    /// Verifica se o nome do personagem já existe na base de dados.
    /// </summary>
    private async Task<bool> BeUniqueNameAsync(string name, CancellationToken cancellationToken)
    {
        return !await _characterRepo.ExistPlayerAsync(name, true, cancellationToken);
    }

    private async Task<bool> BeWithinCharacterLimitAsync(CreateCharacterCommand command, CancellationToken cancellationToken)
    {
        var count = await _characterRepo.CountMyPlayersAsync(_userId, true, cancellationToken);
        return count < MaxCharactersPerUser; // Limite de 3 personagens
    }
}

public class CreateCharacterCommandHandler(
    IPlayerRepository characterRepo, 
    IUser user, IMapper map)
    : IRequestHandler<CreateCharacterCommand, PlayerData>
{
    public async Task<PlayerData> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        var player = new Player
        {
            UserId = user.Id!,
            Name = request.Name, 
            Gender = (byte)request.Gender,
            Vocation = (byte)request.Vocation
        };
        
        characterRepo.CreatePlayer(player);
        var result = await characterRepo.SaveChangesAsync(cancellationToken);
        if (result == 0)
            throw new Exception("Error creating character (no changes saved).");
        
        var character = await characterRepo.GetPlayerAsync(player.Name, true, false, cancellationToken)
        ?? throw new Exception("Error creating character (not found after creation).");
        
        return map.Map<PlayerData>(character);
    }
}
