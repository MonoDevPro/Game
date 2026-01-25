using Game.Infrastructure.ArchECS.Services.Navigation;

namespace Game.Simulation;

public interface ISimulationCommand
{
    bool Execute(IWorldSimulation simulation);
}