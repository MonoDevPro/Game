using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.Core.MapGame.Services;

/// Motora de navegação que compõe GameMapService + ISpatialService
/// para movimentos atômicos e checagens rápidas, sem alocações.
public static class MapNavigation
{
    public enum MoveBlockReason : byte
    {
        None = 0,
        OutOfBounds,
        MapBlocked,
        Occupied
    }

    /// Checa se um passo (from -> to) é válido (bounds, colisão e ocupação).
    public static bool CanStep(in GameMapService map, in ISpatialService spatial,
        in Position from, in Position to, out MoveBlockReason reason)
    {
        // Bounds
        if (!map.InBounds(to))
        {
            reason = MoveBlockReason.OutOfBounds;
            return false;
        }

        // Colisão do mapa
        if (!map.TryIsBlocked(to, out var blocked) || blocked)
        {
            reason = blocked ? MoveBlockReason.MapBlocked : MoveBlockReason.OutOfBounds;
            return false;
        }

        // Ocupação via spatial (fast path)
        if (spatial.TryGetFirstAt(to, out var occupant) && !occupant.Equals(default(Entity)))
        {
            // Se quiser permitir empurrão ou empilhar, trate aqui
            reason = MoveBlockReason.Occupied;
            return false;
        }

        reason = MoveBlockReason.None;
        return true;
    }

    /// Movimento atômico: opcionalmente usa reserva para evitar race no mesmo tick.
    /// Retorna false com razão se não puder mover.
    public static bool TryMoveAtomic(in GameMapService map, in ISpatialService spatial,
        in Entity entity, in Position from, in Position to, bool useReservation,
        out MoveBlockReason reason, out ReservationToken token)
    {
        token = default;

        if (!CanStep(in map, in spatial, in from, in to, out reason))
            return false;

        if (useReservation)
        {
            if (!spatial.TryReserve(to, entity, out token))
            {
                reason = MoveBlockReason.Occupied;
                return false;
            }
        }

        // Se a implementação de spatial possuir TryMove atômico, usamos
        if (!spatial.TryMove(from, to, entity))
        {
            if (!token.Equals(default(ReservationToken)))
                spatial.ReleaseReservation(token);
            reason = MoveBlockReason.Occupied;
            return false;
        }

        // Liberar reserva após sucesso (se houver)
        if (!token.Equals(default(ReservationToken)))
            spatial.ReleaseReservation(token);

        reason = MoveBlockReason.None;
        return true;
    }

    /// Conveniência: passo de 4-direções
    public static Position Step4(in Position from, int dx, int dy)
        => new Position { X = from.X + dx, Y = from.Y + dy };
}