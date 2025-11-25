using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcMotorSystem(
    World world,
    ILogger<NpcMotorSystem>? logger = null)
    : GameSystem(world)
{
    [Query]
    [All<NpcPath, Input, Position, NavigationAgent, DirtyFlags>]
    public void DriveMotor(
        ref Input input,
        ref NpcPath path,
        ref DirtyFlags dirty,
        in Position pos,
        in NavigationAgent nav)
    {
        // Se já estamos perto o suficiente (ex: para atacar), pare.
        if (nav.Destination.HasValue && Distance(pos, nav.Destination.Value) <= nav.StoppingDistance)
        {
            input.InputX = 0;
            input.InputY = 0;
            return;
        }

        // Lógica pura de seguir waypoints (Steering)
        if (path.HasPath && !path.IsPathComplete)
        {
            var waypoint = path.GetCurrentWaypoint();
            
            // Se chegou no waypoint atual, avança para o próximo
            if (pos.X == waypoint.X && pos.Y == waypoint.Y)
            {
                if (path.AdvanceToNextWaypoint())
                {
                    waypoint = path.GetCurrentWaypoint();
                }
                else
                {
                    // Terminou o caminho
                    input.InputX = 0;
                    input.InputY = 0;
                    return;
                }
            }

            (input.InputX, input.InputY) = GetDirection(pos, waypoint);
            dirty.MarkDirty(DirtyComponentType.Input); // Marca input como sujo para rede/movimento
        }
        else
        {
            input.InputX = 0;
            input.InputY = 0;
        }
    }

    private static float Distance(Position a, Position b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static (sbyte X, sbyte Y) GetDirection(Position current, Position target)
    {
        int dx = target.X - current.X;
        int dy = target.Y - current.Y;
        
        sbyte dirX = 0;
        sbyte dirY = 0;

        if (dx > 0) dirX = 1;
        else if (dx < 0) dirX = -1;

        if (dy > 0) dirY = 1;
        else if (dy < 0) dirY = -1;

        return (dirX, dirY);
    }
}
