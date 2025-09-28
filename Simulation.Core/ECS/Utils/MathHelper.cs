namespace Simulation.Core.ECS.Utils;

public static class MathHelper
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}