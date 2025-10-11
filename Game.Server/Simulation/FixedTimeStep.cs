namespace Game.Server.Simulation;

public class FixedTimeStep(float fixedDeltaTime)
{
    private float _accumulator;

    public void Accumulate(float deltaTime)
    {
        _accumulator += Math.Min(deltaTime, 0.25f); // Prevenir spiral of death
    }

    public bool ShouldUpdate()
    {
        return _accumulator >= fixedDeltaTime;
    }

    public void Step()
    {
        _accumulator -= fixedDeltaTime;
    }
}