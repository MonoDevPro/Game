namespace Game.Domain.Data;

public record CharacterState(
    int CharacterId,
    int MapId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int DirX,
    int DirY,
    int CurrentHp,
    int CurrentMp
);