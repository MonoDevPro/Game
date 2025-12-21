namespace GameECS.Modules.Entities.Server.Persistence;

/// <summary>
/// Dados persistidos do player.
/// </summary>
public sealed class PlayerData
{
    public int AccountId { get; set; }
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int MapId { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public DateTime LastLogin { get; set; }
    public DateTime LastSave { get; set; }
    public TimeSpan TotalPlayTime { get; set; }

    // Dados extras em JSON para extensibilidade
    public string? ExtendedData { get; set; }
}

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

/// <summary>
/// Interface para persistência de players.
/// </summary>
public interface IPlayerPersistence
{
    /// <summary>
    /// Carrega dados do player.
    /// </summary>
    Task<PlayerData?> LoadPlayerAsync(int accountId, int characterId, CancellationToken ct = default);

    /// <summary>
    /// Salva dados do player.
    /// </summary>
    Task<bool> SavePlayerAsync(PlayerData data, CancellationToken ct = default);

    /// <summary>
    /// Cria novo player.
    /// </summary>
    Task<PlayerData?> CreatePlayerAsync(int accountId, string name, CancellationToken ct = default);

    /// <summary>
    /// Deleta player.
    /// </summary>
    Task<bool> DeletePlayerAsync(int accountId, int characterId, CancellationToken ct = default);

    /// <summary>
    /// Lista characters de uma conta.
    /// </summary>
    Task<IReadOnlyList<PlayerData>> GetCharactersAsync(int accountId, CancellationToken ct = default);

    /// <summary>
    /// Verifica se nome está disponível.
    /// </summary>
    Task<bool> IsNameAvailableAsync(string name, CancellationToken ct = default);
}

/// <summary>
/// Interface para persistência de pets.
/// </summary>
public interface IPetPersistence
{
    /// <summary>
    /// Carrega pets do player.
    /// </summary>
    Task<IReadOnlyList<PetData>> LoadPetsAsync(int characterId, CancellationToken ct = default);

    /// <summary>
    /// Salva pet.
    /// </summary>
    Task<bool> SavePetAsync(PetData data, CancellationToken ct = default);

    /// <summary>
    /// Cria novo pet.
    /// </summary>
    Task<PetData?> CreatePetAsync(int characterId, string templateId, string? customName = null, CancellationToken ct = default);

    /// <summary>
    /// Deleta pet.
    /// </summary>
    Task<bool> DeletePetAsync(int petId, CancellationToken ct = default);
}
