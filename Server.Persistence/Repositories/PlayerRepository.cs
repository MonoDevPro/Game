using Server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Simulation.Core.ECS.Components;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Contracts.Repositories;
using Simulation.Core.Persistence.Models;

namespace Server.Persistence.Repositories;

public class PlayerRepository : EFCoreRepository<int, PlayerModel>, IPlayerRepository
{
    public PlayerRepository(SimulationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Carrega o PlayerModel, atualiza-o com o PlayerData, e guarda as alterações.
    /// </summary>
    public async Task<bool> UpdateFromDataAsync(PlayerData data, CancellationToken ct = default)
    {
        var playerModel = await GetAsync(data.Id, ct);
        if (playerModel == null)
            return false; // Jogador não encontrado

        // Usa o método de extensão para mapear os dados (presume-se que UpdateFromData existe)
        playerModel.UpdateFromData(data);
        
        // Atualiza apenas os dados "quentes" que podem mudar durante o jogo.
        playerModel.HealthCurrent = data.HealthCurrent;
        playerModel.Id = data.Id;
        playerModel.PosX = data.PosX;
        playerModel.PosY = data.PosY;
        playerModel.DirX = data.DirX;
        playerModel.DirY = data.DirY;

        // Marca como modificado e salva via SaveChangesAsync (base expõe SaveChangesAsync)
        Context.Update(playerModel);
        await SaveChangesAsync(ct);

        return true;
    }

    public async Task<PlayerData?> GetPlayerByName(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var playerModel = await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == name, ct);

        if (playerModel == null) return null;
        return PlayerData.FromModel(playerModel);
    }

    public async Task<PlayerData?> GetPlayerById(int id, CancellationToken ct = default)
    {
        var playerModel = await GetAsync(id, ct);
        if (playerModel == null) return null;
        return PlayerData.FromModel(playerModel);
    }
}