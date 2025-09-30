namespace GameWeb.Domain.Entities;

public class Map : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Tiles { get; set; } = [];
    public byte[] Collision { get; set; } = [];
    public bool UsePadded { get; set; }
    public bool BorderBlocked { get; set; }
}
