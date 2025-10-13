using System;
using Game.Abstractions;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Packets.DTOs;
using Godot;
using GodotClient.Systems;

namespace GodotClient.Visuals;

/// <summary>
/// Representação visual do jogador com animações direcionais.
/// Autor: MonoDevPro (refatorado)
/// Data: 2025-10-13
/// Mudanças: movimento frame-rate independente, predição local segura,
/// interpolação consistente para remoto, thresholds configuráveis,
/// inicialização robusta e limpeza de nulos.
/// </summary>
public sealed partial class AnimatedPlayerVisual : Node2D
{
    // --- Editor-configuráveis ---
    [Export] private float TileSize = 32f; // pixels por tile
    [Export] private float DefaultTilesPerSecond = 5f; // velocidade enviada pelo servidor (tiles/s)
    [Export] private float SnapDistanceTiles = 4f; // se a discrepância for maior que isso, "snap" para posição do servidor
    [Export] private float ArriveThreshold = 2f; // pixels para considerar "no alvo"

    // --- Nodes ---
    private AnimatedSprite2D? _sprite;
    private Label? _nameLabel;
    private ProgressBar? _healthBar;

    // --- Estado ---
    private bool _isLocal;
    private Vector2 _targetPosition = Vector2.Zero; // pixels
    private Vector2 _currentPosition = Vector2.Zero; // pixels
    private DirectionEnum _currentDirection = DirectionEnum.South;

    // velocidade em pixels por segundo (derivada da velocidade em tiles/s recebida do servidor)
    private float _currentSpeedPx;

    // Cache de colisão para predição (compartilhado entre todos os players)
    private static byte[]? _collisionCache;
    private static int _mapWidth;
    private static int _mapHeight;

    public bool IsMoving => _currentPosition.DistanceTo(_targetPosition) > 0.1f && _currentPosition != _targetPosition;

    public override void _Ready()
    {
        base._Ready();

        // Tenta encontrar nós existentes (se o Node estiver instanciado via scene file)
        _sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");
        _nameLabel = GetNodeOrNull<Label>("NameLabel");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");

        // Se não houverem nós (instanciação por código), cria-os
        if (_sprite == null)
        {
            _sprite = new AnimatedSprite2D
            {
                Name = "Sprite",
                Position = Vector2.Zero,
                Centered = true
            };
            AddChild(_sprite);
        }

        if (_nameLabel == null)
        {
            _nameLabel = new Label
            {
                Name = "NameLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
                Position = new Vector2(-32, -48),
                Text = string.Empty
            };
            _nameLabel.AddThemeColorOverride("font_color", Colors.White);
            AddChild(_nameLabel);
        }

        if (_healthBar == null)
        {
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

        // Inicializa posições com a posição atual do nó (caso a Scene tenha sido colocada no mapa)
        _currentPosition = Position;
        _targetPosition = Position;

        // Inicializa velocidade padrão (tiles/s -> pixels/s)
        _currentSpeedPx = DefaultTilesPerSecond * TileSize;

        // Garantia de animação inicial
        PlayDirectionalAnimation("idle", _currentDirection);
    }

    /// <summary>
    /// Atualiza o visual de acordo com snapshot (chamado pelo sistema de rede).
    /// </summary>
    public void Update(PlayerSnapshot snapshot, bool isLocal)
    {
        _isLocal = isLocal;

        LoadSprites(snapshot.Vocation, snapshot.Gender);
        UpdateLabel(snapshot.Name);

        // Converte posição em pixels
        var serverPosPx = new Vector2(snapshot.Position.X * TileSize, snapshot.Position.Y * TileSize);

        // Ajuste de correção: se a diferença for muito grande, "snap" para a posição do servidor
        var dist = _currentPosition.DistanceTo(serverPosPx);
        if (!_isLocal && dist > SnapDistanceTiles * TileSize)
        {
            _currentPosition = serverPosPx;
            _targetPosition = serverPosPx;
            Position = _currentPosition;
        }
        else
        {
            // atualiza alvo para interpolação normal
            _targetPosition = serverPosPx;
        }

        // Atualiza facing e velocidade
        UpdateFacing(snapshot.Facing);
        UpdateSpeed(snapshot.Speed);

        // Tint local para fácil visual durante debug
        if (_sprite is not null)
        {
            _sprite.Modulate = isLocal ? Colors.Chartreuse : Colors.White;
        }

        // Se houver movimento do servidor, inicia animação de walk
        if (_currentPosition.DistanceTo(_targetPosition) > ArriveThreshold)
        {
            // determina direção a partir do vetor de movimento
            var delta = (_targetPosition - _currentPosition).Normalized();
            var dir = VectorToDirection(delta);
            _currentDirection = dir;
            PlayDirectionalAnimation("walk", _currentDirection);
        }
        else
        {
            PlayDirectionalAnimation("idle", _currentDirection);
        }
    }

    private void LoadSprites(VocationType vocation, Gender gender)
    {
        if (_sprite is null) return;

        var spriteFrames = AssetManager.Instance.GetSpriteFrames(vocation, gender);
        _sprite.SpriteFrames = spriteFrames;

        // Mantém animação coerente com direção atual
        PlayDirectionalAnimation(_currentPosition == _targetPosition ? "idle" : "walk", _currentDirection);
    }

    /// <summary>
    /// Carrega cache de colisão do mapa (chamado uma vez no início do jogo).
    /// </summary>
    public static void LoadCollisionCache(byte[] collisionData, int width, int height)
    {
        _collisionCache = collisionData;
        _mapWidth = width;
        _mapHeight = height;
        GD.Print($"[AnimatedPlayerVisual] Collision cache loaded: {width}x{height}");
    }

    private static bool CanMoveTo(Coordinate position)
    {
        if (_collisionCache == null || _collisionCache.Length == 0)
            return true;

        if (position.X < 0 || position.Y < 0 || position.X >= _mapWidth || position.Y >= _mapHeight)
            return false;

        var index = position.Y * _mapWidth + position.X;
        return index < _collisionCache.Length && _collisionCache[index] == 0;
    }

    /// <summary>
    /// Predição de movimento local (aplicada apenas para o jogador local).
    /// </summary>
    public void PredictLocalMovement(GridOffset movement)
    {
        if (!_isLocal || movement == GridOffset.Zero)
            return;

        // Assegura que _targetPosition esteja alinhado a tiles antes de predizer
        var currentTile = new Coordinate(
            Mathf.RoundToInt(_targetPosition.X / TileSize),
            Mathf.RoundToInt(_targetPosition.Y / TileSize));

        var nextTile = new Coordinate(currentTile.X + movement.X, currentTile.Y + movement.Y);

        if (!CanMoveTo(nextTile))
        {
            PlayDirectionalAnimation("idle", _currentDirection);
            return;
        }

        // Aplicar predição: atualiza target e inicia animação de walk
        _targetPosition = new Vector2(nextTile.X * TileSize, nextTile.Y * TileSize);

        // Atualiza direção baseada no deslocamento
        var newDirection = new Coordinate(movement.X, movement.Y).ToDirectionEnum();
        _currentDirection = newDirection;

        PlayDirectionalAnimation("walk", _currentDirection);

        // Se estivermos exatamente no tile anterior, garantir que _currentPosition esteja alinhado para evitar "tremores"
        if (_currentPosition.DistanceTo(Position) <= 1f)
        {
            _currentPosition = Position;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Interpolação frame-rate independente usando MoveToward com distância máxima por frame
        if (_currentPosition.DistanceTo(_targetPosition) > ArriveThreshold)
        {
            float maxDistanceThisFrame = _currentSpeedPx * (float)delta;
            _currentPosition = _currentPosition.MoveToward(_targetPosition, maxDistanceThisFrame);
            Position = _currentPosition;

            // Atualiza animação caso comece a mover-se
            if (!(_sprite?.IsPlaying() ?? false))
            {
                PlayDirectionalAnimation("walk", _currentDirection);
            }

            // Se chegou muito perto, snap final
            if (_currentPosition.DistanceTo(_targetPosition) <= ArriveThreshold)
            {
                _currentPosition = _targetPosition;
                Position = _targetPosition;
                PlayDirectionalAnimation("idle", _currentDirection);
            }
        }
    }
    
    /// <summary>
    /// Atualização de posição vinda do servidor (compatível com quem chamava o método antigo).
    /// Mantém comportamento de snap quando a discrepância for grande e atualiza animação/direção.
    /// </summary>
    public void UpdatePosition(Coordinate position)
    {
        var serverPosPx = new Vector2(position.X * TileSize, position.Y * TileSize);
        var dist = _currentPosition.DistanceTo(serverPosPx);
        if (dist > SnapDistanceTiles * TileSize)
        {
            _currentPosition = serverPosPx;
            _targetPosition = serverPosPx;
            Position = _currentPosition;
            PlayDirectionalAnimation("idle", _currentDirection);
            return;
        }
        _targetPosition = serverPosPx;
        var delta = _targetPosition - _currentPosition;
        if (delta.LengthSquared() > 0.0001f)
        {
            var dir = VectorToDirection(delta.Normalized());
            _currentDirection = dir;
            PlayDirectionalAnimation("walk", _currentDirection);
        }
        else
        {
            PlayDirectionalAnimation("idle", _currentDirection);
        }
    }

    public void UpdateFacing(Coordinate facing)
    {
        if (_sprite is null) return;

        var newDirection = facing.ToDirectionEnum();
        if (newDirection != _currentDirection)
        {
            _currentDirection = newDirection;
            if (_currentPosition.DistanceTo(_targetPosition) <= ArriveThreshold)
            {
                PlayDirectionalAnimation("idle", _currentDirection);
            }
        }
    }

    /// <summary>
    /// Atualiza a velocidade de movimento (recebida do servidor em tiles/s).
    /// </summary>
    public void UpdateSpeed(float tilesPerSecond)
    {
        // Proteção contra valores inválidos
        if (tilesPerSecond <= 0f)
            tilesPerSecond = DefaultTilesPerSecond;

        _currentSpeedPx = tilesPerSecond * TileSize;
    }

    private void PlayDirectionalAnimation(string baseAnim, DirectionEnum direction)
    {
        if (_sprite is null) return;

        string animName = direction switch
        {
            DirectionEnum.South or DirectionEnum.SouthEast or DirectionEnum.SouthWest => $"{baseAnim}_south",
            DirectionEnum.North or DirectionEnum.NorthEast or DirectionEnum.NorthWest => $"{baseAnim}_north",
            DirectionEnum.East or DirectionEnum.West => $"{baseAnim}_side",
            _ => $"{baseAnim}_south",
        };

        // Flip horizontal apenas para direção oeste (assumindo frames para lado direito)
        _sprite.FlipH = direction == DirectionEnum.West;

        if (_sprite.SpriteFrames != null && _sprite.Animation != animName)
        {
            _sprite.Play(animName);
        }
    }

    public void UpdateVitals(int currentHp, int maxHp, int currentMp, int maxMp)
    {
        if (_healthBar is not null)
        {
            _healthBar.MaxValue = Math.Max(1, maxHp);
            _healthBar.Value = Mathf.Clamp(currentHp, 0, maxHp);
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

    // Helpers
    private static DirectionEnum VectorToDirection(Vector2 v)
    {
        if (v.LengthSquared() < 0.0001f) return DirectionEnum.South;

        var angle = Mathf.RadToDeg(Mathf.Atan2(-v.Y, v.X)); // y negativo invertido para convenção de direção
        // normaliza [-180,180) para [0,360)
        if (angle < 0) angle += 360f;

        // Dividir em 8 direções
        if (angle >= 337.5f || angle < 22.5f) return DirectionEnum.East;
        if (angle >= 22.5f && angle < 67.5f) return DirectionEnum.NorthEast;
        if (angle >= 67.5f && angle < 112.5f) return DirectionEnum.North;
        if (angle >= 112.5f && angle < 157.5f) return DirectionEnum.NorthWest;
        if (angle >= 157.5f && angle < 202.5f) return DirectionEnum.West;
        if (angle >= 202.5f && angle < 247.5f) return DirectionEnum.SouthWest;
        if (angle >= 247.5f && angle < 292.5f) return DirectionEnum.South;
        return DirectionEnum.SouthEast;
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