using System;
using Game.Abstractions;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Packets.DTOs;
using Godot;
using GodotClient.Systems;

namespace GodotClient.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro (modificado)
/// Data: 2025-10-13 21:40:00
/// </summary>
public sealed partial class AnimatedPlayerVisual : Node2D
{
    // --- Editor-configuráveis ---
    [Export] private float _tileSize = 32f;
    [Export] private float _defaultTilesPerSecond = 5f;

    // --- Nodes ---
    private AnimatedSprite2D? _sprite;
    private Label? _nameLabel;
    private ProgressBar? _healthBar;

    // --- Estado ---
    private bool _isLocal;
    private Vector2 _targetPosition = Vector2.Zero; // pixels (para renderização)
    private Vector2 _currentPosition = Vector2.Zero; // pixels (posição visual atual)
    private DirectionEnum _currentDirection = DirectionEnum.South;
    private float _currentSpeedPx;
    private float _currentTilesPerSecond; // <-- tiles / second usado para movimento e animação

    // --- Cache de Colisão ---
    private static byte[]? _collisionCache;
    private static int _mapWidth;
    private static int _mapHeight;

    public override void _Ready()
    {
        base._Ready();

        // Inicialização de nós (igual ao original)
        _sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");
        _nameLabel = GetNodeOrNull<Label>("NameLabel");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");

        if (_sprite == null)
        {
            _sprite = new AnimatedSprite2D { Name = "Sprite", Position = Vector2.Zero, Centered = true };
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

        _currentPosition = Position;
        _targetPosition = Position;

        // inicializa velocidade com padrão
        _currentTilesPerSecond = Math.Max(0.0001f, _defaultTilesPerSecond);
        _currentSpeedPx = _currentTilesPerSecond * _tileSize;

        PlayDirectionalAnimation("idle", _currentDirection);
        UpdateAnimationSpeedForCurrent();
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
        if (_sprite is null) return;

        var spriteFrames = AssetManager.Instance.GetSpriteFrames(vocation, gender);
        _sprite.SpriteFrames = spriteFrames;

        // atualiza a animação atual baseado se estamos andando ou idle
        PlayDirectionalAnimation(_currentPosition == _targetPosition ? "idle" : "walk", _currentDirection);
        UpdateAnimationSpeedForCurrent();
    }

    public static void LoadCollisionCache(byte[] collisionData, int width, int height)
    {
        _collisionCache = collisionData;
        _mapWidth = width;
        _mapHeight = height;
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
    /// Predição de movimento local com rastreamento de sequência.
    /// </summary>
    public void PredictLocalMovement(GridOffset movement)
    {
        if (!_isLocal || movement == GridOffset.Zero)
            return;

        if (_currentPosition.DistanceTo(_targetPosition) > 0.1f)
            return; // Ainda estamos nos movendo, ignora novo input até chegar

        // 1. Calcular direção baseada no input
        _currentDirection = new Coordinate(movement.X, movement.Y).ToDirectionEnum();

        var currentTile = new Coordinate(
            Mathf.RoundToInt(_currentPosition.X / _tileSize),
            Mathf.RoundToInt(_currentPosition.Y / _tileSize));

        // 2. Calcular próxima posição baseada na posição PREDITA atual (não visual)
        var nextTile = new Coordinate(
            currentTile.X + movement.X,
            currentTile.Y + movement.Y);

        // 3. Validar colisão local
        if (!CanMoveTo(nextTile))
            return;

        _targetPosition = new Vector2(nextTile.X * _tileSize, nextTile.Y * _tileSize);
        PlayDirectionalAnimation("walk", _currentDirection);
        UpdateAnimationSpeedForCurrent();
    }

    /// <summary>
    /// Atualiza posição confirmada pelo servidor (com reconciliação para ReliableSequenced).
    /// </summary>
    public void UpdatePosition(Coordinate serverPosition)
    {
        _targetPosition = new Vector2(serverPosition.X * _tileSize, serverPosition.Y * _tileSize);

        // Se estivermos muito longe, snap imediato
        if (_currentPosition.DistanceTo(_targetPosition) > _tileSize)
        {
            _currentPosition = _targetPosition;
            Position = _currentPosition;
            PlayDirectionalAnimation("idle", _currentDirection);
            UpdateAnimationSpeedForCurrent();
            return;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Interpolação suave apenas se não estamos no target
        if (_currentPosition.DistanceTo(_targetPosition) > 0.1f)
        {
            // Interpolação frame-rate independente
            _currentPosition = _currentPosition.MoveToward(_targetPosition, _currentSpeedPx * (float)delta);
            Position = _currentPosition;

            // Se chegamos perto o suficiente, snap para evitar jitter
            float distance = _currentPosition.DistanceTo(_targetPosition);
            if (distance <= 0.1f)
            {
                _currentPosition = _targetPosition;
                Position = _currentPosition;

                // Ao chegar, ir para idle
                PlayDirectionalAnimation("idle", _currentDirection);
                UpdateAnimationSpeedForCurrent();
            }
        }
    }

    public void UpdateFacing(Coordinate facing)
    {
        _currentDirection = facing.ToDirectionEnum();
        // atualiza animação se necessário (por exemplo trocar idle/walk variant)
        PlayDirectionalAnimation(_currentPosition == _targetPosition ? "idle" : "walk", _currentDirection);
        UpdateAnimationSpeedForCurrent();
    }

    /// <summary>
    /// Chame esta função sempre que receber a velocidade "cellsPerSecond" (tilesPerSecond)
    /// do servidor/cliente para manter animação e interpolação sincronizadas.
    /// </summary>
    public void UpdateSpeed(float tilesPerSecond)
    {
        if (tilesPerSecond <= 0f)
            tilesPerSecond = _defaultTilesPerSecond;

        _currentTilesPerSecond = tilesPerSecond;
        _currentSpeedPx = tilesPerSecond * _tileSize;

        UpdateAnimationSpeedForCurrent();
    }

    /// <summary>
    /// Ajusta a velocidade da animação (AnimatedSprite2D.Speed) proporcional a tiles/sec.
    /// Estratégia: queremos que a animação rode proporcionalmente à velocidade de movimento.
    /// Aqui definimos: sprite.Speed = framesInAnimation * tilesPerSecond (com um mínimo).
    /// </summary>
    private void UpdateAnimationSpeedForCurrent()
    {
        if (_sprite == null || _sprite.SpriteFrames == null)
            return;

        string anim = _sprite.Animation;
        if (string.IsNullOrEmpty(anim) || !_sprite.SpriteFrames.HasAnimation(anim))
            return;

        try
        {
            int frames = _sprite.SpriteFrames.GetFrameCount(anim);
            // Se for idle, mantemos uma velocidade baixa para idle (evita "parado" com 0/0)
            if (anim.StartsWith("idle", StringComparison.OrdinalIgnoreCase))
            {
                _sprite.SpriteFrames.SetAnimationSpeed(anim, 1f); // idle sempre 1
            }
            else
            {
                // frames * tilesPerSecond => frames/sec
                float targetFps = MathF.Max(0.05f, frames * _currentTilesPerSecond);
                _sprite.SpriteFrames.SetAnimationSpeed(anim, targetFps);
            }
        }
        catch
        {
            // fallback seguro
            _sprite.SpriteFrames.SetAnimationSpeed(anim, MathF.Max(1f, _currentTilesPerSecond));
        }
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

        _sprite.FlipH = direction == DirectionEnum.West;

        if (_sprite.SpriteFrames != null && _sprite.Animation != animName)
            _sprite.Play(animName);
    }

    public void UpdateVitals(int currentHp, int maxHp, int currentMp, int maxMp)
    {
        if (_healthBar is not null)
        {
            _healthBar.MaxValue = Math.Max(1, maxHp);
            _healthBar.Value = Mathf.Clamp(currentHp, 0, maxHp);
        }
    }

    private void UpdateLabel(string name)
    {
        if (_nameLabel is not null)
        {
            _nameLabel.Text = name;
        }
    }

}