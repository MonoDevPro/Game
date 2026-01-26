using System;
using Game.Core.Autoloads;
using Game.Domain;
using Godot;

namespace Game.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
[Tool]
public abstract partial class DefaultVisual : Node2D
{
    public AnimatedSprite2D? Sprite;
    public Label? NameLabel;
    public ProgressBar? HealthBar;
    public ProgressBar? ManaBar;
    private Node2D? _pivot;
    private Direction _currentDirection = Direction.South;
    
    private float _movementAnimationDuration = 1f;
    private float _attackAnimationDuration = 1f;

    public override void _Ready()
    {
        base._Ready();
        _pivot = GetNodeOrNull<Node2D>("Pivot");
        Sprite = GetNodeOrNull<AnimatedSprite2D>("Pivot/AnimatedSprite2D");
        NameLabel = GetNodeOrNull<Label>("Pivot/NameLabel");
        HealthBar = GetNodeOrNull<ProgressBar>("Pivot/HealthBar");
        ManaBar = GetNodeOrNull<ProgressBar>("Pivot/ManaBar");
        
        if (Sprite == null)
        {
            GD.PrintErr("[DefaultVisual] AnimatedSprite2D node not found!");
            Sprite = new AnimatedSprite2D { Name = "Sprite", Position = Vector2.Zero, Centered = true };
            AddChild(Sprite);
        }

        if (NameLabel == null)
        {
            GD.PrintErr("[DefaultVisual] NameLabel node not found!");
            NameLabel = new Label
            {
                Name = "NameLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
                Position = new Vector2(-32, -48),
                Text = string.Empty
            };
            NameLabel.AddThemeColorOverride("font_color", Colors.White);
            AddChild(NameLabel);
        }

        if (HealthBar == null)
        {
            GD.PrintErr("[DefaultVisual] HealthBar node not found!");
            HealthBar = new ProgressBar
            {
                Name = "HealthBar",
                Position = new Vector2(-16, 22),
                Size = new Vector2(32, 4),
                MaxValue = 100,
                Value = 100,
                ShowPercentage = false
            };
            _pivot?.AddChild(HealthBar);
        }
        
        if (ManaBar == null)
        {
            GD.PrintErr("[DefaultVisual] ManaBar node not found!");
            ManaBar = new ProgressBar
            {
                Name = "ManaBar",
                Position = new Vector2(-16, 26),
                Size = new Vector2(32, 4),
                MaxValue = 100,
                Value = 100,
                ShowPercentage = false
            };
            _pivot?.AddChild(ManaBar);
        }
        
        GD.Print("[DefaultVisual] Ready completed.");
    }
    
    public void UpdateAnimationSpeed(float movementDuration = 1f, float attackDuration = 1f)
    {
        if (movementDuration < 0.1f) movementDuration = 0.1f;
        if (attackDuration < 0.1f) attackDuration = 0.1f;
        _movementAnimationDuration = movementDuration;
        _attackAnimationDuration = attackDuration;
        RefreshAnimationSpeeds();
    }
    
    public void RefreshAnimationSpeeds()
    {
        if (Sprite is null) 
            return;
        
        string anim = Sprite.Animation;
        if (string.IsNullOrEmpty(anim) || !Sprite.SpriteFrames.HasAnimation(anim))
            return;
        try
        {
            int frames = Sprite.SpriteFrames.GetFrameCount(anim);
            // Se for idle, mantemos uma velocidade baixa para idle (evita "parado" com 0/0)
            if (anim.StartsWith("idle", StringComparison.OrdinalIgnoreCase))
                Sprite.SpriteFrames.SetAnimationSpeed(anim, 1f); // idle sempre 1
            else if (anim.StartsWith("attack", StringComparison.OrdinalIgnoreCase))
            {
                // frames / seconds = frames per second
                float targetFps = MathF.Max(0.05f, frames / _attackAnimationDuration);
                Sprite.SpriteFrames.SetAnimationSpeed(anim, targetFps);
            }
            else
            {
                // frames * tilesPerSecond => frames/sec
                float targetFps = MathF.Max(0.05f, frames * _movementAnimationDuration);
                Sprite.SpriteFrames.SetAnimationSpeed(anim, targetFps);
            }
        }
        catch
        {
            // fallback seguro
            Sprite.SpriteFrames.SetAnimationSpeed(anim, MathF.Max(1f, _movementAnimationDuration));
        }
    }

    public void UpdatePosition(Vector3I gridPos)
    {
        const float pixelsPerCell = 32f;
        Position = new Vector2(gridPos.X * pixelsPerCell, gridPos.Y * pixelsPerCell);
        ZIndex = gridPos.Z;
    }

    public void LoadSprite(Vocation vocation, Gender gender)
    {
        if (Sprite is null) return;
        var spriteFrames = AssetManager.Instance.GetSpriteFrames(vocation, gender);
        Sprite.SpriteFrames = spriteFrames;
    }

    public void UpdateAnimationState(Direction direction, bool isMoving = false, bool isAttacking = false, bool isDead = false)
    {
        if (Sprite is null) return;
        
        _currentDirection = direction;
        
        UpdateAnimationState(Sprite, isMoving, isAttacking, isDead);
    }
    
    private void UpdateAnimationState(AnimatedSprite2D sprite, bool isMoving = false, bool isAttacking = false, bool isDead = false)
    {
        if (Sprite is null) return;
        
        // Determina animação
        string animation = isMoving ? "walk" : "idle";
        animation = isAttacking ? "attack" : animation;
        
        string animName = _currentDirection switch
        {
            Direction.South or Direction.SouthEast or Direction.SouthWest => $"{animation}_south",
            Direction.North or Direction.NorthEast or Direction.NorthWest => $"{animation}_north",
            Direction.East or Direction.West => $"{animation}_side",
            _ => $"{animation}",
        };
        
        animName = isDead ? "dead" : animName;

        Sprite.FlipH = _currentDirection is Direction.West;
        
        if (sprite.Animation == animName) 
            return;
        
        sprite.Animation = animName;
        sprite.Play();
        
        RefreshAnimationSpeeds();
    }

    public void UpdateVitals(int currentHp, int maxHp, int currentMp, int maxMp)
    {
        if (HealthBar is null) return;
        HealthBar.MaxValue = Math.Max(1, maxHp);
        HealthBar.Value = Mathf.Clamp(currentHp, 0, maxHp);
        
        if (ManaBar is null) return;
        ManaBar.MaxValue = Math.Max(1, maxMp);
        ManaBar.Value = Mathf.Clamp(currentMp, 0, maxMp);
    }

    public void UpdateName(string name)
    {
        if (NameLabel is not null)
            NameLabel.Text = name;
    }
    
    // Exemplo simples de label flutuante de dano
    public void CreateFloatingDamageLabel(int damage, bool critical)
    {
        if (damage <= 0) return;
        var label = new Label
        {
            Text = critical ? $"-{damage}!" : "-" + damage,
            Position = new Vector2(32, -24),
            Modulate = critical ? Colors.Yellow : Colors.Red
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        
        AddChild(label);
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", label.Position.Y - 16, 0.6f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
    }
    
    public void CreateFloatingHealLabel(int healAmount)
    {
        if (healAmount <= 0) return;
        var label = new Label
        {
            Text = $"+{healAmount}",
            Position = new Vector2(32, -24),
            Modulate = Colors.Green,
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        
        AddChild(label);
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", label.Position.Y - 16, 0.6f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
    }
    
    
    public void ChangeTempColor(Color color, float duration = 0.3f)
    {
        if (Sprite is null) return;
        var originalColor = Sprite.Modulate;
        Sprite.Modulate = color;
        var tween = CreateTween();
        tween.TweenProperty(Sprite, "modulate", originalColor, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
    
    public void MakeCamera()
    {
        var camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (camera != null)
        {
            camera.MakeCurrent();
            return;
        }
        camera = GD.Load<PackedScene>("res://Scenes/Prefabs/Camera2D.tscn").Instantiate<Camera2D>();
        AddChild(camera);
        camera.MakeCurrent();
    }
}
