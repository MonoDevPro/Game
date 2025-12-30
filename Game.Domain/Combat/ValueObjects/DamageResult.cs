using System.Runtime.InteropServices;
using Game.Domain.Commons.Enums;

namespace Game.Domain.Combat.ValueObjects;

/// <summary>
/// Resultado de um cálculo de dano.
/// Component ECS para armazenar o resultado de ataques.
/// Este ValueObject é otimizado para uso como component no ArchECS.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DamageResult(int Damage, DamageType Type, bool IsCritical);
