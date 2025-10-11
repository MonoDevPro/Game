using Game.Abstractions;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Packets.DTOs;
using Godot;
using GodotClient.Systems;

namespace GodotClient.Visuals;

/// <summary>
/// Representação visual do jogador com animações direcionais.
/// Autor: MonoDevPro
/// Data: 2025-01-11 15:38:44
/// </summary>
public sealed partial class AnimatedPlayerVisual : Node2D
{
    private const float TileSize = 32f;
    private const float MoveSpeed = 8f;
    
    private AnimatedSprite2D? _sprite;
    private Label? _nameLabel;
    private ProgressBar? _healthBar;
    private bool _isLocal;
    
    private Vector2 _targetPosition;
    private Vector2 _currentPosition;
    private DirectionEnum _currentDirection = DirectionEnum.South;

    public override void _Ready()
    {
        base._Ready();
        
        _sprite = new AnimatedSprite2D
        {
            Name = "Sprite",
            Position = Vector2.Zero,
            Centered = true
        };
        AddChild(_sprite);

        _nameLabel = new Label
        {
            Name = "NameLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-32, -48),
            Text = string.Empty
        };
        _nameLabel.AddThemeColorOverride("font_color", Colors.White);
        AddChild(_nameLabel);

        _healthBar = new ProgressBar
        {
            Name = "HealthBar",
            Position = new Vector2(-16, -40),
            Size = new Vector2(32, 4),
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false
        };
        AddChild(_healthBar);
    }

    public void Update(PlayerSnapshot snapshot, bool isLocal)
    {
        _isLocal = isLocal;
        
        LoadSprites(snapshot.Vocation, snapshot.Gender);
        UpdateLabel(snapshot.Name);
        UpdatePosition(snapshot.Position);
        UpdateFacing(snapshot.Facing);
        
        if (_sprite is not null)
        {
            _sprite.Modulate = isLocal ? Colors.Chartreuse : Colors.White;
        }
    }

    private void LoadSprites(VocationType vocation, Gender gender)
    {
        if (_sprite is null)
            return;

        var spriteFrames = AssetManager.Instance.GetSpriteFrames(vocation, gender);
        _sprite.SpriteFrames = spriteFrames;
        
        // ✅ FIX: Inicia com animação direcional
        PlayDirectionalAnimation("idle", _currentDirection);
    }

    public void UpdatePosition(Coordinate position)
    {
        _targetPosition = new Vector2(position.X * TileSize, position.Y * TileSize);
        
        if (_isLocal)
        {
            _currentPosition = _targetPosition;
            Position = _currentPosition;
        }
        else if (_currentPosition != _targetPosition)
        {
            // ✅ FIX: Usa animação direcional
            PlayDirectionalAnimation("walk", _currentDirection);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (!_isLocal && _currentPosition != _targetPosition)
        {
            var distance = _currentPosition.DistanceTo(_targetPosition);
            
            if (distance < 0.5f)
            {
                _currentPosition = _targetPosition;
                Position = _currentPosition;
                
                // ✅ FIX: Usa animação direcional
                PlayDirectionalAnimation("idle", _currentDirection);
            }
            else
            {
                _currentPosition = _currentPosition.Lerp(_targetPosition, (float)delta * MoveSpeed);
                Position = _currentPosition;
            }
        }
    }

    public void UpdateFacing(Coordinate facing)
    {
        if (_sprite is null)
            return;

        var newDirection = facing.ToDirectionEnum();
        
        // ✅ OTIMIZAÇÃO: Só atualiza se a direção mudou
        if (newDirection != _currentDirection)
        {
            _currentDirection = newDirection;
    
            // Atualiza animação se estiver parado
            if (_currentPosition == _targetPosition)
            {
                PlayDirectionalAnimation("idle", _currentDirection);
            }
        }
    }

    private void PlayDirectionalAnimation(string baseAnim, DirectionEnum direction)
    {
        if (_sprite is null)
            return;

        string animName;

        switch (direction)
        {
            case DirectionEnum.South:
            case DirectionEnum.SouthEast:
            case DirectionEnum.SouthWest:
                animName = $"{baseAnim}_south";
                break;
                
            case DirectionEnum.North:
            case DirectionEnum.NorthEast:
            case DirectionEnum.NorthWest:
                animName = $"{baseAnim}_north";
                break;
                
            case DirectionEnum.East:
            case DirectionEnum.West:
                animName = $"{baseAnim}_side";
                break;

            default:
                animName = $"{baseAnim}_south";
                break;
        }

        _sprite.FlipH = direction == DirectionEnum.West;
    
        // ✅ FIX: Só muda se animação for diferente
        if (_sprite.Animation != animName)
        {
            _sprite.Play(animName);
        }
    }

    public void UpdateVitals(int currentHp, int maxHp, int currentMp, int maxMp)
    {
        if (_healthBar is not null)
        {
            _healthBar.MaxValue = maxHp;
            _healthBar.Value = currentHp;
        }
    }

    public void ShowDamageNumber(int damage, DamageType damageType)
    {
        var damageLabel = new Label
        {
            Text = $"-{damage}",
            Position = new Vector2(0, -50),
            Modulate = GetDamageColor(damageType)
        };
        AddChild(damageLabel);

        var tween = CreateTween();
        tween.TweenProperty(damageLabel, "position:y", -80, 1.0f);
        tween.Parallel().TweenProperty(damageLabel, "modulate:a", 0.0f, 1.0f);
        tween.TweenCallback(Callable.From(damageLabel.QueueFree));
    }

    private void UpdateLabel(string name)
    {
        if (_nameLabel is not null)
        {
            _nameLabel.Text = name;
        }
    }

    private static Color GetDamageColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => Colors.White,
            DamageType.Magical => Colors.Cyan,
            DamageType.Fire => Colors.OrangeRed,
            DamageType.Ice => Colors.LightBlue,
            DamageType.Lightning => Colors.Yellow,
            DamageType.Poison => Colors.Green,
            _ => Colors.White
        };
    }
}

public enum DamageType
{
    Physical,
    Magical,
    Fire,
    Ice,
    Lightning,
    Poison,
    True
}