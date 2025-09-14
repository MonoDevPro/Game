namespace Simulation.Core.Options;

public enum Authority { Server, Client }
public enum SyncTrigger { OnChange, OnTick, OnSpawn }

/// <summary>
/// Encapsula as opções de configuração para um componente sincronizado.
/// </summary>
/// <summary>
/// Encapsula as opções de configuração para um componente sincronizado.
/// </summary>
public record SyncOptions
{
    public Authority Authority { get; init; } = Authority.Server;
    public SyncTrigger Trigger { get; init; } = SyncTrigger.OnChange;
    public ushort SyncRateTicks { get; init; } = 0; // 0 = a cada tick (se o gatilho for OnTick)

    public static SyncOptions DefaultServer => new() { Authority = Authority.Server };
    public static SyncOptions DefaultClient => new() { Authority = Authority.Client };
}