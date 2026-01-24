namespace Game.Contracts;

public readonly record struct EnterWorldRequest(string EnterTicket);
public readonly record struct WorldSpawnInfo(int CharacterId, string Name, int X, int Y);
public readonly record struct EnterWorldResponse(bool Success, string? Error, WorldSpawnInfo? Spawn);

/// <summary>
/// Comando de movimento por delta (relativo à posição atual).
/// </summary>
public readonly record struct WorldMoveCommand(int CharacterId, int Dx, int Dy);

/// <summary>
/// Comando de navegação para uma posição específica (usa pathfinding).
/// </summary>
public readonly record struct WorldNavigateCommand(int CharacterId, int TargetX, int TargetY, int TargetFloor = 0);

/// <summary>
/// Comando para parar movimento.
/// </summary>
public readonly record struct WorldStopCommand(int CharacterId);

/// <summary>
/// Estado de um jogador para sincronização.
/// </summary>
/// <param name="CharacterId">ID do personagem.</param>
/// <param name="Name">Nome do jogador.</param>
/// <param name="X">Posição X atual.</param>
/// <param name="Y">Posição Y atual.</param>
/// <param name="Floor">Andar atual.</param>
/// <param name="DirX">Direção X do movimento (-1, 0, 1).</param>
/// <param name="DirY">Direção Y do movimento (-1, 0, 1).</param>
/// <param name="IsMoving">Se está em movimento.</param>
/// <param name="TargetX">Posição X de destino (para interpolação).</param>
/// <param name="TargetY">Posição Y de destino (para interpolação).</param>
/// <param name="MoveProgress">Progresso do movimento atual (0.0 a 1.0).</param>
public readonly record struct PlayerState(
    int CharacterId, 
    string Name, 
    int X, 
    int Y, 
    int Floor = 0,
    int DirX = 0, 
    int DirY = 0, 
    bool IsMoving = false,
    int TargetX = 0,
    int TargetY = 0,
    float MoveProgress = 0f);

/// <summary>
/// Snapshot do estado do mundo para sincronização.
/// </summary>
/// <param name="ServerTick">Tick do servidor quando o snapshot foi criado.</param>
/// <param name="Timestamp">Timestamp UTC em milissegundos para compensação de latência.</param>
/// <param name="Players">Lista de estados dos jogadores.</param>
public readonly record struct WorldSnapshot(long ServerTick, long Timestamp, List<PlayerState> Players)
{
    /// <summary>
    /// Construtor de compatibilidade (sem tick/timestamp).
    /// </summary>
    public WorldSnapshot(List<PlayerState> Players) : this(0, 0, Players) { }
    
    /// <summary>
    /// Construtor de compatibilidade (sem timestamp).
    /// </summary>
    public WorldSnapshot(long ServerTick, List<PlayerState> Players) 
        : this(ServerTick, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Players) { }
}
public readonly record struct WorldCollisions(int MapId, int Floor, int Width, int Height, byte[] CompressedData, int UncompressedBitCount);

public sealed record CharacterData(
    int CharacterId, int MapId, int Floor,
    int X, int Y, int DirX, int DirY,
    int Hp, int MaxHp, int HpRegen,
    int Mp, int MaxMp, int MpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);