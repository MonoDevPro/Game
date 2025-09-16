using Server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Models;

namespace Server.Persistence.Repositories;

public class PlayerRepository(SimulationDbContext context) : EFCoreRepository<int, PlayerModel>(context), IPlayerRepository
{
    private readonly SimulationDbContext _context = context;

    /// <summary>
    /// Carrega o PlayerModel, atualiza-o com o PlayerData, e guarda as alterações.
    /// </summary>
    public async Task<bool> UpdateFromDataAsync(PlayerData data, CancellationToken ct = default)
    {
        var playerModel = await _context.PlayerModels.FindAsync([data.Id], cancellationToken: ct);

        if (playerModel == null)
        {
            return false; // Jogador não encontrado
        }

        // Usa o método de extensão para mapear os dados
        playerModel.UpdateFromData(data);
        
        // Marca a entidade como modificada e guarda as alterações
        _context.PlayerModels.Update(playerModel);
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<PlayerModel?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.PlayerModels
            .AsQueryable()
            .Where(p => p.Name == name)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PlayerModel> CreateWithDefaultsAsync(string name, string passwordHash, CancellationToken ct = default)
    {
        // Defaults iniciais simples; podem ser extraídos para config no futuro
        var player = new PlayerModel
        {
            Name = name,
            PasswordHash = passwordHash,
            MapId = 1,
            PosX = 1,
            PosY = 1,
            MoveSpeed = 1.0f,
            AttackCastTime = 1.0f,
            AttackCooldown = 1.0f,
            HealthMax = 100,
            HealthCurrent = 100,
            AttackDamage = 10,
            AttackRange = 1
        };
        _context.PlayerModels.Add(player);
        await _context.SaveChangesAsync(ct);
        return player;
    }
}
