namespace Game.Contracts;

public enum OpCode : ushort
{
    AuthLoginRequest = 1,
    AuthLoginResponse = 2,
    AuthCharacterListRequest = 3,
    AuthCharacterListResponse = 4,
    AuthSelectCharacterRequest = 5,
    AuthSelectCharacterResponse = 6,
    
    WorldEnterRequest = 100,
    WorldEnterResponse = 101,
    
    /// <summary>Movimento por delta (relativo).</summary>
    WorldMoveCommand = 110,
    /// <summary>Navegação para posição específica (pathfinding).</summary>
    WorldNavigateCommand = 111,
    /// <summary>Para movimento atual.</summary>
    WorldStopCommand = 112,
    
    /// <summary>Snapshot completo do mundo.</summary>
    WorldSnapshot = 120,
    /// <summary>Snapshot delta (apenas mudanças).</summary>
    WorldSnapshotDelta = 121,
    /// <summary>Cliente solicita snapshot completo (após perda de pacotes).</summary>
    WorldSnapshotRequest = 122,
    
    ChatSendRequest = 200,
    ChatMessage = 201,
}
