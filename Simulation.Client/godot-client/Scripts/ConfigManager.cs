using System;
using Application.Models.Models;
using Application.Models.Options;
using Godot;

namespace GodotClient;

public sealed partial class ConfigManager : Node
{
    [Signal] public delegate void ConfigUpdatedEventHandler();

    public WorldOptions? World { get; private set; }
    public NetworkOptions? Network { get; private set; }
    public AuthorityOptions? Authority { get; private set; }

    // C# event opcional para quem preferir subscribe sem usar sinais Godot
    public event Action? ConfigAvailable;

    public bool IsReady => World != null && Network != null && Authority != null;

    public void UpdateFromDto(ConfigDto dto)
    {
        World = dto.World;
        Network = dto.Network;
        Authority = dto.Authority;

        // Emite sinal Godot
        EmitSignal(nameof(ConfigUpdated));
        // Dispara também o evento C#
        ConfigAvailable?.Invoke();
    }
}