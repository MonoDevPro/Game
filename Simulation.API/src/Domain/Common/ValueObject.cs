using System.Diagnostics.CodeAnalysis;

namespace GameWeb.Domain.Common;

/// <summary>
/// Marca um Value Object do domínio.
/// Útil para reflexão, validação e convenções (ex.: registro automático em mappers).
/// </summary>
public interface IValueObject { }

/// <summary>
/// Base mínima para Value Objects usando C# 9+ record types.
/// Não reimplementa igualdade: records já fornecem igualdade por valor.
/// Use esta base apenas para comportamento/contrato compartilhado (helpers, validação, documentação).
/// </summary>
public abstract record ValueObject : IValueObject;
