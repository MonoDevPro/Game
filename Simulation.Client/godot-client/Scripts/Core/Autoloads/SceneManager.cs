using System;
using System.Threading.Tasks;
using Godot;

namespace GodotClient.Core.Autoloads;

/// <summary>
/// Gerenciador centralizado de cenas com transições suaves.
/// Autor: MonoDevPro
/// Data: 2025-01-11 21:16:36
/// </summary>
public partial class SceneManager : Node
{
    private static SceneManager? _instance;
    public static SceneManager Instance => _instance ?? throw new InvalidOperationException("SceneManager not initialized");

    private Node? _currentScene;
    private CanvasLayer? _transitionLayer;
    private ColorRect? _fadeRect;
    
    private const string MenuScenePath = "res://Scenes/menu.tscn";
    private const string GameScenePath = "res://Scenes/game.tscn";

    public override void _Ready()
    {
        base._Ready();
        _instance = this;
        
        CreateTransitionLayer();
        
        GD.Print("[SceneManager] Initialized");
    }

    /// <summary>
    /// Carrega cena do menu principal.
    /// </summary>
    public async void LoadMainMenu()
    {
        try
        {
            await TransitionToScene(MenuScenePath);
        }
        catch (Exception e)
        {
            GD.PushError($"[SceneManager] Failed to load main menu: {e.Message}");
        }
    }

    /// <summary>
    /// Carrega cena do jogo.
    /// </summary>
    public async Task LoadGame()
    {
        await TransitionToScene(GameScenePath);
    }

    /// <summary>
    /// Transição suave entre cenas.
    /// </summary>
    private async System.Threading.Tasks.Task TransitionToScene(string scenePath)
    {
        GD.Print($"[SceneManager] Loading scene: {scenePath}");

        // 1. Fade out
        await FadeOut();

        // 2. Descarrega cena atual
        if (_currentScene != null)
        {
            GD.Print($"[SceneManager] Unloading: {_currentScene.Name}");
            _currentScene.QueueFree();
            _currentScene = null;
        }

        // 3. Aguarda um frame (garante que QueueFree executou)
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // 4. Carrega nova cena
        var packedScene = GD.Load<PackedScene>(scenePath);
        if (packedScene is null)
        {
            GD.PushError($"[SceneManager] Failed to load scene: {scenePath}");
            await FadeIn();
            return;
        }

        _currentScene = packedScene.Instantiate();
        GetTree().Root.AddChild(_currentScene);
        
        GD.Print($"[SceneManager] Loaded: {_currentScene.Name}");

        // 5. Fade in
        await FadeIn();
    }

    /// <summary>
    /// Cria layer de transição (fade to black).
    /// </summary>
    private void CreateTransitionLayer()
    {
        _transitionLayer = new CanvasLayer
        {
            Name = "TransitionLayer",
            Layer = 100 // Sempre por cima
        };
        AddChild(_transitionLayer);

        _fadeRect = new ColorRect
        {
            Color = new Color(0, 0, 0, 0), // Transparente inicial
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _transitionLayer.AddChild(_fadeRect);
    }

    private async System.Threading.Tasks.Task FadeOut(float duration = 0.3f)
    {
        if (_fadeRect is null) return;

        _fadeRect.MouseFilter = Control.MouseFilterEnum.Stop; // Bloqueia input

        var tween = CreateTween();
        tween.TweenProperty(_fadeRect, "color:a", 1.0f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async System.Threading.Tasks.Task FadeIn(float duration = 0.3f)
    {
        if (_fadeRect is null) return;

        var tween = CreateTween();
        tween.TweenProperty(_fadeRect, "color:a", 0.0f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);

        _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore; // Libera input
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _instance = null;
    }
}