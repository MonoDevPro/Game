using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.Persistence.Models;

/// <summary>
/// Métodos de extensão para mapear entre DTOs/componentes (Data) e modelos de persistência (Model).
/// </summary>
public static class ModelMappingExtensions
{
    /// <summary>
    /// Atualiza as propriedades de um PlayerModel com os valores de um PlayerData.
    /// </summary>
    public static void UpdateFromData(this PlayerModel model, PlayerData data)
    {
        // Atualiza apenas os dados "quentes" que podem mudar durante o jogo.
        model.HealthCurrent = data.HealthCurrent;
        model.MapId = data.MapId;
        model.PosX = data.PosX;
        model.PosY = data.PosY;
        model.DirX = data.DirX;
        model.DirY = data.DirY;
    }

    /// <summary>
    /// Cria uma nova instância de PlayerModel a partir de um PlayerData.
    /// </summary>
    public static PlayerModel ToModel(this PlayerData data)
    {
        return new PlayerModel
        {
            Id = data.Id,
            Name = data.Name,
            Gender = data.Gender,
            Vocation = data.Vocation,
            HealthMax = data.HealthMax,
            HealthCurrent = data.HealthCurrent,
            AttackDamage = data.AttackDamage,
            AttackRange = data.AttackRange,
            AttackCastTime = data.AttackCastTime,
            AttackCooldown = data.AttackCooldown,
            MoveSpeed = data.MoveSpeed,
            MapId = data.MapId,
            PosX = data.PosX,
            PosY = data.PosY,
            DirX = data.DirX,
            DirY = data.DirY,
        };
    }
    
    /// <summary>
    /// Cria uma nova instância de MapModel a partir de um MapData.
    /// Útil para criar um novo registo de mapa na base de dados.
    /// </summary>
    public static MapModel ToModel(this MapData data)
    {
        return new MapModel
        {
            MapId = data.MapId,
            Name = data.Name,
            Width = data.Width,
            Height = data.Height,
            UsePadded = data.UsePadded,
            BorderBlocked = data.BorderBlocked,
            TilesRowMajor = data.TilesRowMajor,
            CollisionRowMajor = data.CollisionRowMajor
        };
    }
}
