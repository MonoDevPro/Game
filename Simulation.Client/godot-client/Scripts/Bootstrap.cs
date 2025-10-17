using Godot;
using GodotClient.Autoloads;

namespace GodotClient;

public partial class Bootstrap : Node
{
    public override void _Ready()
    {
        base._Ready();
        
        // Carrega a cena do menu principal
        SceneManager.Instance.LoadMainMenu();
        
        GD.Print("[Bootstrap] Game started");
    }
}