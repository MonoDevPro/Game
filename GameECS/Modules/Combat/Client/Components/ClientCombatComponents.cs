using System.Runtime.CompilerServices;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Combat.Shared.Data;

namespace GameECS.Modules.Combat.Client.Components;

#region Visual Components

/// <summary>
/// Posição visual de vida sincronizada do servidor.
/// </summary>
public struct SyncedHealth
{
    public int Current;
    public int Maximum;
    public long SyncTick;

    public readonly float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;
    public readonly bool IsDead => Current <= 0;

    public void Sync(int current, int max, long tick)
    {
        Current = current;
        Maximum = max;
        SyncTick = tick;
    }
}

/// <summary>
/// Posição visual de mana sincronizada do servidor.
/// </summary>
public struct SyncedMana
{
    public int Current;
    public int Maximum;
    public long SyncTick;

    public readonly float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;

    public void Sync(int current, int max, long tick)
    {
        Current = current;
        Maximum = max;
        SyncTick = tick;
    }
}

/// <summary>
/// Barra de vida visual com interpolação suave.
/// </summary>
public struct HealthBar
{
    public float DisplayPercentage;
    public float TargetPercentage;
    public float InterpolationSpeed;
    public bool IsVisible;

    public static HealthBar Default => new()
    {
        DisplayPercentage = 1f,
        TargetPercentage = 1f,
        InterpolationSpeed = 5f,
        IsVisible = true
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(float deltaTime)
    {
        float diff = TargetPercentage - DisplayPercentage;
        DisplayPercentage += diff * Math.Min(1f, InterpolationSpeed * deltaTime);
    }

    public void SetTarget(float percentage)
    {
        TargetPercentage = Math.Clamp(percentage, 0f, 1f);
    }

    public void SetImmediate(float percentage)
    {
        TargetPercentage = Math.Clamp(percentage, 0f, 1f);
        DisplayPercentage = TargetPercentage;
    }
}

/// <summary>
/// Barra de mana visual com interpolação suave.
/// </summary>
public struct ManaBar
{
    public float DisplayPercentage;
    public float TargetPercentage;
    public float InterpolationSpeed;
    public bool IsVisible;

    public static ManaBar Default => new()
    {
        DisplayPercentage = 1f,
        TargetPercentage = 1f,
        InterpolationSpeed = 8f,
        IsVisible = true
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(float deltaTime)
    {
        float diff = TargetPercentage - DisplayPercentage;
        DisplayPercentage += diff * Math.Min(1f, InterpolationSpeed * deltaTime);
    }

    public void SetTarget(float percentage)
    {
        TargetPercentage = Math.Clamp(percentage, 0f, 1f);
    }
}

#endregion

#region Animation Components

/// <summary>
/// Estado de animação de ataque.
/// </summary>
public struct AttackAnimation
{
    public AttackAnimationState State;
    public float Progress;
    public float Duration;
    public int TargetEntityId;
    public VocationType AttackerVocation;

    public readonly bool IsComplete => Progress >= 1f || State == AttackAnimationState.None;

    public void Start(int targetId, VocationType vocation, float duration)
    {
        State = AttackAnimationState.WindUp;
        Progress = 0f;
        Duration = duration;
        TargetEntityId = targetId;
        AttackerVocation = vocation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(float deltaTime)
    {
        if (State == AttackAnimationState.None) return;

        Progress += deltaTime / Duration;

        if (Progress >= 0.3f && State == AttackAnimationState.WindUp)
            State = AttackAnimationState.Strike;
        else if (Progress >= 0.7f && State == AttackAnimationState.Strike)
            State = AttackAnimationState.Recovery;
        else if (Progress >= 1f)
            State = AttackAnimationState.None;
    }

    public void Cancel()
    {
        State = AttackAnimationState.None;
        Progress = 0f;
    }
}

public enum AttackAnimationState : byte
{
    None = 0,
    WindUp,     // Preparação do ataque
    Strike,     // Momento do golpe
    Recovery    // Recuperação pós-ataque
}

/// <summary>
/// Efeito visual de dano flutuante.
/// </summary>
public struct FloatingDamageText
{
    public int Damage;
    public float PositionX;
    public float PositionY;
    public float VelocityY;
    public float Lifetime;
    public float MaxLifetime;
    public bool IsCritical;
    public DamageType Type;

    public readonly bool IsExpired => Lifetime >= MaxLifetime;
    public readonly float Alpha => 1f - (Lifetime / MaxLifetime);

    public static FloatingDamageText Create(int damage, float x, float y, bool isCritical, DamageType type)
    {
        return new FloatingDamageText
        {
            Damage = damage,
            PositionX = x,
            PositionY = y,
            VelocityY = -50f,  // Move para cima
            Lifetime = 0f,
            MaxLifetime = 1.5f,
            IsCritical = isCritical,
            Type = type
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(float deltaTime)
    {
        PositionY += VelocityY * deltaTime;
        VelocityY *= 0.95f;  // Desacelera
        Lifetime += deltaTime;
    }
}

/// <summary>
/// Buffer de textos de dano flutuantes.
/// </summary>
public struct FloatingDamageBuffer
{
    public const int MaxTexts = 8;

    private FloatingDamageText _text0, _text1, _text2, _text3;
    private FloatingDamageText _text4, _text5, _text6, _text7;
    public int Count;

    public void Add(FloatingDamageText text)
    {
        if (Count >= MaxTexts)
        {
            // Remove o mais antigo
            ShiftLeft();
        }

        SetAt(Count, text);
        Count++;
    }

    public FloatingDamageText GetAt(int index)
    {
        return index switch
        {
            0 => _text0,
            1 => _text1,
            2 => _text2,
            3 => _text3,
            4 => _text4,
            5 => _text5,
            6 => _text6,
            _ => _text7
        };
    }

    public void UpdateAt(int index, FloatingDamageText text)
    {
        SetAt(index, text);
    }

    private void SetAt(int index, FloatingDamageText text)
    {
        switch (index)
        {
            case 0: _text0 = text; break;
            case 1: _text1 = text; break;
            case 2: _text2 = text; break;
            case 3: _text3 = text; break;
            case 4: _text4 = text; break;
            case 5: _text5 = text; break;
            case 6: _text6 = text; break;
            case 7: _text7 = text; break;
        }
    }

    private void ShiftLeft()
    {
        _text0 = _text1;
        _text1 = _text2;
        _text2 = _text3;
        _text3 = _text4;
        _text4 = _text5;
        _text5 = _text6;
        _text6 = _text7;
        Count--;
    }

    public void RemoveExpired()
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            if (GetAt(i).IsExpired)
            {
                // Move todos para a esquerda
                for (int j = i; j < Count - 1; j++)
                {
                    SetAt(j, GetAt(j + 1));
                }
                Count--;
            }
        }
    }
}

#endregion

#region Tags

/// <summary>
/// Tag: entidade é o jogador local.
/// </summary>
public struct LocalCombatPlayer { }

/// <summary>
/// Tag: entidade de combate do cliente.
/// </summary>
public struct ClientCombatEntity { }

/// <summary>
/// Tag: entidade está visualmente morta.
/// </summary>
public struct VisuallyDead { }

/// <summary>
/// Tag: alvo atualmente selecionado pelo jogador.
/// </summary>
public struct SelectedTarget
{
    public int TargetEntityId;
    public long SelectionTick;
}

#endregion
