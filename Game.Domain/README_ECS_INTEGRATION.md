# 🎯 Integração Domain Events → ArchECS

## Visão Geral

Este documento explica como converter Domain Events do DDD em componentes e sistemas do ArchECS.

## Arquitetura

```
Domain Layer (DDD)          ECS Layer (ArchECS)
┌─────────────────┐         ┌──────────────────┐
│ BaseEntity      │────────▶│ Entity (ID)      │
│ - DomainEvents  │         │ - Components     │
└─────────────────┘         └──────────────────┘
        │                            │
        ▼                            ▼
┌─────────────────┐         ┌──────────────────┐
│ IDomainEvent    │────────▶│ Event Component  │
│ (records)       │         │ (struct)         │
└─────────────────┘         └──────────────────┘
```

## Fluxo de Conversão

### 1. Detectar Domain Events

```csharp
// Após operação de domínio
character.TryPromoteTo(VocationType.Knight);

// Eventos foram adicionados internamente
foreach (var evt in character.DomainEvents)
{
    ProcessDomainEvent(world, entityId, evt);
}

// Limpar após processar
character.ClearDomainEvents();
```

### 2. Converter para Componentes ECS

```csharp
public static void ProcessDomainEvent(World world, Entity entity, IDomainEvent domainEvent)
{
    switch (domainEvent)
    {
        case CharacterLeveledUpEvent levelUp:
            world.AddComponent(entity, new LevelUpEventComponent
            {
                OldLevel = levelUp.OldLevel,
                NewLevel = levelUp.NewLevel,
                ExperienceGained = levelUp.ExperienceGained
            });
            break;
            
        case ItemEquippedEvent equipped:
            world.AddComponent(entity, new ItemEquippedEventComponent
            {
                ItemId = equipped.ItemId,
                Slot = equipped.Slot
            });
            break;
            
        case DamageTakenEvent damage:
            world.AddComponent(entity, new DamageTakenEventComponent
            {
                Damage = damage.Damage,
                AttackerId = damage.AttackerId,
                IsCritical = damage.IsCritical,
                IsLethal = damage.IsLethal
            });
            break;
    }
}
```

### 3. Processar com Sistemas ECS

```csharp
// Sistema reativo que processa level ups
public class LevelUpSystem : ISystem
{
    public void Update(World world)
    {
        var query = world.Query<LevelUpEventComponent>().Build();
        
        foreach (var entity in query)
        {
            var levelUpEvent = entity.Get<LevelUpEventComponent>();
            
            // Processar efeitos do level up
            UpdateStats(entity, levelUpEvent);
            PlayLevelUpEffect(entity);
            NotifyUI(levelUpEvent);
            
            // Remover componente de evento após processar
            entity.Remove<LevelUpEventComponent>();
        }
    }
}
```

## Padrões de Uso

### Adicionar Eventos em Entidades

```csharp
public class Character : BaseEntity
{
    public void GainExperience(long amount, string source)
    {
        var oldLevel = Level;
        Experience += amount;
        
        // Adicionar evento de experiência ganha
        AddDomainEvent(new ExperienceGainedEvent
        {
            CharacterId = Id,
            Amount = amount,
            Source = source
        });
        
        // Se subiu de nível, adicionar evento adicional
        if (Level > oldLevel)
        {
            AddDomainEvent(new CharacterLeveledUpEvent
            {
                CharacterId = Id,
                OldLevel = oldLevel,
                NewLevel = Level,
                ExperienceGained = amount
            });
        }
    }
}
```

### Processar Eventos no Application Layer

```csharp
public class GameLoop
{
    private readonly World _world;
    private readonly ICharacterRepository _characterRepo;
    
    public void ProcessTurn()
    {
        // 1. Executar lógica de domínio
        var character = _characterRepo.GetById(characterId);
        character.GainExperience(100, "Quest Completed");
        
        // 2. Salvar mudanças
        _characterRepo.Update(character);
        
        // 3. Processar eventos de domínio
        foreach (var evt in character.DomainEvents)
        {
            ProcessDomainEvent(_world, GetEntityId(character.Id), evt);
        }
        
        // 4. Limpar eventos
        character.ClearDomainEvents();
        
        // 5. Executar sistemas ECS
        _world.Update();
    }
}
```

## Eventos Disponíveis

### CharacterEvents

- **CharacterLeveledUpEvent**: Personagem subiu de nível
- **ExperienceGainedEvent**: Personagem ganhou experiência
- **CharacterPromotedEvent**: Personagem mudou de vocação

### ItemEvents

- **ItemEquippedEvent**: Item foi equipado
- **ItemUnequippedEvent**: Item foi desequipado

### CombatEvents

- **DamageTakenEvent**: Entidade recebeu dano

## Benefícios da Arquitetura

1. **Separação de Responsabilidades**: Domain layer mantém lógica de negócio, ECS cuida de rendering e sistemas reativos
2. **Testabilidade**: Domain events podem ser testados independentemente do ECS
3. **Flexibilidade**: Novos sistemas ECS podem reagir a eventos existentes sem modificar domínio
4. **Performance**: ECS processa eventos em batch de forma eficiente
5. **Auditoria**: Todos os eventos de domínio são rastreáveis

## Próximos Passos

1. Implementar componentes de evento no GameECS
2. Criar sistemas reativos para processar eventos
3. Adicionar mais eventos conforme necessário
4. Implementar persistência de eventos para event sourcing (opcional)
