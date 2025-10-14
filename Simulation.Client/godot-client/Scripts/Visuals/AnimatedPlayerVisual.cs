using System;
using System.Collections.Generic;
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
    // Estrutura para armazenar inputs para reconciliação
    private struct PlayerInputState { public uint Sequence; public DirectionOffset Movement; }
    
    // --- Configuração ---
    [Export] private float _tileSize = 32f;
    [Export] private float _defaultTilesPerSecond = 5f;

    // --- Nós ---
    private AnimatedSprite2D? _sprite;
    private Label? _nameLabel;
    private ProgressBar? _healthBar;

    // --- Estado de Movimento ---
    private bool _isLocal;
    private Vector2 _currentVisualPosition = Vector2.Zero; // Posição visual atual, interpolada a cada frame.
    private Vector2 _serverPosition = Vector2.Zero;       // Última posição autoritativa recebida do servidor (em pixels).
    
    // --- Predição Local ---
    private DirectionOffset _lastInputMovement = DirectionOffset.Zero; // Último input de movimento recebido do InputManager.
    private readonly Queue<PlayerInputState> _pendingInputs = new(); // Fila de inputs enviados ao servidor para reconciliação.
    private uint _inputSequence = 0; // Contador para identificar cada input.
    /// <summary>
    /// Retorna o número de sequência atual para ser enviado ao servidor.
    /// </summary>
    public uint GetCurrentInputSequence() => _inputSequence;
    
    // --- Estado de Animação ---
    private DirectionEnum _currentDirection = DirectionEnum.South;
    private float _currentSpeedPx;
    private float _currentTilesPerSecond;

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

        _currentVisualPosition = Position;
        _serverPosition = Position;
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
    public void SetLocalMovementInput(DirectionOffset movement)
    {
        if (!_isLocal) return;
        _lastInputMovement = movement;
        _inputSequence++; // <<< INCREMENTA AQUI

        // Adiciona o estado de input à fila de predição para futura reconciliação.
        _inputSequence++;
        _pendingInputs.Enqueue(new PlayerInputState
        {
            Sequence = _inputSequence,
            Movement = movement
        });
    }

    // ===================================================================
    // 2. LÓGICA DE ATUALIZAÇÃO DO SERVIDOR (CHAMADA PELO GAMESCRIPT)
    // ===================================================================

    /// <summary>
    /// Atualiza o estado do jogador com base nos dados autoritativos do servidor.
    /// </summary>
    public void UpdateFromServer(Coordinate serverGridPos, Coordinate facing, float speed, uint lastProcessedInputSequence)
    {
        // Atualiza a velocidade e direção com base nos dados do servidor.
        UpdateSpeed(speed);
        UpdateFacing(facing);

        // Define a posição-alvo autoritativa do servidor.
        _serverPosition = new Vector2(serverGridPos.X * _tileSize, serverGridPos.Y * _tileSize);

        // --- RECONCILIAÇÃO ---
        if (_isLocal)
        {
            // Remove da fila todos os inputs que o servidor já processou.
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().Sequence <= lastProcessedInputSequence)
            {
                _pendingInputs.Dequeue();
            }

            // Agora, a posição do servidor é a base. Reaplicamos todos os inputs pendentes
            // sobre essa posição para calcular a nova posição predita "corrigida".
            Vector2 correctedPosition = _serverPosition;
            foreach (var inputState in _pendingInputs)
            {
                correctedPosition = PredictNextPosition(correctedPosition, inputState.Movement);
            }

            // A posição visual atual se torna a posição corrigida.
            // Qualquer "salto" será suavizado pela interpolação no _Process.
            _currentVisualPosition = correctedPosition;
        }
        else
        {
            // Para jogadores remotos, simplesmente usamos a interpolação.
            // Se estiver muito longe, faz o "snap" para a posição correta.
            if (_currentVisualPosition.DistanceTo(_serverPosition) > _tileSize * 2)
            {
                _currentVisualPosition = _serverPosition;
            }
        }
    }

    // ===================================================================
    // 3. LOOP DE JOGO (_PROCESS)
    // ===================================================================

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_isLocal)
        {
            // --- PREDIÇÃO CONTÍNUA ---
            // A cada frame, aplicamos o último input conhecido para mover o personagem localmente.
            _currentVisualPosition = PredictNextPosition(_currentVisualPosition, _lastInputMovement);
            Position = _currentVisualPosition;
        }
        else
        {
            // --- INTERPOLAÇÃO PARA JOGADORES REMOTOS ---
            // Move suavemente em direção à última posição conhecida do servidor.
            _currentVisualPosition = _currentVisualPosition.MoveToward(_serverPosition, _currentSpeedPx * (float)delta);
            Position = _currentVisualPosition;
        }

        // Atualiza a animação com base no estado atual do movimento.
        bool isMoving = _lastInputMovement != DirectionOffset.Zero && _isLocal ||
                        !_isLocal && _currentVisualPosition.DistanceTo(_serverPosition) > 0.1f;
        
        PlayDirectionalAnimation(isMoving ? "walk" : "idle", _currentDirection);
        UpdateAnimationSpeedForCurrent();
    }
    
    /// <summary>
    /// Função auxiliar que calcula a próxima posição com base na posição atual e no input,
    /// validando contra o cache de colisão.
    /// </summary>
    private Vector2 PredictNextPosition(Vector2 currentPixelPos, DirectionOffset movement)
    {
        if (movement == DirectionOffset.Zero)
            return currentPixelPos;
            
        var currentGridPos = new Coordinate(
            Mathf.RoundToInt(currentPixelPos.X / _tileSize),
            Mathf.RoundToInt(currentPixelPos.Y / _tileSize));

        var nextGridPos = currentGridPos + movement;

        if (CanMoveTo(nextGridPos))
        {
            // Calcula a direção do movimento e atualiza a animação
            _currentDirection = new Coordinate(movement.X, movement.Y).ToDirectionEnum();
            
            // Retorna a nova posição em pixels
            return new Vector2(nextGridPos.X * _tileSize, nextGridPos.Y * _tileSize);
        }
        
        // Se não pode mover, retorna a posição atual
        return currentPixelPos;
    }

    public void Update(PlayerSnapshot snapshot, bool isLocal)
    {
        _isLocal = isLocal;

        LoadSprites(snapshot.Vocation, snapshot.Gender);
        UpdateLabel(snapshot.Name);
        UpdateFromServer(snapshot.Position, snapshot.Facing, snapshot.Speed, 0);

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
        PlayDirectionalAnimation(_currentVisualPosition == _serverPosition ? "idle" : "walk", _currentDirection);
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
        _currentDirection = facing.ToDirectionEnum();
        // atualiza animação se necessário (por exemplo trocar idle/walk variant)
        PlayDirectionalAnimation(_currentVisualPosition == _serverPosition ? "idle" : "walk", _currentDirection);
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