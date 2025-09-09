using Arch.Core;

namespace Simulation.Core.Shared.Network.Contracts;

public interface IPacket
{
    public Entity Entity { get; set; }
}