namespace Game.Abstractions;

public static class EnumByteExtensions
{
    /// <summary>
    /// Converte qualquer enum para byte. Pode lançar OverflowException se fora do intervalo.
    /// Uso: myEnumValue.ToByte();
    /// </summary>
    public static byte ToByte(this Enum value)
    {
        return Convert.ToByte(value);
    }

    /// <summary>
    /// Versão genérica (constrangida a Enum). Idêntica à anterior, apenas genérica.
    /// Uso: myEnumValue.ToByte();
    /// </summary>
    public static byte ToByte<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        return Convert.ToByte(value);
    }

    /// <summary>
    /// Tenta converter para byte sem lançar; retorna true se conseguiu.
    /// Uso: if (myEnumValue.TryToByte(out byte b)) { ... }
    /// </summary>
    public static bool TryToByte<TEnum>(this TEnum value, out byte result) where TEnum : struct, Enum
    {
        try
        {
            result = Convert.ToByte(value);
            return true;
        }
        catch (OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Converte para byte; se não for possível (overflow), retorna o default fornecido.
    /// Uso: var b = myEnumValue.ToByteOrDefault(0);
    /// </summary>
    public static byte ToByteOrDefault<TEnum>(this TEnum value, byte defaultValue = 0) where TEnum : struct, Enum
    {
        return value.TryToByte(out var r) ? r : defaultValue;
    }
}