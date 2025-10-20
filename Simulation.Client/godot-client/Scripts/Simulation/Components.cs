using Godot;

namespace GodotClient.Simulation;

// Referências Godot que ficam no ECS (adapter para a camada visual)
public struct NodeRef { public Node2D Node2D; public bool IsVisible; }

public struct LocalPlayerTag { }

// Interpolação para jogadores remotos (suaviza render entre snapshots)
public struct RemoteInterpolation
{
    public float LerpAlpha;   // 0..1 (ex.: 0.15f)
    public float ThresholdPx; // px para snap final
}
