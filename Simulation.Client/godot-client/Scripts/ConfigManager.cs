using System;
using Application.Abstractions.Options;
using Godot;
using GodotClient.API;

namespace GodotClient;

public sealed partial class ConfigManager : Node
{
    [Signal] public delegate void ConfigUpdatedEventHandler();

    public WorldOptions? World { get; private set; }
    public NetworkOptions? Network { get; private set; }
    public AuthorityOptions? Authority { get; private set; }

    public bool IsReady => World != null && Network != null && Authority != null;
    
    public override void _Ready()
    {
        GetNode<ApiClient>("$ApiClient").FetchConfig();
    }

    public void UpdateFromDto(ConfigDto dto)
    {
        World = dto.World;
        Network = dto.Network;
        Authority = dto.Authority;

        // Emite sinal Godot
        EmitSignal(nameof(ConfigUpdated));
    }
}