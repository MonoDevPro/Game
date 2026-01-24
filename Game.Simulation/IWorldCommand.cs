namespace Game.Simulation;

public interface IWorldCommand
{
    void Apply(WorldState state);
}
