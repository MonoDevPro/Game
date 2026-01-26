using MemoryPack;

namespace Game.Contracts;

[MemoryPackable]
public readonly partial record struct EnterWorldRequest(string EnterTicket) : IEnvelopePayload;

[MemoryPackable]
public readonly partial record struct WorldSpawnInfo(int CharacterId, string Name, int X, int Y, int Floor, int DirX, int DirY);

[MemoryPackable]
public readonly partial record struct EnterWorldResponse(bool Success, string? Error, WorldSpawnInfo? Spawn) : IEnvelopePayload;

/// <summary>
/// Comando de movimento por delta (relativo à posição atual).
/// </summary>
[MemoryPackable]
public readonly partial record struct WorldMoveCommand(int CharacterId, int Dx, int Dy) : IEnvelopePayload;

/// <summary>
/// Comando de navegação para uma posição específica (usa pathfinding).
/// </summary>
[MemoryPackable]
public readonly partial record struct WorldNavigateCommand(int CharacterId, int TargetX, int TargetY, int TargetFloor = 0) : IEnvelopePayload;

/// <summary>
/// Comando para parar movimento.
/// </summary>
[MemoryPackable]
public readonly partial record struct WorldStopCommand(int CharacterId) : IEnvelopePayload;

/// <summary>
/// Comando de ataque básico direcional.
/// </summary>
[MemoryPackable]
public readonly partial record struct WorldBasicAttackCommand(int CharacterId, int DirX, int DirY) : IEnvelopePayload;

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
[MemoryPackable]
public readonly partial record struct PlayerState(
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
    float MoveProgress = 0f,
    int CurrentHp = 0,
    int MaxHp = 0,
    int CurrentMp = 0,
    int MaxMp = 0);

/// <summary>
/// Snapshot do estado do mundo para sincronização.
/// </summary>
/// <param name="ServerTick">Tick do servidor quando o snapshot foi criado.</param>
/// <param name="Timestamp">Timestamp UTC em milissegundos para compensação de latência.</param>
/// <param name="Players">Lista de estados dos jogadores.</param>
[MemoryPackable]
public readonly partial record struct WorldSnapshot(
    long ServerTick,
    long Timestamp,
    List<PlayerState> Players) : IEnvelopePayload;
