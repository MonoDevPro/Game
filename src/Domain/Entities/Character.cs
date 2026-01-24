using Domain.Entities.Common;

public record AttackResult(int IntendedDamage, DamageResult DamageResult);
public record DamageResult(int ActualDamage, int RemainingHealth, bool Died);

}
    }
            throw new InvalidOperationException("Character is dead and cannot perform this action.");
        if (IsDead)
    {
    private void ThrowIfDead()

    }
            Mana = Mana.Full(newAttributes.MaxMana);
        if (Mana.Current > newAttributes.MaxMana)
            
            Health = Health.Full(newAttributes.MaxHealth);
        if (Health.Current > newAttributes.MaxHealth)
        // Ajusta HP/Mana se os máximos mudaram
        
        Attributes = newAttributes;
        
        ArgumentNullException.ThrowIfNull(newAttributes);
        ThrowIfDead();
    {
    public void UpdateAttributes(CharacterAttributes newAttributes)

    }
        RaiseDomainEvent(new CharacterResurrectedEvent(this));
        
        Mana = Mana.Full(Attributes.MaxMana);
        Health = Health.Full(Attributes.MaxHealth).Reduce(Attributes.MaxHealth - healthToRestore);
        var healthToRestore = (Attributes.MaxHealth * healthPercentage) / 100;

            throw new InvalidOperationException("Character is not dead.");
        if (!IsDead)
    {
    public void Resurrect(int healthPercentage = 50)

    }
        Mana = Mana.Full(Attributes.MaxMana);
        Health = Health.Full(Attributes.MaxHealth);
            
            throw new InvalidOperationException("Cannot restore a dead character. Use Resurrect instead.");
        if (IsDead)
    {
    public void FullRestore()

    }
        return _inventory.Remove(item);
        ArgumentNullException.ThrowIfNull(item);
        ThrowIfDead();
    {
    public bool RemoveItem(Item item)

    }
        _inventory.Add(item);
        ArgumentNullException.ThrowIfNull(item);
        ThrowIfDead();
    {
    public void AddItem(Item item)

    }
               + (Level.Value * 10);
               + Attributes.Dexterity 
               + Attributes.Intelligence 
        return Attributes.Strength 
        // Poder total considerando todos os atributos e nível
    {
    public int CalculateTotalPower()

    }
        return baseDamage;
        var baseDamage = Attributes.Strength + (Level.Value * 2);
        // Dano base = Força + bônus por nível
    {
    public int CalculateAttackDamage()

    }
        return new AttackResult(damage, damageResult);

        RaiseDomainEvent(new CharacterAttackedEvent(this, target, damageResult.ActualDamage));

        var damageResult = target.TakeDamage(damage);
        var damage = CalculateAttackDamage();

            throw new InvalidOperationException("Cannot attack a dead character.");
        if (target.IsDead)

        ThrowIfDead();
        ArgumentNullException.ThrowIfNull(target);
    {
    public AttackResult Attack(Character target)

    }
        Position = newPosition;
        ThrowIfDead();
    {
    public void MoveTo(Position newPosition)

    }
        return true;
        Mana = Mana.Consume(amount);

            return false;
        if (!Mana.HasEnough(amount))
        
        ThrowIfDead();
    {
    public bool ConsumeMana(int amount)

    }
        Mana = Mana.Restore(amount);
        ThrowIfDead();
    {
    public void RestoreMana(int amount)

    }
        Health = Health.Restore(amount);
        ThrowIfDead();
    {
    public void Heal(int amount)

    }
        return new DamageResult(actualDamage, Health.Current, died);

        }
            RaiseDomainEvent(new CharacterDiedEvent(this));
        {
        if (died)

        var died = !previousHealth.IsDead && Health.IsDead;
        var actualDamage = previousHealth.Current - Health.Current;
        
        Health = Health.Reduce(amount);
        var previousHealth = Health;
    {
    public DamageResult TakeDamage(int amount)

    }
        }
            RaiseDomainEvent(new CharacterLeveledUpEvent(this, Level));

            Experience = remainingXp;
            Level = newLevel;

            var remainingXp = Experience.SubtractLevelRequirement(Level);
            var newLevel = Level.Increment();
        {
        while (Experience.CanLevelUp(Level))

        Experience = Experience.Add(amount);
        
        ThrowIfDead();
    {
    public void GainExperience(int amount)

    }
        return character;
        character.RaiseDomainEvent(new CharacterCreatedEvent(character));
        var character = new Character(id, name, attributes);
    {
        CharacterAttributes attributes)
        CharacterName name,
        CharacterId id,
    public static Character Create(

    }
        Position = Position.Origin;
        Mana = Mana.Full(attributes.MaxMana);
        Health = Health.Full(attributes.MaxHealth);
        Attributes = attributes;
        Experience = ExperiencePoints.Zero;
        Level = CharacterLevel.Initial;
        Name = name;
    {
        CharacterAttributes attributes) : base(id)
        CharacterName name,
        CharacterId id,
    private Character(

    private Character() : base(default!) { }

    public bool IsAlive => !IsDead;
    public bool IsDead => Health.IsDead;
    
    public IReadOnlyCollection<Item> Inventory => _inventory.AsReadOnly();
    public Position Position { get; private set; }
    public CharacterAttributes Attributes { get; private set; }
    public Mana Mana { get; private set; }
    public Health Health { get; private set; }
    public ExperiencePoints Experience { get; private set; }
    public CharacterLevel Level { get; private set; }
    public CharacterName Name { get; private set; }

    private readonly List<Item> _inventory = [];
{
public class Character : AggregateRoot<CharacterId>

namespace Domain.Entities;

using Domain.ValueObjects;
using Domain.Events;
