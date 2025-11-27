using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;

namespace Game.ECS.Components;

public struct NpcPatrol
{
    public Position HomePosition;
}

/// <summary>
/// Componente que armazena o caminho calculado pelo sistema de pathfinding A*.
/// Usa um buffer inline via fixed array para zero-allocation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NpcPath
{
    /// <summary>Capacidade máxima de waypoints (inline buffer)</summary>
    public const int MaxWaypoints = 32;
    
    /// <summary>Intervalo padrão de recálculo em segundos (só usado quando alvo se move)</summary>
    public const float RecalculateInterval = 1.0f;
    
    /// <summary>Distância mínima do alvo para considerar recálculo (tiles²)</summary>
    public const int TargetMovedThresholdSq = 9; // 3 tiles
    
    /// <summary>Tempo máximo que um NPC pode ficar no mesmo waypoint antes de recalcular (stuck detection)</summary>
    public const float StuckTimeout = 1.0f;
    
    /// <summary>
    /// Buffer fixo inline de waypoints (X,Y alternados: X0,Y0,X1,Y1...).
    /// Total: 32 waypoints = 64 ints = 256 bytes
    /// </summary>
    private fixed int _waypoints[MaxWaypoints * 2];
    
    /// <summary>Índice do waypoint atual sendo seguido (0 a WaypointCount-1)</summary>
    public byte CurrentIndex;
    
    /// <summary>Quantos waypoints válidos existem no buffer</summary>
    public byte WaypointCount;
    
    /// <summary>Timer para recálculo periódico do caminho</summary>
    public float RecalculateTimer;
    
    /// <summary>Flag que indica se o caminho precisa ser recalculado</summary>
    public bool NeedsRecalculation;
    
    /// <summary>Última posição conhecida do alvo (para detectar mudanças significativas)</summary>
    public Position LastTargetPosition;
    
    /// <summary>Timer de stuck detection - reseta quando muda de waypoint</summary>
    public float StuckTimer;
    
    /// <summary>
    /// Obtém o waypoint no índice especificado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Position GetWaypoint(int index)
    {
        if ((uint)index >= MaxWaypoints) return default;
        fixed (int* ptr = _waypoints)
        {
            int baseIdx = index * 2;
            return new Position(ptr[baseIdx], ptr[baseIdx + 1]);
        }
    }
    
    /// <summary>
    /// Define o waypoint no índice especificado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWaypoint(int index, Position position)
    {
        if ((uint)index >= MaxWaypoints) return;
        fixed (int* ptr = _waypoints)
        {
            int baseIdx = index * 2;
            ptr[baseIdx] = position.X;
            ptr[baseIdx + 1] = position.Y;
        }
    }
    
    /// <summary>
    /// Retorna o waypoint atual sendo seguido.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Position GetCurrentWaypoint() => GetWaypoint(CurrentIndex);
    
    /// <summary>
    /// Avança para o próximo waypoint. Retorna true se ainda há waypoints.
    /// Reseta o timer de stuck detection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AdvanceToNextWaypoint()
    {
        if (CurrentIndex + 1 >= WaypointCount)
            return false;
        
        CurrentIndex++;
        StuckTimer = 0f; // Reseta o timer ao avançar
        return true;
    }
    
    /// <summary>
    /// Verifica se há um caminho válido.
    /// </summary>
    public readonly bool HasPath => WaypointCount > 0;
    
    /// <summary>
    /// Verifica se completou o caminho.
    /// </summary>
    public readonly bool IsPathComplete => CurrentIndex >= WaypointCount;
    
    /// <summary>
    /// Verifica se o NPC está stuck (não progride no path).
    /// </summary>
    public readonly bool IsStuck => StuckTimer >= StuckTimeout;
    
    /// <summary>
    /// Limpa o caminho atual.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearPath()
    {
        WaypointCount = 0;
        CurrentIndex = 0;
        StuckTimer = 0f;
    }
    
    /// <summary>
    /// Marca para recálculo.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RequestRecalculation()
    {
        NeedsRecalculation = true;
    }
    
    /// <summary>
    /// Cria um NpcPath com valores iniciais.
    /// </summary>
    public static NpcPath CreateDefault() => new()
    {
        CurrentIndex = 0,
        WaypointCount = 0,
        RecalculateTimer = 0f,
        NeedsRecalculation = true,
        LastTargetPosition = default
    };
}
