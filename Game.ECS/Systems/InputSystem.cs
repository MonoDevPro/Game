using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por processar input do jogador local.
/// Converte input em ações (movimento, ataque, habilidades, etc).
/// </summary>
public sealed partial class InputSystem(World world) : GameSystem(world)
{
    [Query]
    [All<LocalPlayerTag, PlayerInput, PlayerControlled>]
    private void ProcessPlayerInput(in Entity e, in PlayerInput input, [Data] float deltaTime)
    {
        // O sistema de movimento já processa o input
        // Este sistema pode ser estendido para processar outros tipos de input
        // como habilidades, usos de itens, etc
    }

    /// <summary>
    /// Aplica input a um jogador local (normalmente chamado a partir da camada de input).
    /// </summary>
    public bool ApplyPlayerInput(Entity entity, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (!World.IsAlive(entity) || !World.Has<LocalPlayerTag>(entity))
            return false;

        var input = new PlayerInput
        {
            InputX = inputX,
            InputY = inputY,
            Flags = flags
        };

        World.Set(entity, input);
        World.MarkNetworkDirty(entity, SyncFlags.Input);

        return true;
    }

    /// <summary>
    /// Limpa o input de um jogador.
    /// </summary>
    public bool ClearPlayerInput(Entity entity)
    {
        return ApplyPlayerInput(entity, 0, 0, InputFlags.None);
    }

    /// <summary>
    /// Obtém o input atual de um jogador.
    /// </summary>
    public bool TryGetPlayerInput(Entity entity, out PlayerInput input)
    {
        input = default;
        
        if (!World.IsAlive(entity) || !World.Has<PlayerInput>(entity))
            return false;

        input = World.Get<PlayerInput>(entity);
        return true;
    }
}
