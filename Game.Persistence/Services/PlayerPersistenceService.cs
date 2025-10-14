using Game.Domain.Entities;
using Game.Persistence.DTOs;
using Game.Persistence.Interfaces;
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
    /// <summary>
    /// Persiste dados completos do personagem ao desconectar (posição, direção, vitals).
    /// Não persiste inventário para evitar overhead e problemas de sincronização.
    /// </summary>
    public async Task PersistDisconnectAsync(
        DisconnectPersistenceDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ Buscar Character com Stats usando repositório especializado
            var character = await unitOfWork.Characters
                .GetByIdWithStatsAsync(dto.CharacterId, cancellationToken);

            if (character is null)
            {
                logger.LogWarning(
                    "Cannot persist disconnect data for character {CharacterId}: not found",
                    dto.CharacterId);
                return;
            }

            // ✅ Atualizar posição e direção
            character.PositionX = dto.PositionX;
            character.PositionY = dto.PositionY;
            character.DirectionEnum = dto.Direction;
            character.LastUpdatedAt = DateTime.UtcNow;

            character.Stats.CurrentHp = dto.CurrentHp;
            character.Stats.CurrentMp = dto.CurrentMp;

            // ✅ Salvar via Unit of Work
            await unitOfWork.Characters.UpdateAsync(character, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Disconnect data persisted for character {CharacterName} {DisconnectPersistenceDto} ",
                character.Name,
                dto);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting disconnect data for character {CharacterId}",
                dto.CharacterId);
            throw;
        }
    }

    /// <summary>
    /// Persiste apenas a posição e direção do personagem (operação rápida).
    /// </summary>
    public async Task PersistPositionAsync(
        PositionPersistenceDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var character = await unitOfWork.Characters
                .GetByIdAsync(dto.CharacterId, cancellationToken);

            if (character is null)
            {
                logger.LogWarning(
                    "Cannot persist position for character {CharacterId}: not found",
                    dto.CharacterId);
                return;
            }

            character.PositionX = dto.PositionX;
            character.PositionY = dto.PositionY;
            character.DirectionEnum = dto.Direction;
            character.LastUpdatedAt = DateTime.UtcNow;

            await unitOfWork.Characters.UpdateAsync(character, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Position persisted for character {CharacterId}: ({X},{Y}) facing {Direction}",
                dto.CharacterId,
                dto.PositionX,
                dto.PositionY,
                dto.Direction);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting position for character {CharacterId}",
                dto.CharacterId);
            throw;
        }
    }

    /// <summary>
    /// Persiste apenas os vitals (HP/MP) do personagem (operação rápida).
    /// </summary>
    public async Task PersistVitalsAsync(
        VitalsPersistenceDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var character = await unitOfWork.Characters
                .GetByIdWithStatsAsync(dto.CharacterId, cancellationToken);

            if (character?.Stats is null)
            {
                logger.LogWarning(
                    "Cannot persist vitals for character {CharacterId}: stats not found",
                    dto.CharacterId);
                return;
            }

            character.Stats.CurrentHp = dto.CurrentHp;
            character.Stats.CurrentMp = dto.CurrentMp;

            await unitOfWork.Characters.UpdateAsync(character, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Vitals persisted for character {CharacterId}: HP {HP}, MP {MP}",
                dto.CharacterId,
                dto.CurrentHp,
                dto.CurrentMp);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting vitals for character {CharacterId}",
                dto.CharacterId);
            throw;
        }
    }

    /// <summary>
    /// Persiste stats completos do personagem.
    /// </summary>
    public async Task PersistStatsAsync(
        StatsPersistenceDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var character = await unitOfWork.Characters
                .GetByIdWithStatsAsync(dto.CharacterId, cancellationToken);

            if (character?.Stats is null)
            {
                logger.LogWarning(
                    "Cannot persist stats for character {CharacterId}: stats not found",
                    dto.CharacterId);
                return;
            }

            // ✅ Atualizar todos os stats
            character.Stats.Level = dto.Level;
            character.Stats.Experience = dto.Experience;
            character.Stats.BaseStrength = dto.BaseStrength;
            character.Stats.BaseDexterity = dto.BaseDexterity;
            character.Stats.BaseIntelligence = dto.BaseIntelligence;
            character.Stats.BaseConstitution = dto.BaseConstitution;
            character.Stats.BaseSpirit = dto.BaseSpirit;
            character.Stats.CurrentHp = dto.CurrentHp;
            character.Stats.CurrentMp = dto.CurrentMp;

            await unitOfWork.Characters.UpdateAsync(character, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Stats persisted for character {CharacterId}: Level {Level}, Experience {Experience}",
                dto.CharacterId,
                dto.Level,
                dto.Experience);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting stats for character {CharacterId}",
                dto.CharacterId);
            throw;
        }
    }

    /// <summary>
    /// Persiste inventário completo do personagem (operação pesada).
    /// </summary>
    public async Task PersistInventoryAsync(
        InventoryPersistenceDto dto,
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
                if (slotDto.ItemId is null || slotDto.Quantity <= 0)
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
            
            await unitOfWork.Characters.UpdateAsync(character, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Inventory persisted for character {CharacterId}: {SlotCount} slots",
                dto.CharacterId,
                dto.Slots.Count);
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