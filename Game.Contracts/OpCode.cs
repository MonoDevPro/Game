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
    WorldMoveCommand = 110,
    WorldSnapshot = 120,
    ChatSendRequest = 200,
    ChatMessage = 201,
}
