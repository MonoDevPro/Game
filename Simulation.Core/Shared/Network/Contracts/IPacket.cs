using Arch.Core;

namespace Simulation.Core.Shared.Network.Contracts;

public interface IPacket
{
    public int PlayerId { get; set; }
}