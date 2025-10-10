namespace Game.Server.Simulation;

public class FixedTimeStep
{
    private readonly float _fixedDeltaTime;
    private float _accumulator;

    public FixedTimeStep(float fixedDeltaTime)
    {
        _fixedDeltaTime = fixedDeltaTime;
    }

    public void Accumulate(float deltaTime)
    {
        _accumulator += Math.Min(deltaTime, 0.25f); // Prevenir spiral of death
    }

    public bool ShouldUpdate()
    {
        return _accumulator >= _fixedDeltaTime;
    }

    public void Step()
    {
        _accumulator -= _fixedDeltaTime;
    }
}