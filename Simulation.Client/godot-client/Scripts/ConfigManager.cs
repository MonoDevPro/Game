using GameWeb.Application.Common.Options;
using Godot;
using GodotClient.API;
using Simulation.Core;

namespace GodotClient;

public sealed partial class ConfigManager : Node
{
    [Signal] public delegate void ConfigUpdatedEventHandler();

    public WorldOptions? World { get; private set; }
    public NetworkOptions? Network { get; private set; }
    public MapOptions? Map { get; private set; }
    public AuthorityOptions? Authority { get; private set; } = new() { Authority = Simulation.Core.Authority.Client };

    public bool IsReady => World != null && Network != null && Authority != null;
    
    public override void _Ready()
    {
        GetNode<ApiClient>("$ApiClient").FetchConfig();
    }

    public void UpdateFromDto(OptionsDto dto)
    {
        World = dto.World;
        Network = dto.Network;
        Map = dto.Map;

        // Emite sinal Godot
        EmitSignal(nameof(ConfigUpdated));
    }
}