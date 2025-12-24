using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Client.Combat.Components;

namespace GameECS.Client.Combat.Systems;

/// <summary>
/// Sistema que processa input de combate do jogador local.
/// </summary>
public sealed partial class ClientCombatInputSystem : BaseSystem<World, float>
{
    private readonly ICombatInputProvider _inputProvider;
    private readonly ICombatNetworkSender _networkSender;
    private int? _pendingTargetSelection;
    private bool _pendingAttack;

    public ClientCombatInputSystem(
        World world,
        ICombatInputProvider inputProvider,
        ICombatNetworkSender networkSender) : base(world)
    {
        _inputProvider = inputProvider;
        _networkSender = networkSender;
    }

    public override void BeforeUpdate(in float t)
    {
        _pendingTargetSelection = null;
        _pendingAttack = false;

        // Verifica seleção de alvo
        if (_inputProvider.IsTargetSelected())
        {
            var targetId = _inputProvider.GetTargetUnderCursor();
            if (targetId.HasValue)
            {
                _pendingTargetSelection = targetId.Value;
                _networkSender.SendTargetSelection(targetId.Value);
            }
        }

        // Verifica input de ataque
        if (_inputProvider.IsAttackPressed())
        {
            _pendingAttack = true;
        }
    }

    [Query]
    [All<LocalCombatPlayer, ClientCombatEntity>]
    private void ProcessLocalPlayerInput(Entity entity)
    {
        // Processa seleção de alvo
        if (_pendingTargetSelection.HasValue)
        {
            if (World.Has<SelectedTarget>(entity))
            {
                ref var selected = ref World.Get<SelectedTarget>(entity);
                selected.TargetEntityId = _pendingTargetSelection.Value;
            }
            else
            {
                World.Add(entity, new SelectedTarget { TargetEntityId = _pendingTargetSelection.Value });
            }
        }

        // Processa ataque
        if (_pendingAttack && World.Has<SelectedTarget>(entity))
        {
            ref var selected = ref World.Get<SelectedTarget>(entity);
            if (selected.TargetEntityId > 0)
            {
                _networkSender.SendAttackRequest(selected.TargetEntityId);
            }
        }
    }
}
