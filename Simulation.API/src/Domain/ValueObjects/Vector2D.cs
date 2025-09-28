namespace GameWeb.Domain.ValueObjects;

public record Vector2D(int X, int Y) : ValueObject
{
    public override string ToString()
    {
        return $"{X},{Y}";
    }
    
    public static Vector2D FromString(string value)
    {
        var parts = value.Split(',');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid vector format. Expected format: 'X,Y'");
        }

        if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
        {
            throw new ArgumentException("Invalid vector components. X and Y must be integers.");
        }

        return new Vector2D(x, y);
    }
}
