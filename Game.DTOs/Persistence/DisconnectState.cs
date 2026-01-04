namespace Game.DTOs.Persistence;

/// <summary>
/// DTO para persistir dados de desconexão do personagem.
/// Autor: MonoDevPro
/// Data: 2025-10-13 20:30:00
/// </summary>
public sealed record DisconnectPersistenceDto
{
    public required int CharacterId { get; init; }
    public required int MapId { get; init; }
    public required int PositionX { get; init; }
    public required int PositionY { get; init; }
    public required int PositionZ { get; init; }
    public required int DirX { get; init; }
    public required int DirY { get; init; }
    public required int CurrentHp { get; init; }
    public required int CurrentMp { get; init; }
    
    public override string ToString()
    {
        return $"DisconnectPersistenceDto(CharacterId={CharacterId}, MapId=({MapId}), Position=({PositionX}, {PositionY}), Facing=({DirX}, {DirY}), CurrentHp={CurrentHp}, CurrentMp={CurrentMp})";
    }
}