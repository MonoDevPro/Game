namespace Game.Domain.Enums;

public enum EquipmentSlotType : byte
{
    None = 0,
    Head = 1,
    Chest = 2,
    Legs = 3,
    Feet = 4,
    Hands = 5,
    MainHand = 6,
    OffHand = 7,
    Accessory1 = 8,
    Accessory2 = 9,
    Accessory3 = 10,
    
    // mantenha este COUNT atualizado (necessário para fixed buffers)
    Count = 11
}

