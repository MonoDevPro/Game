using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Players;

/// <summary>
/// Serviço responsável pela persistência de dados de personagens no banco de dados.
/// Gerencia sincronização de stats, inventário e equipamentos.
/// 
/// Autor: MonoDevPro
/// Data: 2025-10-12 22:34:09
/// </summary>
public sealed class PlayerPersistenceService(GameDbContext dbContext, ILogger<PlayerPersistenceService> logger)
{
    /// <summary>
    /// Persiste dados básicos do personagem ao desconectar (posição, direção, vitals).
    /// Não persiste inventário para evitar overhead e problemas de sincronização.
    /// </summary>
    public async Task PersistDisconnectAsync(
        int characterId,
        int positionX,
        int positionY,
        DirectionEnum direction,
        int? currentHp = null,
        int? currentMp = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ Buscar apenas Character e Stats (sem Inventory/Equipment)
            var character = await dbContext.Characters
                .Include(c => c.Stats)
                .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

            if (character is null)
            {
                logger.LogWarning(
                    "Cannot persist disconnect data for character {CharacterId}: not found",
                    characterId);
                return;
            }

            // ✅ Atualizar posição e direção
            character.PositionX = positionX;
            character.PositionY = positionY;
            character.DirectionEnum = direction;
            character.LastUpdatedAt = DateTime.UtcNow;

            // ✅ Atualizar vitals se fornecidos e Stats existir
            if (currentHp.HasValue && currentMp.HasValue)
            {
                character.Stats.CurrentHp = currentHp.Value;
                character.Stats.CurrentMp = currentMp.Value;
            }

            // ✅ Salvar alterações
            dbContext.Characters.Update(character);
            dbContext.Stats.Update(character.Stats);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Disconnect data persisted for character {CharacterId} ({CharacterName}): Position ({X},{Y}), Direction: {Direction}, HP: {HP}, MP: {MP}",
                characterId,
                character.Name,
                positionX,
                positionY,
                direction,
                currentHp ?? character.Stats?.CurrentHp ?? 0,
                currentMp ?? character.Stats?.CurrentMp ?? 0);
        }
        catch (DbUpdateException dbEx)
        {
            logger.LogError(
                dbEx,
                "Database error while persisting disconnect data for character {CharacterId}",
                characterId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while persisting disconnect data for character {CharacterId}",
                characterId);
        }
    }

    /// <summary>
    /// Persiste os stats do personagem.
    /// </summary>
    private static void PersistStats(Character snapshot, Character character)
    {
        character.Stats.Level = snapshot.Stats.Level;
        character.Stats.Experience = snapshot.Stats.Experience;
        character.Stats.BaseStrength = snapshot.Stats.BaseStrength;
        character.Stats.BaseDexterity = snapshot.Stats.BaseDexterity;
        character.Stats.BaseIntelligence = snapshot.Stats.BaseIntelligence;
        character.Stats.BaseConstitution = snapshot.Stats.BaseConstitution;
        character.Stats.BaseSpirit = snapshot.Stats.BaseSpirit;
        character.Stats.CurrentHp = snapshot.Stats.CurrentHp;
        character.Stats.CurrentMp = snapshot.Stats.CurrentMp;
    }

    /// <summary>
    /// Persiste o inventário do personagem, sincronizando slots.
    /// </summary>
    private void PersistInventory(Character snapshot, Character character)
    {
        // ✅ Garantir que as coleções existem
        character.Inventory.Slots ??= new List<InventorySlot>();
        
        var snapshotSlots = snapshot.Inventory.Slots ?? Array.Empty<InventorySlot>();

        // ✅ Se não há slots no snapshot e não há slots no character, nada a fazer
        if (snapshotSlots.Count == 0 && character.Inventory.Slots.Count == 0)
        {
            logger.LogDebug(
                "No inventory slots to persist for character {CharacterId}",
                character.Id);
            return;
        }

        // ✅ Criar dicionário de slots existentes
        var existingSlots = character.Inventory.Slots.ToDictionary(slot => slot.SlotIndex);

        // ✅ Processar slots do snapshot
        foreach (var slotSnapshot in snapshotSlots)
        {
            // ✅ Slot vazio ou inválido - remover se existir
            if (slotSnapshot.ItemId is null || slotSnapshot.Quantity <= 0)
            {
                if (existingSlots.TryGetValue(slotSnapshot.SlotIndex, out var slotToRemove))
                {
                    character.Inventory.Slots.Remove(slotToRemove);
                    dbContext.InventorySlots.Remove(slotToRemove);
                    existingSlots.Remove(slotSnapshot.SlotIndex);
                    
                    logger.LogDebug(
                        "Removed empty slot {SlotIndex} for character {CharacterId}",
                        slotSnapshot.SlotIndex,
                        character.Id);
                }

                continue;
            }

            // ✅ Slot existe - atualizar ou substituir
            if (existingSlots.TryGetValue(slotSnapshot.SlotIndex, out var existingSlot))
            {
                // Mesmo item - apenas atualizar quantidade
                if (existingSlot.ItemId == slotSnapshot.ItemId)
                {
                    existingSlot.Quantity = slotSnapshot.Quantity;
                    existingSlot.IsActive = slotSnapshot.IsActive;
                    
                    logger.LogDebug(
                        "Updated slot {SlotIndex} for character {CharacterId}: Item {ItemId}, Quantity {Quantity}",
                        slotSnapshot.SlotIndex,
                        character.Id,
                        slotSnapshot.ItemId,
                        slotSnapshot.Quantity);
                }
                // Item diferente - remover antigo e criar novo
                else
                {
                    character.Inventory.Slots.Remove(existingSlot);
                    dbContext.InventorySlots.Remove(existingSlot);

                    var newSlot = CreateSlot(character.Inventory.Id, character.Inventory, slotSnapshot);
                    character.Inventory.Slots.Add(newSlot);
                    
                    logger.LogDebug(
                        "Replaced slot {SlotIndex} for character {CharacterId}: Old Item {OldItemId} -> New Item {NewItemId}",
                        slotSnapshot.SlotIndex,
                        character.Id,
                        existingSlot.ItemId,
                        slotSnapshot.ItemId);
                }

                existingSlots.Remove(slotSnapshot.SlotIndex);
            }
            // ✅ Slot não existe - criar novo
            else
            {
                var newSlot = CreateSlot(character.Inventory.Id, character.Inventory, slotSnapshot);
                character.Inventory.Slots.Add(newSlot);
                
                logger.LogDebug(
                    "Added new slot {SlotIndex} for character {CharacterId}: Item {ItemId}, Quantity {Quantity}",
                    slotSnapshot.SlotIndex,
                    character.Id,
                    slotSnapshot.ItemId,
                    slotSnapshot.Quantity);
            }
        }

        // ✅ Remover slots que não existem mais no snapshot
        foreach (var leftover in existingSlots.Values)
        {
            character.Inventory.Slots.Remove(leftover);
            dbContext.InventorySlots.Remove(leftover);
            
            logger.LogDebug(
                "Removed leftover slot {SlotIndex} for character {CharacterId}",
                leftover.SlotIndex,
                character.Id);
        }
    }

    /// <summary>
    /// Cria um novo slot de inventário a partir de um snapshot.
    /// </summary>
    private static InventorySlot CreateSlot(int inventoryId, Inventory inventory, InventorySlot snapshot)
    {
        return new InventorySlot
        {
            SlotIndex = snapshot.SlotIndex,
            Quantity = snapshot.Quantity,
            ItemId = snapshot.ItemId,
            InventoryId = inventoryId,
            Inventory = inventory,
            IsActive = snapshot.IsActive
        };
    }

    /// <summary>
    /// Persiste apenas a posição e direção do personagem (rápido).
    /// </summary>
    public async Task PersistPositionAsync(
        int characterId,
        int positionX,
        int positionY,
        DirectionEnum direction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var character = await dbContext.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

            if (character is null)
            {
                logger.LogWarning(
                    "Cannot persist position for character {CharacterId}: not found",
                    characterId);
                return;
            }

            character.PositionX = positionX;
            character.PositionY = positionY;
            character.DirectionEnum = direction;
            character.LastUpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Position persisted for character {CharacterId}: ({X},{Y}) facing {Direction}",
                characterId,
                positionX,
                positionY,
                direction);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting position for character {CharacterId}",
                characterId);
        }
    }

    /// <summary>
    /// Persiste apenas os vitals (HP/MP) do personagem (rápido).
    /// </summary>
    public async Task PersistVitalsAsync(
        int characterId,
        int currentHp,
        int currentMp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await dbContext.Stats
                .FirstOrDefaultAsync(s => s.CharacterId == characterId, cancellationToken);

            if (stats is null)
            {
                logger.LogWarning(
                    "Cannot persist vitals for character {CharacterId}: stats not found",
                    characterId);
                return;
            }

            stats.CurrentHp = currentHp;
            stats.CurrentMp = currentMp;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Vitals persisted for character {CharacterId}: HP {HP}, MP {MP}",
                characterId,
                currentHp,
                currentMp);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error persisting vitals for character {CharacterId}",
                characterId);
        }
    }
}