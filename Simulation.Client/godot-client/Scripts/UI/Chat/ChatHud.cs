using System;
using System.Collections.Generic;
using Game.Network.Packets.Game;
using Godot;

namespace GodotClient.UI.Chat;

public partial class ChatHud : Control
{
    [Export]
    public int MaxMessages { get; set; } = 80;

    public event Action<string>? MessageSubmitted;

    private ScrollContainer? _scrollContainer;
    private VBoxContainer? _historyContainer;
    private LineEdit? _input;
    private readonly Queue<Control> _messageControls = new();

    public override void _Ready()
    {
        AnchorRight = 1f;
        AnchorBottom = 1f;
        OffsetRight = 0;
        OffsetBottom = 0;
        MouseFilter = MouseFilterEnum.Ignore;
        SetProcessUnhandledInput(true);

        var panel = BuildPanel();
        AddChild(panel);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
            return;

        if (_input is null)
            return;

        // Enter focuses chat, Escape defocuses
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
            {
                oldControl.QueueFree();
            }
        }

        ScrollToBottom();
    }

    private PanelContainer BuildPanel()
    {
        var panel = new PanelContainer
        {
            Name = "ChatPanel",
            AnchorLeft = 0f,
            AnchorRight = 0f,
            AnchorTop = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 16f,
            OffsetRight = 420f,
            OffsetTop = -260f,
            OffsetBottom = -16f,
            MouseFilter = MouseFilterEnum.Stop
        };

        var background = new VBoxContainer
        {
            Name = "ChatVBox",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        var title = new Label
        {
            Text = "Chat",
            HorizontalAlignment = HorizontalAlignment.Left,
            MouseFilter = MouseFilterEnum.Ignore
        };

        _scrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 160),
            MouseFilter = MouseFilterEnum.Pass
        };

        _historyContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _scrollContainer.AddChild(_historyContainer);

        _input = new LineEdit
        {
            PlaceholderText = "Press Enter to focus, ESC to exit...",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _input.TextSubmitted += OnTextSubmitted;

        background.AddChild(title);
        background.AddChild(_scrollContainer);
        background.AddChild(_input);
        panel.AddChild(background);

        return panel;
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
        {
            label.AddThemeColorOverride("font_color", Colors.SkyBlue);
        }
        else if (packet.IsHistory)
        {
            label.AddThemeColorOverride("font_color", Colors.DimGray);
        }

        return label;
    }

    private void ScrollToBottom()
    {
        if (_scrollContainer is null)
            return;

        var scrollbar = _scrollContainer.GetVScrollBar();
        if (scrollbar is not null)
        {
            scrollbar.Value = scrollbar.MaxValue;
        }
    }

    private void OnTextSubmitted(string text)
    {
        _input?.Clear();
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;
        MessageSubmitted?.Invoke(trimmed);
    }
}
