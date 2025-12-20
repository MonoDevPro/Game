using Arch.Core;
using Arch.System;
using Game.ECS.Navigation.Components;
using Game.ECS.Navigation.Data;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema que processa input do player local e envia ao servidor.
/// </summary>
public sealed class ClientInputSystem :  BaseSystem<World, float>
{
    private readonly IInputProvider _inputProvider;
    private readonly INetworkSender _networkSender;
    private readonly float _cellSize;
    
    private int _sequenceId;
    private float _inputCooldown;
    private const float InputCooldownTime = 0.1f; // 100ms entre inputs

    public ClientInputSystem(
        World world,
        IInputProvider inputProvider,
        INetworkSender networkSender,
        float cellSize = 32f) : base(world)
    {
        _inputProvider = inputProvider;
        _networkSender = networkSender;
        _cellSize = cellSize;
        _sequenceId = 0;
    }

    public override void Update(in float deltaTime)
    {
        // Cooldown de input
        if (_inputCooldown > 0)
        {
            _inputCooldown -= deltaTime;
            return;
        }

        ProcessClickToMove(World);
    }

    private void ProcessClickToMove(World world)
    {
        // Verifica clique do mouse
        if (! _inputProvider.IsClickPressed())
            return;

        var clickWorldPos = _inputProvider.GetClickWorldPosition();
        
        // Converte para coordenadas de grid
        int targetX = (int)(clickWorldPos.X / _cellSize);
        int targetY = (int)(clickWorldPos.Y / _cellSize);

        // Encontra player local
        var query = new QueryDescription()
            .WithAll<SyncedGridPosition, LocalPlayer, ClientNavigationEntity>();

        world.Query(in query, (Entity entity, ref SyncedGridPosition pos) =>
        {
            // Não envia se já está no destino
            if (pos.X == targetX && pos.Y == targetY)
                return;

            // Envia requisição ao servidor
            var input = new MoveInput
            {
                SequenceId = ++_sequenceId,
                TargetX = (short)targetX,
                TargetY = (short)targetY,
                ClientTick = DateTimeOffset.UtcNow. ToUnixTimeMilliseconds()
            };

            _networkSender.SendMoveInput(input);
            _inputCooldown = InputCooldownTime;
        });
    }
}

/// <summary>
/// Interface para provider de input (abstrai implementação específica).
/// </summary>
public interface IInputProvider
{
    bool IsClickPressed();
    (float X, float Y) GetClickWorldPosition();
    (float X, float Y) GetMovementAxis(); // Para movimento WASD
}

/// <summary>
/// Interface para envio de mensagens de rede.
/// </summary>
public interface INetworkSender
{
    void SendMoveInput(MoveInput input);
}