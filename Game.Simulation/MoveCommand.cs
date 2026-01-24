namespace Game.Simulation;

public sealed record MoveCommand(int CharacterId, int Dx, int Dy) : IWorldCommand
{
    public void Apply(WorldState state)
    {
        state.Move(CharacterId, Dx, Dy);
    }
}
