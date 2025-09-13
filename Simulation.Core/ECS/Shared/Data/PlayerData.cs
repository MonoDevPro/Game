using MemoryPack;
using Simulation.Core.Persistence.Models;

namespace Simulation.Core.ECS.Shared.Data;

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
    public int MapId { get; init; }
    public int PosX { get; init; }
    public int PosY { get; init; }
    public int DirX { get; init; }
    public int DirY { get; init; }
    public int HealthCurrent { get; init; }

    /// <summary>
    /// Construtor de fábrica para criar um PlayerData a partir de um PlayerModel.
    /// </summary>
    public static PlayerData FromModel(PlayerModel model)
    {
        return new PlayerData
        {
            Id = model.Id,
            Name = model.Name ?? string.Empty,
            Gender = model.Gender,
            Vocation = model.Vocation,
            HealthMax = model.HealthMax,
            AttackDamage = model.AttackDamage,
            AttackRange = model.AttackRange,
            AttackCastTime = model.AttackCastTime,
            AttackCooldown = model.AttackCooldown,
            MoveSpeed = model.MoveSpeed,
            MapId = model.MapId,
            PosX = model.PosX,
            PosY = model.PosY,
            DirX = model.DirX,
            DirY = model.DirY,
            HealthCurrent = model.HealthCurrent
        };
    }
}