using MemoryPack;

namespace Simulation.Core.ECS.Components.Data;

public enum Gender : int { None, Male, Female }
public enum Vocation : int { None, Mage, Archer }

/// <summary>
/// Um componente que transporta os dados de um jogador, geralmente
/// carregados da base de dados, para dentro do mundo ECS.
/// Sendo um 'record struct', é um value-type imutável e performático.
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerData
{
    // --- Dados Frios / de Identificação ---
    public int Id { get; init; }
    public string Name { get; init; }
    public Gender Gender { get; init; }
    public Vocation Vocation { get; init; }
    
    // --- Stats Base (geralmente não mudam a cada tick) ---
    public int HealthMax { get; init; }
    public int AttackDamage { get; init; }
    public int AttackRange { get; init; }
    public float AttackCastTime { get; init; }
    public float AttackCooldown { get; init; }
    public float MoveSpeed { get; init; }
    
    // --- Dados Quentes / de Estado (mudam a cada tick) ---
    public int PosX { get; init; }
    public int PosY { get; init; }
    public int DirX { get; init; }
    public int DirY { get; init; }
    public int HealthCurrent { get; init; }
}