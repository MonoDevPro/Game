namespace Game.Domain.Commons.Enums;

public enum EquipmentSlotType : byte
{
    Head = 0,
    Chest = 1,
    Legs = 2,
    Feet = 3,
    Hands = 4,
    MainHand = 5,
    OffHand = 6,
    Accessory1 = 7,
    Accessory2 = 8,
    Accessory3 = 9,
    
    // mantenha este COUNT atualizado (necessário para fixed buffers)
    Count = 10
}

