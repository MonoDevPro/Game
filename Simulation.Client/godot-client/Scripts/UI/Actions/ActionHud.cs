using Game.Core.Environment;
using Game.Simulation;
using Godot;

namespace Game.UI.Actions;

public enum ActionHudMode
{
    Always,          // Sempre visível
    TouchscreenOnly, // Visível apenas em plataformas mobile
    WhenActioned     // Visível somente quando alguma ação é executada
}

public partial class ActionHud
{
    public static ActionHud CreateInstance()
    {
        var actionHud = GD.Load<PackedScene>("res://Scenes/Prefabs/ActionHud.tscn")
            .Instantiate<ActionHud>();
        return actionHud;
    }
}

[Tool]
public partial class ActionHud : Control
{
    [Export] public EnvironmentPlatforms SupportedPlatforms = EnvironmentPlatforms.Android | EnvironmentPlatforms.iOS;
    [Export] public ActionHudMode VisibilityMode = ActionHudMode.Always;

    // Ações configuráveis para os botões da HUD
    [ExportGroup("Actions")]
    [Export] public string Action1 = "action_1";
    [Export] public string Action2 = "action_2";

    // Caminhos dos botões na cena (podem ser nulos se você não quiser usar todos)
    [ExportGroup("Button Nodes")]
    [Export] public NodePath Action1ButtonPath;
    [Export] public NodePath Action2ButtonPath;

    private Button _action1Button;
    private Button _action2Button;

    // Controle interno: se a HUD está "ativa" porque uma ação está acontecendo
    private int _activeActionsCount = 0;

    public override void _Ready()
    {
        base._Ready();

        // Se a plataforma atual não for suportada, some com a HUD
        if (!SupportedPlatforms.HasFlag(EnvironmentSettings.CurrentPlatform))
        {
            QueueFree();
            return;
        }

        CacheButtons();
        ConnectButtonSignals();
        UpdateVisibility();
        
        GD.PrintS("ActionHud ready on platform: " + EnvironmentSettings.CurrentPlatform);
        GD.PrintS("Supported platforms: " + SupportedPlatforms);
        GD.PrintS("Visibility mode: " + VisibilityMode);
        GD.PrintS("Action 1: " + Action1);
        GD.PrintS("Action 2: " + Action2);
    }

    private void CacheButtons()
    {
        // Tenta pegar os botões pelos NodePath exportados
        if (!Action1ButtonPath.IsEmpty)
            _action1Button = GetNodeOrNull<Button>(Action1ButtonPath);
        if (!Action2ButtonPath.IsEmpty)
            _action2Button = GetNodeOrNull<Button>(Action2ButtonPath);

        // Fallback: se não foram configurados, tenta pegar por nome
        _action1Button ??= GetNodeOrNull<Button>("Action1Button");
        _action2Button ??= GetNodeOrNull<Button>("Action2Button");
    }

    private void ConnectButtonSignals()
    {
        // Conecta os sinais de cada botão, se existir
        _action1Button.Pressed += OnAction1ButtonPressed;
        _action1Button.ButtonUp += OnAction1ButtonReleased;

        _action2Button.Pressed += OnAction2ButtonPressed;
        _action2Button.ButtonUp += OnAction2ButtonReleased;
    }

    public void UpdateVisibility()
    {
        bool shouldBeVisible = VisibilityMode switch
        {
            ActionHudMode.Always => true,
            ActionHudMode.TouchscreenOnly =>
                EnvironmentSettings.CurrentPlatform.HasFlag(EnvironmentPlatforms.Android) ||
                EnvironmentSettings.CurrentPlatform.HasFlag(EnvironmentPlatforms.iOS),
            ActionHudMode.WhenActioned => _activeActionsCount > 0,
            _ => true
        };
        
        Visible = shouldBeVisible;
    }

    // -------- Métodos públicos para outros sistemas (ex: joystick) --------

    /// <summary>
    /// Chame isto quando alguma ação começar (ex: botão pressionado, joystick forte).
    /// Funciona especialmente quando VisibilityMode = WhenActioned.
    /// </summary>
    public void NotifyActionStarted()
    {
        _activeActionsCount++;
        if (_activeActionsCount < 0)
            _activeActionsCount = 0;
        UpdateVisibility();
    }

    /// <summary>
    /// Chame isto quando a ação terminar (ex: soltar botão).
    /// </summary>
    public void NotifyActionEnded()
    {
        _activeActionsCount--;
        if (_activeActionsCount < 0)
            _activeActionsCount = 0;
        UpdateVisibility();
    }

    // -------- Callbacks dos botões --------

    private void OnAction1ButtonPressed()
    {
        if (string.IsNullOrEmpty(Action1) || GameClient.Instance.IsChatFocused) 
            return;

        Input.ActionPress(Action1, 1.0f);
        NotifyActionStarted();
    }

    private void OnAction1ButtonReleased()
    {
        if (string.IsNullOrEmpty(Action1)) 
            return;

        Input.ActionRelease(Action1);
        NotifyActionEnded();
    }

    private void OnAction2ButtonPressed()
    {
        if (string.IsNullOrEmpty(Action2) || GameClient.Instance.IsChatFocused) 
            return;
        
        Input.ActionPress(Action2, 1.0f);
        NotifyActionStarted();
    }

    private void OnAction2ButtonReleased()
    {
        if (string.IsNullOrEmpty(Action2)) 
            return;
        
        Input.ActionRelease(Action2);
        NotifyActionEnded();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Ao sair da cena, garante que nenhuma ação fique "presa"
        if (!string.IsNullOrEmpty(Action1))
            Input.ActionRelease(Action1);
        if (!string.IsNullOrEmpty(Action2))
            Input.ActionRelease(Action2);

        _activeActionsCount = 0;
    }
}