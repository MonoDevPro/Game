using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Navigation.Client.Components;
using Game.ECS.Navigation.Client.Contracts;
using Game.ECS.Navigation.Shared.Data;

namespace Game.ECS.Navigation.Client.Systems;

/// <summary>
/// Sistema que processa input do player local e envia ao servidor.
/// </summary>
public sealed partial class ClientInputSystem(
    World world,
    IInputProvider inputProvider,
    INetworkSender networkSender,
    float cellSize = 32f)
    : BaseSystem<World, float>(world)
{
    private int _sequenceId = 0;
    private float _inputCooldown;
    private const float InputCooldownTime = 0.1f; // 100ms entre inputs

    public override void Update(in float deltaTime)
    {
        // Cooldown de input
        if (_inputCooldown > 0)
        {
            _inputCooldown -= deltaTime;
            return;
        }
        
        ProcessClickToMoveQuery(World);
    }

    [Query]
    [All<SyncedGridPosition, LocalPlayer, ClientNavigationEntity>]
    private void ProcessClickToMove(in Entity entity, ref SyncedGridPosition pos)
    {
        // Verifica clique do mouse
        if (!inputProvider.IsClickPressed())
            return;

        var clickWorldPos = inputProvider.GetClickWorldPosition();
        
        // Converte para coordenadas de grid
        int targetX = (int)(clickWorldPos.X / cellSize);
        int targetY = (int)(clickWorldPos.Y / cellSize);

        // Não envia se já está no destino
        if (pos.X == targetX && pos.Y == targetY)
            return;

        // Envia requisição ao servidor
        var input = new MoveInput
        {
            SequenceId = ++_sequenceId,
            TargetX = (short)targetX,
            TargetY = (short)targetY,
            ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        networkSender.SendMoveInput(input);
        _inputCooldown = InputCooldownTime;
    }
}