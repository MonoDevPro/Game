using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Components;

// 1. Identificador do Tipo (Leve) - Substitui dados hardcoded na entidade
public struct NpcType
{
    public string TemplateId; // "orc_warrior"
    // O System usará isso para buscar ranges e configs em um Singleton/Cache
}

// 2. Estado Mental (O "Cérebro" Dinâmico)
public struct NpcBrain
{
    public NpcState CurrentState;
    public float StateTimer;
    public Entity CurrentTarget; // Substitui NpcTarget complexo
}

public enum NpcState : byte
{
    Idle,
    Patrol,
    Chase,
    Combat,
    ReturnHome
}

// 3. Intenção de Navegação (Desacopla "Querer ir" de "Como ir")
// O BehaviorSystem define o Destination, o MovementSystem decide como chegar lá.
public struct NavigationAgent
{
    public Position? Destination; 
    public float StoppingDistance;
    public bool IsPathPending; // Flag para pedir recalculo de path
}
