/*namespace Simulation.Core.ECS.NewOptBuilder;

public enum Authority { Server, Client }
public enum SyncTrigger { OnChange, OnTick }

[AttributeUsage(AttributeTargets.Struct)]
public class SynchronizedComponentAttribute(
    Authority authority,
    SyncTrigger trigger = SyncTrigger.OnChange,
    ushort syncRateTicks = 0)
    : Attribute
{
    public Authority Authority { get; } = authority;
    public SyncTrigger Trigger { get; } = trigger;
    public ushort SyncRateTicks { get; } = syncRateTicks;

    /// <summary>
    /// Define o nome do grupo de pacotes para este componente.
    /// Componentes com o mesmo GroupName serão sincronizados juntos num único pacote.
    /// Se for nulo ou vazio, um pacote individual será gerado para este componente.
    /// </summary>
    public string? GroupName { get; set; }
}*/