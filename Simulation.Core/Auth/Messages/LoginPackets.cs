using MemoryPack;
using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Auth.Messages;

/// <summary>
/// Pacote enviado do Cliente para o Servidor solicitando autenticação.
/// </summary>
[MemoryPackable]
public readonly partial record struct LoginRequest(
    string Username, 
    string Password) : IPacket;

[MemoryPackable]
public readonly partial record struct LoginResponse(
    bool Success,
    string Message,
    int AccountId,
    DateTime? LastLoginAt,
    Guid? SessionToken) : IPacket;