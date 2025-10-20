using System;
using Game.Domain.Enums;
using Game.ECS.Components;
using Godot;
using GodotClient.Autoloads;

namespace GodotClient.Simulation.Players;

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
    public bool IsLocalPlayer { get; private set; } = false;
    
    public override void _Ready()
    {
        base._Ready();
        Sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        NameLabel = GetNodeOrNull<Label>("NameLabel");
        HealthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        
        if (Sprite is null)
            GD.PrintErr("[PlayerVisual] AnimatedSprite2D node not found!");
        if (NameLabel is null)
            GD.PrintErr("[PlayerVisual] NameLabel node not found!");
        if (HealthBar is null)
            GD.PrintErr("[PlayerVisual] HealthBar node not found!");
        
        // Inicialização de nós (igual ao original)
        Sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");
        NameLabel = GetNodeOrNull<Label>("NameLabel");
        HealthBar = GetNodeOrNull<ProgressBar>("HealthBar");

        if (Sprite == null)
        {
            Sprite = new AnimatedSprite2D { Name = "Sprite", Position = Vector2.Zero, Centered = true };
            AddChild(Sprite);
        }

        if (NameLabel == null)
        {
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
    
    public void UpdateFromSnapshot(PlayerSnapshot snapshot, bool treatAsLocal)
    {
        IsLocalPlayer = treatAsLocal;
        LoadSprite((VocationType)snapshot.Vocation, (Gender)snapshot.Gender);
        UpdateLabel(snapshot.Name);
        UpdateFacing(new Vector2I(snapshot.FacingX, snapshot.FacingY));
        UpdatePosition(new Vector3I(snapshot.PositionX, snapshot.PositionY, snapshot.PositionZ));
        if (Sprite is not null) UpdateAnimationSpeed(Sprite, snapshot.Speed);
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

    public void UpdateFacing(Vector2I facing)
    {
        if (Sprite is null) return;

        bool isMoving = facing.X != 0 || facing.Y != 0;
        UpdateAnimationState(Sprite, facing, isMoving);
    }
    
    private void UpdateAnimationState(AnimatedSprite2D sprite, Vector2I facing, bool isMoving)
    {
        // Determina animação baseado no movimento
        string animation = isMoving ? "walk" : "idle";
        
        FacingEnum facingEnum = ConvertToFacingEnum(facing.X, facing.Y);
        
        // Atualiza direção (flip horizontal se necessário)
        sprite.FlipH = facingEnum == FacingEnum.West;
        
        string animName = facingEnum switch
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

    private void UpdateLabel(string name)
    {
        if (NameLabel is not null)
            NameLabel.Text = name;
    }
}