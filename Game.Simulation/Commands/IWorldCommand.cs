namespace Game.Simulation.Commands;

public interface IWorldCommand
{
    bool Execute(IWorldSimulation simulation);
}