using System;
using System.Collections.Generic;
using Game.Network.Packets.Game;
using Godot;

namespace GodotClient.UI.Chat;

public partial class ChatHud
{
    public static ChatHud CreateInstance()
    {
        var scene = GD.Load<PackedScene>("res://Scenes/Prefabs/ChatHud.tscn");
        var instance = scene.Instantiate<ChatHud>();
        return instance;
    }
}


public partial class ChatHud : Control
{
    [Export]
    public int MaxMessages { get; set; } = 80;

    public event Action<string>? MessageSubmitted;
    public event Action<bool>? InputFocusChanged;

    private PanelContainer? _chatPanel;
    private ScrollContainer? _scrollContainer;
    private VBoxContainer? _historyContainer;
    private LineEdit? _input;
    private Button? _hideButton;
    private Button? _showButton;

    private bool _isInputFocused;

    private readonly Queue<Control> _messageControls = new();

    public override void _Ready()
    {
        // As ancoragens principais vêm da cena
        MouseFilter = MouseFilterEnum.Ignore;
        SetProcessUnhandledInput(true);

        _chatPanel = GetNode<PanelContainer>("ChatPanel");
        _scrollContainer = GetNode<ScrollContainer>("ChatPanel/ChatVBox/ScrollContainer");
        _historyContainer = GetNode<VBoxContainer>("ChatPanel/ChatVBox/ScrollContainer/HistoryContainer");
        _input = GetNode<LineEdit>("ChatPanel/ChatVBox/Input");
        _hideButton = GetNode<Button>("ChatPanel/ChatVBox/ChatHBox/HideButton");
        _showButton = GetNode<Button>("ChatPanel/ChatVBox/ChatHBox/ShowButton");
        
        _hideButton!.Pressed += OnHideButtonPressed;
        _showButton!.Pressed += OnShowButtonPressed;

        _input.TextSubmitted += OnTextSubmitted;

        InitializeInputFocusTracking();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
            return;

        if (_input is null)
            return;

        if (keyEvent.Keycode == Key.Enter)
        {
            if (!_input.HasFocus())
                _input.GrabFocus();
        }
        else if (keyEvent.Keycode == Key.Escape && _input.HasFocus())
        {
            _input.ReleaseFocus();
        }
    }

    public void AppendMessage(ChatMessagePacket packet)
    {
        if (_historyContainer is null)
            return;

        var label = CreateMessageLabel(packet);
        _historyContainer.AddChild(label);
        _messageControls.Enqueue(label);

        while (_messageControls.Count > MaxMessages)
        {
            if (_messageControls.TryDequeue(out var oldControl))
                oldControl.QueueFree();
        }

        ScrollToBottom();
    }

    private static Label CreateMessageLabel(ChatMessagePacket packet)
    {
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(packet.TimestampUnixMs)
            .ToLocalTime()
            .ToString("HH:mm");

        var source = packet.IsSystem ? "SYSTEM" : packet.SenderName;
        var historySuffix = packet.IsHistory ? " (hist)" : string.Empty;

        var label = new Label
        {
            Text = $"[{timestamp}] {source}{historySuffix}: {packet.Message}",
            HorizontalAlignment = HorizontalAlignment.Left,
            MouseFilter = MouseFilterEnum.Ignore
        };

        if (packet.IsSystem)
            label.AddThemeColorOverride("font_color", Colors.SkyBlue);
        else if (packet.IsHistory)
            label.AddThemeColorOverride("font_color", Colors.DimGray);

        return label;
    }

    private void ScrollToBottom()
    {
        if (_scrollContainer is null)
            return;

        var scrollbar = _scrollContainer.GetVScrollBar();
        if (scrollbar is not null)
            scrollbar.Value = scrollbar.MaxValue;
    }

    private void OnTextSubmitted(string text)
    {
        _input?.Clear();
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        MessageSubmitted?.Invoke(trimmed);
    }
    
    private void OnHideButtonPressed()
    {
        _input!.ReleaseFocus();
        
        _scrollContainer!.Visible = false;
        _input!.Visible = false;
        _hideButton!.Visible = false;
        _showButton!.Visible = true;
        _chatPanel!.Size = new Vector2(_chatPanel.Size.X, 32);
        _chatPanel!.Position = new Vector2(15, GetViewportRect().Size.Y - _chatPanel.Size.Y - 15);
    }
    
    private void OnShowButtonPressed()
    {
        _scrollContainer!.Visible = true;
        _input!.Visible = true;
        _hideButton!.Visible = true;
        _showButton!.Visible = false;
        _chatPanel!.Size = new Vector2(_chatPanel.Size.X, 200);
        _chatPanel!.Position =  new Vector2(15, GetViewportRect().Size.Y - _chatPanel.Size.Y - 15);
        _input!.GrabFocus();
    }
    
    private void InitializeInputFocusTracking()
    {
        if (_input is null)
            return;

        _input.FocusEntered += () => HandleInputFocusChanged(true);
        _input.FocusExited += () => HandleInputFocusChanged(false);
        HandleInputFocusChanged(_input.HasFocus());
    }

    private void HandleInputFocusChanged(bool hasFocus)
    {
        if (_isInputFocused == hasFocus)
            return;

        _isInputFocused = hasFocus;
        InputFocusChanged?.Invoke(hasFocus);
    }
}