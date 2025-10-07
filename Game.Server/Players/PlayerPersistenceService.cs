using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.Domain.Entities;
using Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Game.Server.Players;

public sealed class PlayerPersistenceService
{
    private readonly GameDbContext _dbContext;
    private readonly ILogger<PlayerPersistenceService> _logger;

    public PlayerPersistenceService(GameDbContext dbContext, ILogger<PlayerPersistenceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task PersistAsync(Character snapshot, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            var character = await _dbContext.Characters
                .Include(c => c.Stats)
                .Include(c => c.Inventory)
                    .ThenInclude(i => i.Slots)
                .FirstOrDefaultAsync(c => c.Id == snapshot.Id, cancellationToken);
            if (character is null)
            {
                return;
            }

            character.PositionX = snapshot.PositionX;
            character.PositionY = snapshot.PositionY;
            character.DirectionEnum = snapshot.DirectionEnum;
            character.LastUpdatedAt = DateTime.UtcNow;

            PersistStats(snapshot, character);
            PersistInventory(snapshot, character);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist character {CharacterId}", snapshot.Id);
        }
    }

    private static void PersistStats(Character snapshot, Character character)
    {
        if (character.Stats is null || snapshot.Stats is null)
        {
            return;
        }

        character.Stats.Level = snapshot.Stats.Level;
        character.Stats.Experience = snapshot.Stats.Experience;
        character.Stats.BaseStrength = snapshot.Stats.BaseStrength;
        character.Stats.BaseDexterity = snapshot.Stats.BaseDexterity;
        character.Stats.BaseIntelligence = snapshot.Stats.BaseIntelligence;
        character.Stats.BaseConstitution = snapshot.Stats.BaseConstitution;
        character.Stats.BaseSpirit = snapshot.Stats.BaseSpirit;
        character.Stats.CurrentHp = snapshot.Stats.CurrentHp;
        character.Stats.CurrentMp = snapshot.Stats.CurrentMp;
        character.Stats.LastUpdatedAt = DateTime.UtcNow;
    }

    private void PersistInventory(Character snapshot, Character character)
    {
        if (character.Inventory is null || snapshot.Inventory is null)
        {
            return;
        }

        character.Inventory.LastUpdatedAt = DateTime.UtcNow;

        var existingSlots = character.Inventory.Slots.ToDictionary(slot => slot.SlotIndex);
        var snapshotSlots = snapshot.Inventory.Slots ?? Array.Empty<InventorySlot>();

        foreach (var slotSnapshot in snapshotSlots)
        {
            if (slotSnapshot.ItemId is null || slotSnapshot.Quantity <= 0)
            {
                if (existingSlots.TryGetValue(slotSnapshot.SlotIndex, out var slotToRemove))
                {
                    character.Inventory.Slots.Remove(slotToRemove);
                    _dbContext.InventorySlots.Remove(slotToRemove);
                    existingSlots.Remove(slotSnapshot.SlotIndex);
                }

                continue;
            }

            if (existingSlots.TryGetValue(slotSnapshot.SlotIndex, out var existingSlot))
            {
                if (existingSlot.ItemId == slotSnapshot.ItemId)
                {
                    existingSlot.Quantity = slotSnapshot.Quantity;
                    existingSlot.LastUpdatedAt = DateTime.UtcNow;
                    existingSlot.IsActive = slotSnapshot.IsActive;
                }
                else
                {
                    character.Inventory.Slots.Remove(existingSlot);
                    _dbContext.InventorySlots.Remove(existingSlot);

                    character.Inventory.Slots.Add(CreateSlot(character.Inventory.Id, character.Inventory, slotSnapshot));
                }

                existingSlots.Remove(slotSnapshot.SlotIndex);
            }
            else
            {
                character.Inventory.Slots.Add(CreateSlot(character.Inventory.Id, character.Inventory, slotSnapshot));
            }
        }

        foreach (var leftover in existingSlots.Values)
        {
            character.Inventory.Slots.Remove(leftover);
            _dbContext.InventorySlots.Remove(leftover);
        }
    }

    private static InventorySlot CreateSlot(int inventoryId, Inventory inventory, InventorySlot snapshot)
    {
        return new InventorySlot
        {
            SlotIndex = snapshot.SlotIndex,
            Quantity = snapshot.Quantity,
            ItemId = snapshot.ItemId,
            InventoryId = inventoryId,
            Inventory = inventory,
            CreatedAt = snapshot.CreatedAt == default ? DateTime.UtcNow : snapshot.CreatedAt,
            LastUpdatedAt = DateTime.UtcNow,
            IsActive = snapshot.IsActive
        };
    }
}
