using Godot;

namespace GodotClient.Simulation;

public enum AnimState : byte
{
    Idle = 0,
    Walk = 1,
    Run = 2,
    Attack = 3,
    Hurt = 4,
    Dead = 5
}

// Referências Godot que ficam no ECS (adapter para a camada visual)
public struct NodeRef { public Node2D Node2D; public bool IsVisible; }
public struct SpriteRef { public AnimatedSprite2D Sprite2D; }
public struct VisualPrefab { public string ScenePath; public Node Parent; }

// Configuração da animação por entidade (nomes das animações no AnimatedSprite2D)
public struct VisualAnimSet
{
    public string Idle;
    public string Walk;
    public string Run;
    public string Attack;
    public string Hurt;
    public string Dead;
}

// Estado de animação decidido pela simulação
public struct AnimationState
{
    public AnimState State;
    public float Speed;     // playback speed (1.0 = normal)
    public bool Loop;
}

// Y-sort / Z-index
public struct Sorting
{
    public int BaseZ;
    public int YSortMultiplier; // ex.: 1 para ZIndex = BaseZ + Y*mul
    public bool UseYSort;
}

// Interpolação para jogadores remotos (suaviza render entre snapshots)
public struct RemoteInterpolation
{
    public float LerpAlpha;   // 0..1 (ex.: 0.15f)
    public float ThresholdPx; // px para snap final
}