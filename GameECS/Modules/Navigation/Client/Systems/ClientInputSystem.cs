using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Modules.Navigation.Client.Components;
using GameECS.Modules.Navigation.Shared.Data;

namespace GameECS.Modules.Navigation.Client.Systems;

/// <summary>
/// Sistema que processa input do player local e envia ao servidor.
/// </summary>
public sealed partial class ClientInputSystem : BaseSystem<World, float>
{
    private readonly IInputProvider _inputProvider;
    private readonly INetworkSender _networkSender;
    private readonly float _cellSize;
    
    private int _sequenceId;
    private float _inputCooldown;
    private const float InputCooldownTime = 0.1f; // 100ms entre inputs
    private int _targetX;
    private int _targetY;
    private bool _shouldProcess;

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

        // Verifica clique do mouse
        if (!_inputProvider.IsClickPressed())
            return;

        var clickWorldPos = _inputProvider.GetClickWorldPosition();
        
        // Converte para coordenadas de grid
        _targetX = (int)(clickWorldPos.X / _cellSize);
        _targetY = (int)(clickWorldPos.Y / _cellSize);
        _shouldProcess = true;

        ProcessClickToMoveQuery(World);
    }

    [Query]
    [All<SyncedGridPosition, LocalPlayer, ClientNavigationEntity>]
    private void ProcessClickToMove(Entity entity, ref SyncedGridPosition pos)
    {
        if (!_shouldProcess) return;
        
        // Não envia se já está no destino
        if (pos.X == _targetX && pos.Y == _targetY)
            return;

        // Envia requisição ao servidor
        var input = new MoveInput
        {
            SequenceId = ++_sequenceId,
            TargetX = (short)_targetX,
            TargetY = (short)_targetY,
            ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        _networkSender.SendMoveInput(input);
        _inputCooldown = InputCooldownTime;
        _shouldProcess = false;
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