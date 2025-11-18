using System.Collections.Generic;
using Godot;
using GodotClient.Core.Environment;
using GodotClient.Simulation;

namespace GodotClient.UI.Joystick;

public enum JoystickMode
{
    Fixed, // The joystick doesn't move.
    Dynamic, // Every time the joystick area is pressed, the joystick position is set on the touched position.
    Following // When the finger moves outside the joystick area, the joystick will follow it.
}

public enum VisibilityMode
{
    Always, // Always visible
    TouchscreenOnly, // Visible on touch screens only
    WhenTouched // Visible only when touched
}

public partial class VirtualJoystick
{
    public static VirtualJoystick CreateInstance()
    {
        var joystick = GD.Load<PackedScene>("res://Scenes/Prefabs/Joystick.tscn")
            .Instantiate<VirtualJoystick>();
        return joystick;
    }
    
}


[Tool]
public partial class VirtualJoystick : Control
{
    [Export] public EnvironmentPlatforms SupportedPlatforms = EnvironmentPlatforms.Android | EnvironmentPlatforms.iOS;
    
    [Export] public Color PressedColor = new Color(0.5f, 0.5f, 0.5f);

    [Export(PropertyHint.Range, "0, 200, 1")]
    public float DeadzoneSize = 10;

    [Export(PropertyHint.Range, "0, 500, 1")]
    public float ClampzoneSize = 75;

    [Export] public JoystickMode JoystickMode = JoystickMode.Fixed;
    [Export] public VisibilityMode VisibilityMode = VisibilityMode.Always;
    [Export] public bool UseInputActions = true;
    [Export] public string ActionLeft = "walk_west";
    [Export] public string ActionRight = "walk_east";
    [Export] public string ActionUp = "walk_north";
    [Export] public string ActionDown = "walk_south";
    [Export] public string ActionRunning = "sprint";

    private bool _isPressed = false;
    private Vector2 _output = Vector2.Zero;
    private int _touchIndex = -1;
    private TextureRect _base;
    private TextureRect _tip;
    private Vector2 _baseDefaultPosition;
    private Vector2 _tipDefaultPosition;
    private Color _defaultColor;
    
    public override void _Ready()
    {
        if (!SupportedPlatforms.HasFlag(EnvironmentSettings.CurrentPlatform))
        {
            QueueFree();
            return;
        }
        
        _base = GetNode<TextureRect>("Base");
        _tip = GetNode<TextureRect>("Base/Tip");

        _baseDefaultPosition = _base.Position;
        _tipDefaultPosition = _tip.Position;

        _defaultColor = _tip.Modulate;

        if (ProjectSettings.GetSetting("input_devices/pointing/emulate_mouse_from_touch").AsBool())
            GD.PrintErr("The Project Setting 'emulate_mouse_from_touch' should be set to False");

        if (!ProjectSettings.GetSetting("input_devices/pointing/emulate_touch_from_mouse").AsBool())
            GD.PrintErr("The Project Setting 'emulate_touch_from_mouse' should be set to True");

        /*if (!DisplayServer.IsTouchscreenAvailable() && VisibilityMode == VisibilityMode.TouchscreenOnly)
        {
            Hide();
        }*/

        if (VisibilityMode == VisibilityMode.WhenTouched)
            Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (GameClient.Instance.IsChatFocused)
            return;

        switch (@event)
        {
            case InputEventScreenTouch { Pressed: true } screenTouchEvent:
            {
                if (IsPointInsideJoystickArea(screenTouchEvent.Position) && _touchIndex == -1)
                {
                    if (JoystickMode == JoystickMode.Dynamic || JoystickMode == JoystickMode.Following ||
                        (JoystickMode == JoystickMode.Fixed && IsPointInsideBase(screenTouchEvent.Position)))
                    {
                        if (JoystickMode is JoystickMode.Dynamic or JoystickMode.Following)
                        {
                            MoveBase(screenTouchEvent.Position);
                        }

                        if (VisibilityMode == VisibilityMode.WhenTouched)
                        {
                            Show();
                        }

                        _touchIndex = screenTouchEvent.Index;
                        _tip.Modulate = PressedColor;
                        UpdateJoystick(screenTouchEvent.Position);
                        GetViewport().SetInputAsHandled();
                    }
                }

                break;
            }
            case InputEventScreenTouch screenTouchEvent:
            {
                if (screenTouchEvent.Index == _touchIndex)
                {
                    ResetJoystick();
                    if (VisibilityMode == VisibilityMode.WhenTouched)
                    {
                        Hide();
                    }

                    GetViewport().SetInputAsHandled();
                }

                break;
            }
            case InputEventScreenDrag screenDragEvent:
            {
                if (screenDragEvent.Index == _touchIndex)
                {
                    UpdateJoystick(screenDragEvent.Position);
                    GetViewport().SetInputAsHandled();
                }

                break;
            }
        }
    }

    private void MoveBase(Vector2 newPosition)
    {
        _base.GlobalPosition = newPosition - _base.PivotOffset * GetGlobalTransformWithCanvas().Scale;
    }

    private void MoveTip(Vector2 newPosition)
    {
        _tip.GlobalPosition = newPosition - _tip.PivotOffset * _base.GetGlobalTransformWithCanvas().Scale;
    }

    private bool IsPointInsideJoystickArea(Vector2 point)
    {
        var globalTransform = GetGlobalTransformWithCanvas();
        var scale = globalTransform.Scale;
        bool x = point.X >= GlobalPosition.X && point.X <= GlobalPosition.X + (Size.X * scale.X);
        bool y = point.Y >= GlobalPosition.Y && point.Y <= GlobalPosition.Y + (Size.Y * scale.Y);
        return x && y;
    }

    private Vector2 GetBaseRadius()
    {
        return _base.Size * _base.GetGlobalTransformWithCanvas().Scale / 2;
    }

    private bool IsPointInsideBase(Vector2 point)
    {
        var baseRadius = GetBaseRadius();
        var center = _base.GlobalPosition + baseRadius;
        var vector = point - center;
        return vector.LengthSquared() <= baseRadius.X * baseRadius.X;
    }

    private void UpdateJoystick(Vector2 touchPosition)
    {
        var baseRadius = GetBaseRadius();
        var center = _base.GlobalPosition + baseRadius;
        var vector = touchPosition - center;
        vector = vector.LimitLength(ClampzoneSize);

        if (JoystickMode == JoystickMode.Following && touchPosition.DistanceTo(center) > ClampzoneSize)
        {
            MoveBase(touchPosition - vector);
        }

        MoveTip(center + vector);

        if (vector.LengthSquared() > DeadzoneSize * DeadzoneSize)
        {
            _isPressed = true;
            _output = (vector - (vector.Normalized() * DeadzoneSize)) / (ClampzoneSize - DeadzoneSize);
        }
        else
        {
            _isPressed = false;
            _output = Vector2.Zero;
        }

        if (UseInputActions)
        {
            ProcessInputActions();
        }
    }

    private void ProcessInputActions()
    {
        if (GameClient.Instance.IsChatFocused)
        {
            foreach (var action in new[] { ActionLeft, ActionRight, ActionDown, ActionUp, ActionRunning })
            {
                if (!string.IsNullOrEmpty(action))
                    Input.ActionRelease(action);
            }
            return;
        }
        
        // IsRunning set by strength of the joystick
        if (_output.Length() > 0.8)
        {
            Input.ActionPress(ActionRunning, 1);
        }
        else
        {
            Input.ActionRelease(ActionRunning);
        }
        
        if (_output.X > 0.2 && _output.Y < -0.2)
        {
            Input.ActionRelease(ActionDown);
            Input.ActionRelease(ActionLeft);
            Input.ActionPress(ActionRight, 1);
            Input.ActionPress(ActionUp, 1);
        }
        else if (_output.X < -0.2 && _output.Y < -0.2)
        {
            Input.ActionRelease(ActionDown);
            Input.ActionRelease(ActionRight);
            Input.ActionPress(ActionLeft, 1);
            Input.ActionPress(ActionUp, 1);
        }
        else if (_output.X > 0.2 && _output.Y > 0.2)
        {
            Input.ActionRelease(ActionUp);
            Input.ActionRelease(ActionLeft);
            Input.ActionPress(ActionRight, 1);
            Input.ActionPress(ActionDown, 1);
        }
        else if (_output.X < -0.2 && _output.Y > 0.2)
        {
            Input.ActionRelease(ActionUp);
            Input.ActionRelease(ActionRight);
            Input.ActionPress(ActionLeft, 1);
            Input.ActionPress(ActionDown, 1);
        }
        else if (_output.X > 0.2)
        {
            Input.ActionRelease(ActionLeft);
            Input.ActionRelease(ActionUp);
            Input.ActionRelease(ActionDown);
            Input.ActionPress(ActionRight, 1);
        }
        else if (_output.X < -0.2)
        {
            Input.ActionRelease(ActionRight);
            Input.ActionRelease(ActionUp);
            Input.ActionRelease(ActionDown);
            Input.ActionPress(ActionLeft, 1);
        }
        else if (_output.Y < -0.2)
        {
            Input.ActionRelease(ActionDown);
            Input.ActionRelease(ActionLeft);
            Input.ActionRelease(ActionRight);
            Input.ActionPress(ActionUp, 1);
        }
        else if (_output.Y > 0.2)
        {
            GD.Print(_output);
            Input.ActionRelease(ActionUp);
            Input.ActionRelease(ActionLeft);
            Input.ActionRelease(ActionRight);
            Input.ActionPress(ActionDown, 1);
        }
    }

    private void ResetJoystick()
    {
        _isPressed = false;
        _output = Vector2.Zero;
        _touchIndex = -1;
        _tip.Modulate = _defaultColor;
        _base.Position = _baseDefaultPosition;
        _tip.Position = _tipDefaultPosition;
        
        if (!UseInputActions) return;
        foreach (var action in new[] { ActionLeft, ActionRight, ActionDown, ActionUp })
        {
            Input.ActionRelease(action);
        }
    }
}