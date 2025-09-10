using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Shared.Components;

namespace Simulation.Core.Client.Systems;

/// <summary>
/// Sistema de "renderização" que usa o Source Generator do Arch.
/// </summary>
public partial class RenderSystem(World world) : BaseSystem<World, float>(world)
{
    // A query é definida diretamente no método.
    // Usar 'in Position' indica que apenas lemos o componente, o que é mais eficiente.
    [Query]
    [All<Position>]
    private void Render(in Position pos)
    {
        // Apenas para depuração, para confirmar que estamos a receber atualizações.
        //Console.WriteLine($"[Client] Render Position: X={pos.X}, Y={pos.Y}");
    }
}