using Arch.Core;
using Game.Infrastructure.ArchECS.Commons.Components;

namespace Game.Simulation;

/// <summary>
/// Comando para mover uma entidade por um delta de posição.
/// </summary>
/// <param name="CharacterId">ID do personagem a ser movido.</param>
/// <param name="Dx">Delta X (direção horizontal).</param>
/// <param name="Dy">Delta Y (direção vertical).</param>
public sealed record MoveCommand(int CharacterId, int Dx, int Dy) : IWorldCommand
{
    // Query descritor para buscar entidades com CharacterId e Position
    private static readonly QueryDescription Query = new QueryDescription()
        .WithAll<CharacterId, Position>();
    
    public void Execute(World world)
    {
        world.Query(in Query, (ref CharacterId id, ref Position pos) =>
        {
            if (id.Value == CharacterId)
            {
                pos.X += Dx;
                pos.Y += Dy;
            }
        });
    }
}
