using MemoryPack;
using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Network.API;

[MemoryPackable]
public readonly partial record struct RegisterRequestPacket(string Username, string Password) : IPacket;

[MemoryPackable]
public readonly partial record struct RegisterFailedPacket(string Reason) : IPacket;
