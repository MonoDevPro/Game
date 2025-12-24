namespace GameECS.Shared.Entities.Data;

/// <summary>
/// Dados persistidos do pet.
/// </summary>
public sealed class PetData
{
    public int PetId { get; set; }
    public int OwnerCharacterId { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public string? CustomName { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public bool IsActive { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
}