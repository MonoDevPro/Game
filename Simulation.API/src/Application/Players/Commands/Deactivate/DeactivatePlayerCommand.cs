using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Players.Commands.Deactivate;

public record DeactivatePlayerCommand(int Id) : ICommand<int>;

public class DeactivatePlayerCommandHandler(
    IApplicationDbContext context, 
    IUser user)
    : IRequestHandler<DeactivatePlayerCommand, int>
{
    public async Task<int> Handle(DeactivatePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await context.Players
            .FindAsync([request.Id], cancellationToken);
        
        if (player == null || player.UserId != user.Id) 
            throw new GameWeb.Application.Common.Exceptions.NotFoundException(nameof(Player), request.Id.ToString());
        
        player.IsActive = false;
        return player.Id;
    }
}
