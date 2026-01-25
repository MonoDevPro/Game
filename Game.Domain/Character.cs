namespace Game.Domain;

/// <summary>
/// Personagem jogável
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public sealed record Character(int Id, int AccountId, string Name, Gender Gender, int X, int Y, int Floor, Direction Direction);

public sealed record CharacterVocation(int Id, int CharacterId, Vocation Vocation, int Level, long Experience, int Strength, int Endurance, int Agility, int Intelligence, int Willpower, int HealthPoints, int ManaPoints);
    
public enum Gender : byte { Male, Female, }
public enum Vocation : byte { Warrior, Archer, Mage }
public enum Direction : byte { North, East, South, West, NorthEast, SouthEast, SouthWest, NorthWest }