using System;
using Game.Network.Abstractions;
using Godot;
using Microsoft.Extensions.Configuration;

namespace GodotClient.Core.Autoloads;

public partial class ConfigManager : Node
{
    private static ConfigManager? _instance;
    public static ConfigManager Instance => _instance ?? throw new InvalidOperationException("ConfigManager not initialized");
    
    private readonly ClientConfiguration _configuration;
    public ClientConfiguration Configuration => _configuration;
    
    public ConfigManager()
    {
        _instance = this;
        
        try
        {
            var basePath = ProjectSettings.GlobalizePath("res://");
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var root = builder.Build();
            _configuration = root.Get<ClientConfiguration>() ?? new ClientConfiguration();
        }
        catch (Exception ex)
        {
            GD.PushError($"Failed to load configuration: {ex.Message}");
            _configuration = new ClientConfiguration();
        }
    }   
    
    public override void _Ready()
    {
        base._Ready();
    }

    public NetworkOptions CreateNetworkOptions()
    {
        var network = _configuration.Network ?? new NetworkConfiguration();

        return new NetworkOptions
        {
            IsServer = false,
            ServerAddress = network.ServerAddress,
            ServerPort = network.ServerPort,
            ConnectionKey = network.ConnectionKey,
            PingIntervalMs = network.PingIntervalMs,
            DisconnectTimeoutMs = network.DisconnectTimeoutMs,
        };
    }

    public LoginConfiguration GetLoginConfiguration()
        => _configuration.Login ?? new LoginConfiguration();

    public CharacterSelectionConfiguration GetCharacterSelectionConfiguration()
        => _configuration.CharacterSelection ?? new CharacterSelectionConfiguration();
}

public sealed class ClientConfiguration
{
    public NetworkConfiguration? Network { get; set; }
    public LoginConfiguration? Login { get; set; }
    public CharacterSelectionConfiguration? CharacterSelection { get; set; }
}

public sealed class NetworkConfiguration
{
    public string ServerAddress { get; set; } = "186.219.220.195";
    public int ServerPort { get; set; } = 8001;
    public string ConnectionKey { get; set; } = "default";
    public int PingIntervalMs { get; set; } = 2000;
    public int DisconnectTimeoutMs { get; set; } = 5000;
}

public sealed class LoginConfiguration
{
    public bool AutoLogin { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class CharacterSelectionConfiguration
{
    public int CharacterId { get; set; }
}