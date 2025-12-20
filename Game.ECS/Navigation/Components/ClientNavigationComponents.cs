using System.Runtime.CompilerServices;

namespace Game.ECS.Navigation.Components;

/// <summary>
/// Posição no grid sincronizada do servidor. 
/// </summary>
public struct SyncedGridPosition
{
    public int X;
    public int Y;
    public long LastSyncTick;  // Tick do servidor quando foi sincronizado

    public SyncedGridPosition(int x, int y, long syncTick = 0)
    {
        X = x;
        Y = y;
        LastSyncTick = syncTick;
    }
}

/// <summary>
/// Posição visual para renderização (float, interpolada).
/// </summary>
public struct VisualPosition
{
    public float X;
    public float Y;

    public VisualPosition(float x, float y)
    {
        X = x;
        Y = y;
    }

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public static VisualPosition FromGrid(int gridX, int gridY, float cellSize)
    {
        return new VisualPosition(
            (gridX + 0.5f) * cellSize,
            (gridY + 0.5f) * cellSize
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VisualPosition Lerp(VisualPosition from, VisualPosition to, float t)
    {
        return new VisualPosition(
            from.X + (to.X - from.X) * t,
            from. Y + (to. Y - from.Y) * t
        );
    }

    public readonly float DistanceTo(VisualPosition other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}

/// <summary>
/// Estado de interpolação de movimento no cliente.
/// </summary>
public struct ClientMovementState
{
    public VisualPosition FromPosition;     // Posição inicial da interpolação
    public VisualPosition ToPosition;       // Posição alvo
    public float Progress;                  // 0.0 a 1.0
    public float Duration;                  // Duração em segundos
    public bool IsInterpolating;
    public MovementDirection Direction;

    public readonly bool IsComplete => Progress >= 1.0f;

    public void StartInterpolation(VisualPosition from, VisualPosition to, float duration, MovementDirection dir)
    {
        FromPosition = from;
        ToPosition = to;
        Progress = 0f;
        Duration = duration;
        IsInterpolating = true;
        Direction = dir;
    }

    public void Complete()
    {
        Progress = 1f;
        IsInterpolating = false;
    }

    public void Reset()
    {
        IsInterpolating = false;
        Progress = 0f;
    }

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public readonly VisualPosition GetCurrentPosition()
    {
        if (! IsInterpolating) return ToPosition;
        return VisualPosition. Lerp(FromPosition, ToPosition, Progress);
    }
}

/// <summary>
/// Buffer de movimentos pendentes recebidos do servidor. 
/// Usado para suavizar movimentos quando pacotes chegam em rajadas.
/// </summary>
public struct MovementBuffer
{
    public const int MaxBuffered = 8;

    private unsafe fixed int _targetX[MaxBuffered];
    private unsafe fixed int _targetY[MaxBuffered];
    private unsafe fixed float _durations[MaxBuffered];
    private unsafe fixed byte _directions[MaxBuffered];

    public int Count;
    public int ReadIndex;
    public int WriteIndex;

    public readonly bool HasPending => Count > 0;
    public readonly bool IsFull => Count >= MaxBuffered;

    public unsafe void Enqueue(int targetX, int targetY, float duration, MovementDirection direction)
    {
        if (Count >= MaxBuffered)
        {
            // Buffer cheio - descarta mais antigo
            ReadIndex = (ReadIndex + 1) % MaxBuffered;
            Count--;
        }

        _targetX[WriteIndex] = targetX;
        _targetY[WriteIndex] = targetY;
        _durations[WriteIndex] = duration;
        _directions[WriteIndex] = (byte)direction;

        WriteIndex = (WriteIndex + 1) % MaxBuffered;
        Count++;
    }

    public unsafe bool TryDequeue(out int targetX, out int targetY, out float duration, out MovementDirection direction)
    {
        if (Count <= 0)
        {
            targetX = 0;
            targetY = 0;
            duration = 0;
            direction = MovementDirection.None;
            return false;
        }

        targetX = _targetX[ReadIndex];
        targetY = _targetY[ReadIndex];
        duration = _durations[ReadIndex];
        direction = (MovementDirection)_directions[ReadIndex];

        ReadIndex = (ReadIndex + 1) % MaxBuffered;
        Count--;
        return true;
    }

    public void Clear()
    {
        Count = 0;
        ReadIndex = 0;
        WriteIndex = 0;
    }
}

/// <summary>
/// Configurações visuais do cliente. 
/// </summary>
public struct ClientVisualSettings
{
    public float CellSize;                  // Tamanho da célula em pixels/unidades
    public bool SmoothMovement;             // Interpola movimento? 
    public bool SmoothRotation;             // Interpola rotação? 
    public float InterpolationSpeed;        // Multiplicador de velocidade
    public EasingType EasingFunction;       // Tipo de easing

    public static ClientVisualSettings Default => new()
    {
        CellSize = 32f,
        SmoothMovement = true,
        SmoothRotation = true,
        InterpolationSpeed = 1f,
        EasingFunction = EasingType. SmoothStep
    };

    public static ClientVisualSettings Snappy => new()
    {
        CellSize = 32f,
        SmoothMovement = true,
        SmoothRotation = false,
        InterpolationSpeed = 1.5f,
        EasingFunction = EasingType. QuadOut
    };

    public static ClientVisualSettings Instant => new()
    {
        CellSize = 32f,
        SmoothMovement = false,
        SmoothRotation = false,
        InterpolationSpeed = 1f,
        EasingFunction = EasingType.Linear
    };
}

public enum EasingType :  byte
{
    Linear,
    QuadIn,
    QuadOut,
    QuadInOut,
    SmoothStep,
    SmootherStep
}

/// <summary>
/// Estado de animação baseado no movimento.
/// </summary>
public struct AnimationState
{
    public MovementDirection FacingDirection;
    public AnimationType CurrentAnimation;
    public float AnimationTime;
    public int FrameIndex;

    public void SetAnimation(AnimationType type)
    {
        if (CurrentAnimation != type)
        {
            CurrentAnimation = type;
            AnimationTime = 0;
            FrameIndex = 0;
        }
    }
}

public enum AnimationType : byte
{
    Idle,
    Walking,
    Running
}

/// <summary>
/// Tag:  entidade renderizável com navegação.
/// </summary>
public struct ClientNavigationEntity { }

/// <summary>
/// Tag:  entidade é o player local (tratamento especial).
/// </summary>
public struct LocalPlayer { }

/// <summary>
/// Tag: entidade precisa de sincronização.
/// </summary>
public struct NeedsSync { }