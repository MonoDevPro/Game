using Server.Persistence.Context;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.Models;
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
}
