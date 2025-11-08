using System;
using Game.Domain.Enums;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Godot;
using GodotClient.Core.Autoloads;

namespace GodotClient.Simulation;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
public sealed partial class PlayerVisual : Node2D
{
    public AnimatedSprite2D? Sprite;
    public Label? NameLabel;
    public ProgressBar? HealthBar;
    private FacingEnum _currentFacing = FacingEnum.South;
    
    public static PlayerVisual Create()
    {
        return GD.Load<PackedScene>("res://Scenes/Prefabs/PlayerVisual.tscn").Instantiate<PlayerVisual>();
    }
    
    public override void _Ready()
    {
        base._Ready();
        Sprite = GetNodeOrNull<AnimatedSprite2D>("Pivot/AnimatedSprite2D");
        NameLabel = GetNodeOrNull<Label>("Pivot/NameLabel");
        HealthBar = GetNodeOrNull<ProgressBar>("Pivot/HealthBar");
        
        if (Sprite == null)
        {
            GD.PrintErr("[PlayerVisual] AnimatedSprite2D node not found!");
            Sprite = new AnimatedSprite2D { Name = "Sprite", Position = Vector2.Zero, Centered = true };
            AddChild(Sprite);
        }

        if (NameLabel == null)
        {
            GD.PrintErr("[PlayerVisual] NameLabel node not found!");
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
            GD.PrintErr("[PlayerVisual] HealthBar node not found!");
            HealthBar = new ProgressBar
            {
                Name = "HealthBar",
                Position = new Vector2(-16, -40),
                Size = new Vector2(32, 4),
                MaxValue = 100,
                Value = 100,
                ShowPercentage = false
            };
            AddChild(HealthBar);
        }
    }
    
    public void UpdateFromSnapshot(PlayerData data)
    {
        LoadSprite(VocationType.Archer, Gender.Male);
        UpdateName(data.Name);
        UpdateFacing(new Vector2I(data.FacingX, data.FacingY), false);
        UpdatePosition(new Vector3I(data.SpawnX, data.SpawnY, data.SpawnZ));
        if (Sprite is not null) UpdateAnimationSpeed(Sprite, data.MovementSpeed);
    }

    private void UpdateAnimationSpeed(AnimatedSprite2D sprite, float speed)
    {
        string anim = sprite.Animation;
        if (string.IsNullOrEmpty(anim) || !sprite.SpriteFrames.HasAnimation(anim))
            return;
        try
        {
            int frames = sprite.SpriteFrames.GetFrameCount(anim);
            // Se for idle, mantemos uma velocidade baixa para idle (evita "parado" com 0/0)
            if (anim.StartsWith("idle", StringComparison.OrdinalIgnoreCase))
                sprite.SpriteFrames.SetAnimationSpeed(anim, 1f); // idle sempre 1
            else
            {
                // frames * tilesPerSecond => frames/sec
                float targetFps = MathF.Max(0.05f, frames * speed);
                sprite.SpriteFrames.SetAnimationSpeed(anim, targetFps);
            }
        }
        catch
        {
            // fallback seguro
            sprite.SpriteFrames.SetAnimationSpeed(anim, MathF.Max(1f, speed));
        }
    }
    
    private void UpdatePosition(Vector3I gridPos)
    {
        Position = new Vector2(gridPos.X, gridPos.Y);
        ZIndex = gridPos.Z;
    }

    private void LoadSprite(VocationType vocation, Gender gender)
    {
        if (Sprite is null) return;

        var spriteFrames = AssetManager.Instance.GetSpriteFrames(vocation, gender);
        Sprite.SpriteFrames = spriteFrames;
    }

    public void UpdateFacing(Vector2I facing, bool isMoving)
    {
        _currentFacing = ConvertToFacingEnum(facing.X, facing.Y);
        if (Sprite is null) return;
        UpdateAnimationState(Sprite, isMoving);
    }
    
    private void UpdateAnimationState(AnimatedSprite2D sprite, bool isMoving)
    {
        // Determina animação baseado no movimento
        string animation = isMoving ? "walk" : "idle";
        
        // Atualiza direção (flip horizontal se necessário)
        sprite.FlipH = _currentFacing == FacingEnum.West;
        
        string animName = _currentFacing switch
        {
            FacingEnum.South or FacingEnum.SouthEast or FacingEnum.SouthWest => $"{animation}_south",
            FacingEnum.North or FacingEnum.NorthEast or FacingEnum.NorthWest => $"{animation}_north",
            FacingEnum.East or FacingEnum.West => $"{animation}_side",
            _ => $"{animation}_south",
        };

        if (sprite.Animation == animName) 
            return;
        
        sprite.Animation = animName;
        sprite.Play();
    }
    
    public void PlayAttackAnimation(AttackType type)
    {
        if (Sprite is null) 
            return;

        string animName = type switch
        {
            AttackType.Basic => "attack",
            AttackType.Heavy => "attack",
            AttackType.Critical => "attack",
            AttackType.Magic => "attack",
            _ => "attack"
        };
        
        Sprite.FlipH = _currentFacing == FacingEnum.West;
        animName = _currentFacing switch
        {
            FacingEnum.South or FacingEnum.SouthEast or FacingEnum.SouthWest => $"{animName}_south",
            FacingEnum.North or FacingEnum.NorthEast or FacingEnum.NorthWest => $"{animName}_north",
            FacingEnum.East or FacingEnum.West => $"{animName}_side",
            _ => $"{animName}_south",
        };

        if (Sprite.Animation == animName) 
            return;
        
        Sprite.Animation = animName;
        Sprite.Play();
    }
    
    private FacingEnum ConvertToFacingEnum(int facingX, int facingY)
    {
        return (facingX, facingY) switch
        {
            (0, -1) => FacingEnum.North,
            (1, -1) => FacingEnum.NorthEast,
            (-1, -1) => FacingEnum.NorthWest,
            (0, 1) => FacingEnum.South,
            (1, 1) => FacingEnum.SouthEast,
            (-1, 1) => FacingEnum.SouthWest,
            (1, 0) => FacingEnum.East,
            (-1, 0) => FacingEnum.West,
            _ => FacingEnum.None
        };
    }

    public void UpdateVitals(int currentHp, int maxHp, int currentMp, int maxMp)
    {
        if (HealthBar is not null)
        {
            HealthBar.MaxValue = Math.Max(1, maxHp);
            HealthBar.Value = Mathf.Clamp(currentHp, 0, maxHp);
        }
    }

    private void UpdateName(string name)
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
            Text = critical ? $"{damage}!" : damage.ToString(),
            Position = new Vector2(0, -24),
            Modulate = critical ? Colors.Yellow : Colors.White
        };
        AddChild(label);
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", label.Position.Y - 16, 0.6f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
    }
}