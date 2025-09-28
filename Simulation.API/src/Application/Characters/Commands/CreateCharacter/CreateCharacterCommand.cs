using System.Text.RegularExpressions;
using Application.Models;
using GameWeb.Application.Characters.Models;
using GameWeb.Application.Characters.Specifications;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;
using GameWeb.Domain.Enums;
using GameWeb.Domain.Events;
using GameWeb.Domain.ValueObjects;

namespace GameWeb.Application.Characters.Commands.CreateCharacter;

public record CreateCharacterCommand(string Name, Gender Gender, Vocation Vocation) : ICommand<CharacterDto>;

public partial class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*$")] private static partial Regex NameRegex();
    private const int MaxCharactersPerUser = 3;
    private const int MinNameLength = 3;
    private const int MaxNameLength = 20;
    
    // Manter as dependências como campos privados para serem acedidas pelos métodos
    private readonly IRepository<Character> _characterRepo;
    private readonly string _userId;

    public CreateCharacterCommandValidator(IRepository<Character> characterRepo, IUser user)
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
        var spec = new CharacterByNameSpec(name);
        return !await _characterRepo.AnyBySpecAsync(spec, cancellationToken);
    }

    private async Task<bool> BeWithinCharacterLimitAsync(CreateCharacterCommand command, CancellationToken cancellationToken)
    {
        var spec = new CharactersByOwnerSpec(_userId);
        var count = await _characterRepo.CountBySpecAsync(spec, cancellationToken);
        return count < MaxCharactersPerUser; // Limite de 3 personagens
    }
}

public class CreateCharacterCommandHandler(
    IRepository<Character> characterRepo, 
    IUnitOfWork uow,
    IUser user)
    : IRequestHandler<CreateCharacterCommand, CharacterDto>
{
    public async Task<CharacterDto> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {        
        var character = new Character
        {
            Name = request.Name, 
            Gender = request.Gender, 
            Vocation = request.Vocation, 
            Stats = request.Vocation switch
            {
                Vocation.Warrior => new CharacterStats(0, 150, 150, 20, 1, 0.5f, 1.0f, 3.5f),
                Vocation.Archer => new CharacterStats(0, 100, 100, 15, 5, 0.3f, 0.7f, 4.0f),
                Vocation.Mage => new CharacterStats(0, 80, 80, 25, 4, 1.0f, 2.0f, 3.0f),
                _ => throw new ArgumentOutOfRangeException()
            },
            Position = new Vector2D (5, 5),
            Direction = new Vector2D(0,1),
            OwnerId = user.Id!
        };
        character.AddDomainEvent(new CharacterCreatedEvent(character.Id));
        
        characterRepo.Add(character);
        await uow.SaveChangesAsync(cancellationToken);
        
        return await characterRepo.GetBySpecAsync<CharacterDto>(new CharacterByIdSpec(character.Id), cancellationToken)
               ?? throw new Exception("Error creating character (not found after creation).");
    }
}
