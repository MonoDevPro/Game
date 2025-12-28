namespace Game.Domain.Player.ValueObjects;

/// <summary>
/// Identificador de rede para sincronização cliente-servidor.
/// </summary>
public struct NetworkId(int value)
{
    public int Value = value;
    public static implicit operator int(NetworkId id) => id.Value;
    public static implicit operator NetworkId(int value) => new(value);
}