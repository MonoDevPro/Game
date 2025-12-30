using Arch.Core;
using Game.Domain.ValueObjects.Vitals;
using GameECS.Modules.Navigation.Shared.Data;
using GameECS.Shared.Entities.Data;

namespace GameECS.Modules.Navigation.Client.Systems
{
    /// <summary>
    /// Interface de input consumida pelo client stub.
    /// </summary>
    public interface IInputProvider
    {
        bool IsClickPressed();
        (float X, float Y) GetClickWorldPosition();
        (float X, float Y) GetMovementAxis();
    }

    /// <summary>
    /// Interface de envio de input para o servidor.
    /// </summary>
    public interface INetworkSender
    {
        void SendMoveInput(ref MoveInputData input);
    }
}

namespace GameECS.Client
{
    using GameECS.Modules.Navigation.Client.Systems;

    /// <summary>
    /// Interface mínima para visuais usados pelo cliente.
    /// </summary>
    public interface IEntityVisual
    {
        void Initialize(int networkId, int x, int y);
        void Destroy();
    }

    /// <summary>
    /// Módulo de navegação simplificado para o cliente.
    /// </summary>
    public sealed class NavigationModuleStub
    {
        public void OnMovementSnapshot(in MovementSnapshot snapshot) { }
    }

    /// <summary>
    /// Implementação stub de ClientGameSimulation apenas para satisfazer dependências de compilação.
    /// </summary>
    public sealed class ClientGameSimulation
    {
        private readonly Dictionary<int, Entity> _entities = new();

        public ClientGameSimulation(IInputProvider? inputProvider = null, INetworkSender? networkSender = null)
        {
            World = World.Create();
            NavigationModule = new NavigationModuleStub();
        }

        public World World { get; }
        public NavigationModuleStub NavigationModule { get; }

        public void Update(float delta) { }

        public void CreateLocalPlayer(PlayerDto dto, object? visual = null)
        {
            _entities[dto.NetworkId] = Entity.Null;
        }

        public void CreateRemotePlayer(PlayerDto dto, object? visual = null)
        {
            _entities[dto.NetworkId] = Entity.Null;
        }

        public void CreateNpc(NpcData dto, object? visual = null)
        {
            _entities[dto.NetworkId] = Entity.Null;
        }

        public void DestroyAny(int networkId)
        {
            _entities.Remove(networkId);
        }

        public bool TryGetAnyEntity(int networkId, out Entity entity)
        {
            return _entities.TryGetValue(networkId, out entity);
        }

        public void ApplyVitals(in VitalsSnapshot snapshot)
        {
            // Stub: apenas aceita o pacote
        }

        public void Dispose()
        {
            // noop
        }
    }
}
