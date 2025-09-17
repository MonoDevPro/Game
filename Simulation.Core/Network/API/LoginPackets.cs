using MemoryPack;
using Simulation.Core.ECS.Data;
using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Network.API;

/// <summary>
/// Pacote enviado do Cliente para o Servidor solicitando autenticação.
/// </summary>
[MemoryPackable]
public readonly partial record struct LoginRequestPacket : IPacket
{
    public string Username { get; init; }
    public string Password { get; init; }
}

/// <summary>
/// Pacote enviado do Servidor para o Cliente indicando sucesso no login.
/// Contém o PlayerId (entity id lógica do jogador) e os dados completos
/// para montar a UI/local player.
/// </summary>
[MemoryPackable]
public readonly partial record struct LoginSuccessPacket : IPacket
{
    public int PlayerId { get; init; }
    public PlayerData PlayerData { get; init; }
}

/// <summary>
/// Pacote enviado do Servidor para o Cliente indicando falha no login.
/// </summary>
[MemoryPackable]
public readonly partial record struct LoginFailedPacket : IPacket
{
    public string Reason { get; init; }
}
