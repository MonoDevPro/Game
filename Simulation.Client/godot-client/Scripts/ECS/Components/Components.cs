using Godot;

namespace GodotClient.ECS.Components;

// ============================================
// Client Render - Componentes especficos do cliente
// ============================================

/// <summary>
/// Referncia a um objeto visual (Node2D, GameObject, etc).
/// Usado para sincronizar ECS com a camada de renderizao.
/// NOTA: Este componente deve ser preenchido pela camada de apresentao (Godot, Unity, etc).
/// </summary>
public struct VisualReference
{
    public Node2D VisualNode;  // Node2D no Godot, GameObject no Unity, etc
    public bool IsVisible;
    public int VisualId;        // ID opcional para lookup
}

/// <summary>
/// Dados de interpolao para entidades remotas.
/// Suaviza a renderizao entre snapshots do servidor.
/// </summary>
public struct RemoteInterpolation
{
    public float LerpAlpha;     // Fator de interpolao (0..1, tipicamente 0.1-0.2)
    public float ThresholdPx;   // Limiar em pixels para snap ao alvo
}