namespace Game.Domain.Commons;

/// <summary>
/// Pattern Result para operações que podem falhar.
/// Fornece uma maneira type-safe de retornar sucesso ou falha com mensagens.
/// </summary>
/// <typeparam name="T">Tipo do valor de retorno em caso de sucesso</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Success result cannot have an error");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Failure result must have an error message");

        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Cria um resultado de sucesso.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Cria um resultado de falha.
    /// </summary>
    public static Result<T> Failure(string error) => new(false, default, error);
}

/// <summary>
/// Result sem valor de retorno (apenas sucesso/falha).
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Failure result must have an error message");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Cria um resultado de sucesso.
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Cria um resultado de falha.
    /// </summary>
    public static Result Failure(string error) => new(false, error);
}
