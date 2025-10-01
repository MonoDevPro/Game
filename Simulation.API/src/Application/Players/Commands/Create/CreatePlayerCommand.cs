using System.Text.RegularExpressions;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Players.Models;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Players.Commands.Create;

public record CreatePlayerCommand(string Name, Gender Gender, Vocation Vocation) : ICommand<PlayerDto>;

public partial class CreatePlayerCommandValidator : AbstractValidator<CreatePlayerCommand>
{
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled)] 
    private static partial Regex NameRegex();
    private const int MaxCharactersPerUser = 3;
    private const int MinNameLength = 3;
    private const int MaxNameLength = 20;
    private const bool IsNameUnique = true;
    
    // Manter as dependências como campos privados para serem acedidas pelos métodos
    private readonly IApplicationDbContext _context;
    private readonly string _userId;

    public CreatePlayerCommandValidator(IApplicationDbContext context, IUser user)
    {
        _userId = user.Id!;
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Player name is required.")
            .MinimumLength(MinNameLength).WithMessage($"Player name must be at least {MinNameLength} characters long.")
            .MaximumLength(MaxNameLength).WithMessage($"Player name must not exceed {MaxNameLength} characters.")
            .Matches(NameRegex()).WithMessage("Name must start with a letter and can only contain letters, numbers, and underscores.")
            .MustAsync(BeUniqueNameAsync).WithMessage("Player name already exists.");
        
        RuleFor(x => x.Gender).IsInEnum().NotEqual(Gender.None).WithMessage("A valid gender must be selected.");
        
        RuleFor(x => x.Vocation).IsInEnum().NotEqual(Vocation.None).WithMessage("A valid vocation must be selected.");
        
        RuleFor(x => x)
            .MustAsync(BeWithinCharacterLimitAsync).WithMessage("You have reached the maximum number of characters allowed (3).");
    }

    /// <summary>
    /// Verifica se o nome do personagem já existe na base de dados.
    /// </summary>
    private async Task<bool> BeUniqueNameAsync(string name, CancellationToken ct)
    {
        return !await _context.Players
            .IgnoreQueryFilters()
            .Where(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            .AnyAsync(ct) == IsNameUnique;
    }

    private async Task<bool> BeWithinCharacterLimitAsync(CreatePlayerCommand command, CancellationToken cancellationToken)
    {
        return await _context.Players
            .Where(p => p.UserId == _userId)
            .CountAsync(cancellationToken) < MaxCharactersPerUser; // Limite de 3 personagens
    }
}

public class CreatePlayerCommandHandler(
    IApplicationDbContext context, 
    IUser user, IMapper map)
    : IRequestHandler<CreatePlayerCommand, PlayerDto>
{
    public async Task<PlayerDto> Handle(CreatePlayerCommand request, CancellationToken ct)
    {
        var player = new Player
        {
            UserId = user.Id!,
            Name = request.Name, 
            Gender = (byte)request.Gender,
            Vocation = (byte)request.Vocation
        };

        context.Players.Add(player);
        var result = await context.SaveChangesAsync(ct);
        
        if (result == 0)
            throw new Exception("Error creating character (no changes saved).");
        
        var p = await context.Players.Where(p => p.Name.Equals(player.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefaultAsync(ct)
        ?? throw new Exception("Error creating character (not found after creation).");
        return map.Map<PlayerDto>(p);
    }
}
