using Game.Domain.Entities;
using Game.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Players;

public sealed class PlayerPersistenceService(GameDbContext dbContext, ILogger<PlayerPersistenceService> logger)
{
    public async Task PersistAsync(Character snapshot, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            var character = await dbContext.Characters
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

            PersistStats(snapshot, character);
            PersistInventory(snapshot, character);

            dbContext.Characters.Update(character);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist character {CharacterId}", snapshot.Id);
        }
    }

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

    private void PersistInventory(Character snapshot, Character character)
    {
        var existingSlots = character.Inventory.Slots.ToDictionary(slot => slot.SlotIndex);
        var snapshotSlots = snapshot.Inventory.Slots ?? Array.Empty<InventorySlot>();

        foreach (var slotSnapshot in snapshotSlots)
        {
            if (slotSnapshot.ItemId is null || slotSnapshot.Quantity <= 0)
            {
                if (existingSlots.TryGetValue(slotSnapshot.SlotIndex, out var slotToRemove))
                {
                    character.Inventory.Slots.Remove(slotToRemove);
                    dbContext.InventorySlots.Remove(slotToRemove);
                    existingSlots.Remove(slotSnapshot.SlotIndex);
                }

                continue;
            }

            if (existingSlots.TryGetValue(slotSnapshot.SlotIndex, out var existingSlot))
            {
                if (existingSlot.ItemId == slotSnapshot.ItemId)
                {
                    existingSlot.Quantity = slotSnapshot.Quantity;
                    existingSlot.IsActive = slotSnapshot.IsActive;
                }
                else
                {
                    character.Inventory.Slots.Remove(existingSlot);
                    dbContext.InventorySlots.Remove(existingSlot);

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
            dbContext.InventorySlots.Remove(leftover);
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
            IsActive = snapshot.IsActive
        };
    }
}
