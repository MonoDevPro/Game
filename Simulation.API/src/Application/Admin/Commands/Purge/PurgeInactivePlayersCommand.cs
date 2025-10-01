using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Security;
using GameWeb.Domain.Constants;

namespace GameWeb.Application.Admin.Commands.Purge;

// Only administrators can purge characters
[Authorize(Policy = Policies.CanDeletePlayers)]
public record PurgeInactivePlayersCommand(bool? IsActive = null) : ICommand<long>;

public class PurgeInactivePlayersCommandHandler(IApplicationDbContext context)
    : IRequestHandler<PurgeInactivePlayersCommand, long>
{
    public async Task<long> Handle(PurgeInactivePlayersCommand request, CancellationToken cancellationToken)
    {
        // 1. Usa a especificação para obter a lista de personagens a serem removidos.
        var players = await context.Players
            .IgnoreQueryFilters()
            .Where(p => request.IsActive == null || p.IsActive == request.IsActive)
            .ToListAsync(cancellationToken);
        
        int count = players.Count;

        if (count == 0)
            return count; // Nenhum personagem para remover.
        
        // 2. Itera sobre a lista e marca cada um para ser apagado.
        foreach (var player in players)
            context.Players.Remove(player);

        return count;
    }
}
