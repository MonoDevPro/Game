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
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
public sealed partial class AnimatedPlayerVisual : Node2D
{
    // --- Configuração ---
    [Export] private float _tileSize = 32f;
    [Export] private float _defaultTilesPerSecond = 5f;
    [Export] private float _positionEpsilonFactor = 0.1f; // 10% do tile

    // --- Nós ---
    private AnimatedSprite2D? _sprite;
    private Label? _nameLabel;
    private ProgressBar? _healthBar;

    // --- Estado de Movimento ---
    private bool _isLocal;
    private Vector2 _currentVisualPosition = Vector2.Zero; // Posição visual atual, interpolada a cada frame.
    private Vector2 _serverPosition = Vector2.Zero; // Última posição autoritativa recebida do servidor (em pixels).
    private Vector2 _predictedPosition = Vector2.Zero; // Posição prevista baseada no input local (em pixels).

    private Coordinate _predictedGridPos = new Coordinate(0, 0);
    private Coordinate _serverGridPos = new Coordinate(0, 0);
    private float PositionEpsilon => MathF.Max(0.5f, _tileSize * _positionEpsilonFactor);

    // --- Estado de Animação ---
    private DirectionEnum _currentDirection = DirectionEnum.South;
    private float _currentSpeedPx;
    private float _currentTilesPerSecond;

    // --- Cache de Colisão s---
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

        _currentVisualPosition = Position;
        _serverPosition = Position;
        _predictedPosition = Position;

        // inicializar grid coords
        _serverGridPos = new Coordinate((int)Math.Round(_serverPosition.X / _tileSize),
            (int)Math.Round(_serverPosition.Y / _tileSize));
        _predictedGridPos = _serverGridPos;

        _currentTilesPerSecond = Math.Max(0.0001f, _defaultTilesPerSecond);
        _currentSpeedPx = _currentTilesPerSecond * _tileSize;

        PlayDirectionalAnimation("idle", _currentDirection);
        UpdateAnimationSpeedForCurrent();
    }

    // ===================================================================
    // 1. LÓGICA DE PREDIÇÃO LOCAL (CHAMADA PELO INPUT MANAGER)
    // ===================================================================

    /// <summary>
    /// Armazena o input do jogador para ser processado no _Process a cada frame.
    /// Isso permite movimento contínuo enquanto a tecla está pressionada.
    /// </summary>
    public void SetLocalMovementInput(GridOffset movement)
    {
        if (!_isLocal) return;

        if (movement.ManhattanDistance() > 1)
            movement = movement.Signed(); // normaliza para -1/0/1

        if (movement == GridOffset.Zero)
        {
            // não altere _predictedGridPos — o personagem completa o passo ativo.
            // Apenas atualiza velocidade de animação; a decisão idle/walk ficará centralizada em _Process.
            UpdateAnimationSpeedForCurrent();
            return;
        }

        var currentPredictedGrid = _predictedGridPos;
        var targetGridPos = new Coordinate(currentPredictedGrid.X + movement.X, currentPredictedGrid.Y + movement.Y);

        _currentDirection = movement.ToDirectionEnum();

        if (CanMoveTo(targetGridPos))
        {
            _predictedGridPos = targetGridPos;
            _predictedPosition = new Vector2(targetGridPos.X * _tileSize, targetGridPos.Y * _tileSize);
        }
    }

    // ===================================================================
    // 2. LÓGICA DE ATUALIZAÇÃO DO SERVIDOR (CHAMADA PELO GAMESCRIPT)
    // ===================================================================
    
    public void Update(PlayerSnapshot snapshot, bool isLocal)
    {
        _isLocal = isLocal;

        LoadSprites(snapshot.Vocation, snapshot.Gender);
        UpdateLabel(snapshot.Name);
        UpdateFromServer(snapshot.Position, snapshot.Facing, snapshot.Speed);
        UpdateFacing(snapshot.Facing);

        if (_sprite is not null)
        {
            _sprite.Modulate = isLocal ? Colors.Chartreuse : Colors.White;
        }
    }

    public void UpdateFromServer(Coordinate serverGridPos, Coordinate facing, float speed)
    {
        UpdateSpeed(speed);
        UpdateFacing(facing);

        var authoritativePosition = new Vector2(serverGridPos.X * _tileSize, serverGridPos.Y * _tileSize);
        _serverPosition = authoritativePosition;
        _serverGridPos = serverGridPos;

        if (!_isLocal)
            return;

        // Reconciliação mínima: se discrepância grande, corrija; senão ignore (client keeps prediction).
        var serverDelta = _predictedPosition.DistanceTo(_serverPosition);
        const float snapThreshold = 1.5f * 32f; // exemplo: 1.5 tiles

        if (serverDelta > snapThreshold)
        {
            // grande diferença: snap para o servidor — limpa o predictedGrid para o servidor
            _predictedGridPos = _serverGridPos;
            _predictedPosition = _serverPosition;
        }
        else
        {
            // pequena diferença: deixe o cliente continuar movendo; atualiza somente a grid base.
            _predictedGridPos = _serverGridPos; // opcional: sincroniza grid base, mas não pos
        }

        // deixe a decisão de animação para o loop centralizado (_Process)
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var target = _isLocal ? _predictedPosition : _serverPosition;

        _currentVisualPosition = _currentVisualPosition.MoveToward(target, _currentSpeedPx * (float)delta);
        Position = _currentVisualPosition;

        var dist = _currentVisualPosition.DistanceTo(target);
        bool isMoving = dist > PositionEpsilon;

        DirectionEnum effectiveDirection;
        if (isMoving)
        {
            var deltaVec = target - _currentVisualPosition;
            effectiveDirection = DetermineDirectionFromDelta(deltaVec);
        }
        else
        {
            effectiveDirection = _currentDirection;
        }

        var baseAnim = isMoving ? "walk" : "idle";
        PlayDirectionalAnimation(baseAnim, effectiveDirection);
        UpdateAnimationSpeedForCurrent();
    }

    private void LoadSprites(VocationType vocation, Gender gender)
    {
        if (_sprite is null) return;

        var spriteFrames = AssetManager.Instance.GetSpriteFrames(vocation, gender);
        _sprite.SpriteFrames = spriteFrames;
        // atualiza a animação atual baseado se estamos andando ou idle (decisão centralizada em _Process)
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

    public void UpdateFacing(Coordinate facing)
    {
        // O servidor é autoridade sobre a direção "estática" do jogador.
        _currentDirection = facing.ToDirectionEnum();
        // não forçamos animação aqui — deixamos o _Process decidir (centralizado).
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

// Determina a direção principal com base no delta de movimento.
// Prioriza o eixo com maior componente absoluta para evitar flicker entre animações.
    private DirectionEnum DetermineDirectionFromDelta(Vector2 delta)
    {
        if (delta.LengthSquared() < 0.0001f)
            return _currentDirection;

        var absX = MathF.Abs(delta.X);
        var absY = MathF.Abs(delta.Y);

        if (absX > absY * 1.1f) // evita troca em ties
            return delta.X > 0 ? DirectionEnum.East : DirectionEnum.West;

        if (absY > absX * 1.1f)
            return delta.Y > 0 ? DirectionEnum.South : DirectionEnum.North;

        // quase diagonal — escolha vertical como padrão
        return delta.Y > 0 ? DirectionEnum.South : DirectionEnum.North;
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
            _nameLabel.Text = name;
    }
}