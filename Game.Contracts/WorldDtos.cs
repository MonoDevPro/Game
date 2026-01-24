namespace Game.Contracts;

public readonly record struct EnterWorldRequest(string EnterTicket);
public readonly record struct WorldSpawnInfo(int CharacterId, string Name, int X, int Y);
public readonly record struct EnterWorldResponse(bool Success, string? Error, WorldSpawnInfo? Spawn);
public readonly record struct WorldMoveCommand(int CharacterId, int Dx, int Dy);
public readonly record struct PlayerState(int CharacterId, string Name, int X, int Y);
public readonly record struct WorldSnapshot(List<PlayerState> Players);
public readonly record struct WorldCollisions(int MapId, int Floor, int Width, int Height, byte[] CompressedData, int UncompressedBitCount);

public sealed record CharacterData(
    int CharacterId, int MapId, int Floor,
    int X, int Y, int DirX, int DirY,
    int Hp, int MaxHp, int HpRegen,
    int Mp, int MaxMp, int MpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);