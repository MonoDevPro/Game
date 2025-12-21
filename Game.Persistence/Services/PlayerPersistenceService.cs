using Game.Domain.Entities;
using Game.DTOs.Persistence;
using Game.Persistence.Interfaces;
using Game.Persistence.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Game.Persistence.Services;

/// <summary>
/// Serviço responsável pela persistência de dados de personagens no banco de dados.
/// ⚠️ INTERNAL: Use IPlayerPersistenceService ao invés desta classe concreta.
/// Autor: MonoDevPro
/// Data: 2025-10-13 20:30:00
/// </summary>
internal sealed class PlayerPersistenceService(
    IUnitOfWork unitOfWork,
    ILogger<PlayerPersistenceService> logger)
    : IPlayerPersistenceService
{
    private async Task PersistAsync(
        int characterId,
        Func<ICharacterRepository, CancellationToken, Task<Character?>> fetch,
        Func<Character, bool> apply,
        string context,
        CancellationToken cancellationToken)
    {
        try
        {
            var character = await fetch(unitOfWork.Characters, cancellationToken);

            if (character is null)
            {
                logger.LogWarning(
                    "Cannot persist {Context} for character {CharacterId}: not found",
                    context,
                    characterId);
                return;
            }

            if (!apply(character))
                return;

            await SaveCharacterAsync(character, context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting {Context} for character {CharacterId}",
                context,
                characterId);
            throw;
        }
    }

    private static void ApplyPosition(Character character, PositionState position, bool updateTimestamp)
    {
        character.PositionX = position.PositionX;
        character.PositionY = position.PositionY;
        character.PositionZ = position.PositionZ;

        if (updateTimestamp)
            character.LastUpdatedAt = DateTime.UtcNow;
    }

    private async Task SaveCharacterAsync(Character character, string context, CancellationToken cancellationToken, Action? extraLog = null)
    {
        await unitOfWork.Characters.UpdateAsync(character, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Context} persisted for character {CharacterId}",
            context,
            character.Id);

        extraLog?.Invoke();
    }

    /// <summary>
    /// Persiste dados completos do personagem ao desconectar (posição, direção, vitals).
    /// Não persiste inventário para evitar overhead e problemas de sincronização.
    /// </summary>
    public async Task PersistDisconnectAsync(
        DisconnectPersistenceDto dto,
        CancellationToken cancellationToken = default)
    {
        await PersistAsync(
            dto.CharacterId,
            (repo, ct) => repo.GetByIdWithStatsAsync(dto.CharacterId, ct),
            character =>
            {
                ApplyPosition(character, 
                    new PositionState(
                        dto.PositionX,
                        dto.PositionY, 
                        dto.PositionZ,
                        dto.DirX,
                        dto.DirY), updateTimestamp: true);
                character.Stats.CurrentHp = dto.CurrentHp;
                character.Stats.CurrentMp = dto.CurrentMp;
                return true;
            },
            context: "disconnect data",
            cancellationToken);
    }

    /// <summary>
    /// Persiste apenas os vitals (HP/MP) do personagem (operação rápida).
    /// </summary>
    public async Task PersistVitalsAsync(
        VitalsState dto,
        CancellationToken cancellationToken = default)
    {
        await PersistAsync(
            dto.CharacterId,
            (repo, ct) => repo.GetByIdWithStatsAsync(dto.CharacterId, ct),
            character =>
            {
                character.Stats.CurrentHp = dto.CurrentHp;
                character.Stats.CurrentMp = dto.CurrentMp;
                return true;
            },
            context: "vitals",
            cancellationToken);
    }

    /// <summary>
    /// Persiste stats completos do personagem.
    /// </summary>
    public async Task PersistStatsAsync(
        StatsState dto,
        CancellationToken cancellationToken = default)
    {
        await PersistAsync(
            dto.CharacterId,
            (repo, ct) => repo.GetByIdWithStatsAsync(dto.CharacterId, ct),
            character =>
            {
                character.Stats.Level = dto.Level;
                character.Stats.Experience = dto.Experience;
                character.Stats.BaseStrength = dto.BaseStrength;
                character.Stats.BaseDexterity = dto.BaseDexterity;
                character.Stats.BaseIntelligence = dto.BaseIntelligence;
                character.Stats.BaseConstitution = dto.BaseConstitution;
                character.Stats.BaseSpirit = dto.BaseSpirit;
                character.Stats.CurrentHp = dto.CurrentHp;
                character.Stats.CurrentMp = dto.CurrentMp;
                return true;
            },
            context: "stats",
            cancellationToken);
    }

    /// <summary>
    /// Persiste inventário completo do personagem (operação pesada).
    /// </summary>
    public async Task PersistInventoryAsync(
        InventoryState dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var character = await unitOfWork.Characters
                .GetByIdWithStatsAndInventoryAsync(dto.CharacterId, cancellationToken);

            if (character?.Inventory is null)
            {
                logger.LogWarning(
                    "Cannot persist inventory for character {CharacterId}: inventory not found",
                    dto.CharacterId);
                return;
            }

            // ✅ Garantir que a coleção de slots existe
            character.Inventory.Slots ??= new List<InventorySlot>();

            // ✅ Criar dicionário de slots existentes
            var existingSlots = character.Inventory.Slots.ToDictionary(slot => slot.SlotIndex);

            // ✅ Processar slots do DTO
            foreach (var slotDto in dto.Slots)
            {
                // Slot vazio - remover se existir
                if (slotDto.ItemId <= 0 || slotDto.Quantity <= 0)
                {
                    if (existingSlots.TryGetValue(slotDto.SlotIndex, out var slotToRemove))
                    {
                        character.Inventory.Slots.Remove(slotToRemove);
                        existingSlots.Remove(slotDto.SlotIndex);
                    }
                    continue;
                }

                // Slot existe - atualizar ou substituir
                if (existingSlots.TryGetValue(slotDto.SlotIndex, out var existingSlot))
                {
                    // Mesmo item - apenas atualizar quantidade
                    if (existingSlot.ItemId == slotDto.ItemId)
                    {
                        existingSlot.Quantity = slotDto.Quantity;
                        existingSlot.IsActive = slotDto.IsActive;
                    }
                    // Item diferente - remover antigo e criar novo
                    else
                    {
                        character.Inventory.Slots.Remove(existingSlot);

                        var newSlot = new InventorySlot
                        {
                            SlotIndex = slotDto.SlotIndex,
                            Quantity = slotDto.Quantity,
                            ItemId = slotDto.ItemId,
                            InventoryId = character.Inventory.Id,
                            Inventory = character.Inventory,
                            IsActive = slotDto.IsActive
                        };
                        character.Inventory.Slots.Add(newSlot);
                    }

                    existingSlots.Remove(slotDto.SlotIndex);
                }
                // Slot não existe - criar novo
                else
                {
                    var newSlot = new InventorySlot
                    {
                        SlotIndex = slotDto.SlotIndex,
                        Quantity = slotDto.Quantity,
                        ItemId = slotDto.ItemId,
                        InventoryId = character.Inventory.Id,
                        Inventory = character.Inventory,
                        IsActive = slotDto.IsActive
                    };
                    character.Inventory.Slots.Add(newSlot);
                }
            }

            // ✅ Remover slots que não existem mais no DTO
            foreach (var leftover in existingSlots.Values)
                character.Inventory.Slots.Remove(leftover);
            
            await SaveCharacterAsync(
                character,
                context: "inventory",
                cancellationToken,
                () => logger.LogInformation(
                    "Inventory persisted for character {CharacterId}: {SlotCount} slots",
                    dto.CharacterId,
                    dto.Slots.Count));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting inventory for character {CharacterId}",
                dto.CharacterId);
            throw;
        }
    }
}