using Godot;

namespace GodotClient.ECS.Components;

// Referências Godot que ficam no ECS (adapter para a camada visual)
public struct NodeRef { public Node2D Node2D; public bool IsVisible; }

