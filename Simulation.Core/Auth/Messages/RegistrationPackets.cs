using MemoryPack;
using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Auth.Messages
{
    [MemoryPackable]
    public readonly partial record struct RegisterRequest(string Username, string Password) : IPacket;

    [MemoryPackable]
    public readonly partial record struct RegisterResponse(bool Success, string Message) : IPacket;
}