using System.Runtime.CompilerServices;
using GameECS.Modules.Navigation.Shared.Components;

namespace GameECS.Modules.Navigation.Client.Components;

#region Position Components

/// <summary>
/// Posição sincronizada do servidor.
/// </summary>
public struct SyncedGridPosition
{
    public int X;
    public int Y;
    public long SyncTick;

    public SyncedGridPosition(int x, int y, long tick = 0)
    {
        X = x;
        Y = y;
        SyncTick = tick;
    }

    public readonly GridPosition ToGridPosition() => new(X, Y);
}

/// <summary>
/// Posição visual interpolada (para renderização).
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VisualPosition FromGrid(GridPosition pos, float cellSize)
        => new((pos.X + 0.5f) * cellSize, (pos.Y + 0.5f) * cellSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VisualPosition FromGrid(int x, int y, float cellSize)
        => new((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VisualPosition Lerp(VisualPosition a, VisualPosition b, float t)
        => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    public readonly float DistanceTo(VisualPosition other)
    {
        float dx = X - other.X, dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public readonly float DistanceSquaredTo(VisualPosition other)
    {
        float dx = X - other.X, dy = Y - other.Y;
        return dx * dx + dy * dy;
    }
}

#endregion

#region Interpolation Components

/// <summary>
/// Estado de interpolação visual entre duas posições.
/// </summary>
public struct VisualInterpolation
{
    public VisualPosition From;
    public VisualPosition To;
    public float Progress;
    public float Duration;
    public bool IsActive;
    public MovementDirection Direction;

    public readonly bool IsComplete => Progress >= 1f;

    public void Start(VisualPosition from, VisualPosition to, float duration, MovementDirection dir)
    {
        From = from;
        To = to;
        Progress = 0f;
        Duration = Math.Max(duration, 0.001f); // Evita divisão por zero
        IsActive = true;
        Direction = dir;
    }

    public void Finish()
    {
        Progress = 1f;
        IsActive = false;
    }

    public void Reset()
    {
        IsActive = false;
        Progress = 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly VisualPosition GetCurrentPosition()
        => IsActive ? VisualPosition.Lerp(From, To, Progress) : To;
}

/// <summary>
/// Buffer circular de movimentos pendentes do servidor.
/// </summary>
public struct MovementQueue
{
    public const int Capacity = 8;

    private unsafe fixed int _targetX[Capacity];
    private unsafe fixed int _targetY[Capacity];
    private unsafe fixed float _durations[Capacity];
    private unsafe fixed byte _directions[Capacity];

    public int Count;
    private int _readIdx;
    private int _writeIdx;

    public readonly bool HasItems => Count > 0;
    public readonly bool IsFull => Count >= Capacity;

    public unsafe void Enqueue(int x, int y, float duration, MovementDirection dir)
    {
        if (Count >= Capacity)
        {
            // Buffer cheio - descarta mais antigo
            _readIdx = (_readIdx + 1) % Capacity;
            Count--;
        }

        _targetX[_writeIdx] = x;
        _targetY[_writeIdx] = y;
        _durations[_writeIdx] = duration;
        _directions[_writeIdx] = (byte)dir;

        _writeIdx = (_writeIdx + 1) % Capacity;
        Count++;
    }

    public unsafe bool TryDequeue(out int x, out int y, out float duration, out MovementDirection dir)
    {
        if (Count <= 0)
        {
            x = y = 0;
            duration = 0;
            dir = MovementDirection.None;
            return false;
        }

        x = _targetX[_readIdx];
        y = _targetY[_readIdx];
        duration = _durations[_readIdx];
        dir = (MovementDirection)_directions[_readIdx];

        _readIdx = (_readIdx + 1) % Capacity;
        Count--;
        return true;
    }

    public void Clear()
    {
        Count = 0;
        _readIdx = 0;
        _writeIdx = 0;
    }
}

#endregion

#region Visual Settings

/// <summary>
/// Configurações visuais do cliente.
/// </summary>
public struct ClientVisualConfig
{
    public float CellSize;
    public bool SmoothMovement;
    public float InterpolationSpeed;
    public EasingType Easing;

    public static ClientVisualConfig Default => new()
    {
        CellSize = 32f,
        SmoothMovement = true,
        InterpolationSpeed = 1f,
        Easing = EasingType.SmoothStep
    };

    public static ClientVisualConfig Snappy => new()
    {
        CellSize = 32f,
        SmoothMovement = true,
        InterpolationSpeed = 1.5f,
        Easing = EasingType.QuadOut
    };

    public static ClientVisualConfig Instant => new()
    {
        CellSize = 32f,
        SmoothMovement = false,
        InterpolationSpeed = 1f,
        Easing = EasingType.Linear
    };
}

/// <summary>
/// Tipos de easing para interpolação.
/// </summary>
public enum EasingType : byte
{
    Linear = 0,
    QuadIn,
    QuadOut,
    QuadInOut,
    SmoothStep,
    SmootherStep
}

#endregion

#region Animation Components

/// <summary>
/// Estado de animação do sprite.
/// </summary>
public struct SpriteAnimation
{
    public MovementDirection Facing;
    public AnimationClip Clip;
    public float Time;
    public int Frame;

    public void SetClip(AnimationClip clip)
    {
        if (Clip != clip)
        {
            Clip = clip;
            Time = 0f;
            Frame = 0;
        }
    }

    public void Reset()
    {
        Clip = AnimationClip.Idle;
        Time = 0f;
        Frame = 0;
    }
}

/// <summary>
/// Clips de animação disponíveis.
/// </summary>
public enum AnimationClip : byte
{
    Idle = 0,
    Walk,
    Run,
    Attack,
    Death
}

#endregion

#region Tag Components

/// <summary>
/// Tag: entidade é renderizável com navegação.
/// </summary>
public struct ClientNavigationEntity { }

/// <summary>
/// Tag: entidade é o player local (controlado pelo jogador).
/// </summary>
public struct LocalPlayer { }

/// <summary>
/// Tag: entidade é controlável.
/// </summary>
public struct Controllable { }

/// <summary>
/// Tag: entidade precisa de sincronização com servidor.
/// </summary>
public struct NeedsSync { }

/// <summary>
/// Tag: entidade está visível na tela.
/// </summary>
public struct OnScreen { }

#endregion