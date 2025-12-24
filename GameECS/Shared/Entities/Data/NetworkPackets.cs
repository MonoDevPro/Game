using GameECS.Shared.Entities.Components;
using MemoryPack;

namespace GameECS.Shared.Entities.Data;

/// <summary>
/// Dados de um jogador para sincronização de rede.
/// </summary>
[MemoryPackable]
public partial record struct PlayerDto(
    int PlayerId,
    int NetworkId,
    int MapId,
    string Name,
    GenderType Gender,
    VocationType Vocation,
    int X,
    int Y,
    int Z,
    byte Direction,
    int Hp,
    int MaxHp,
    int HpRegen,
    int Mp,
    int MaxMp,
    int MpRegen,
    float MovementSpeed,
    float AttackSpeed,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense
);

/// <summary>
/// Dados de um NPC para sincronização de rede.
/// </summary>
[MemoryPackable]
public partial record struct NpcData(
    int NetworkId,
    string Name,
    string TemplateId,
    byte Type,
    int X,
    int Y,
    int Z,
    byte Direction,
    int Hp,
    int MaxHp,
    int Mp,
    int MaxMp,
    float MovementSpeed,
    float AttackSpeed,
    int Level
);

/// <summary>
/// Pacote de spawn de jogadores.
/// </summary>
[MemoryPackable]
public partial record struct PlayerSpawnPacket(PlayerDto[] PlayerData);

/// <summary>
/// Pacote de spawn de NPCs.
/// </summary>
[MemoryPackable]
public partial record struct NpcSpawnPacket(NpcData[] Npcs);

/// <summary>
/// Dados de mapa para sincronização.
/// </summary>
[MemoryPackable]
public partial struct MapData
{
    public string Id;
    public string Name;
    public ushort Width;
    public ushort Height;
    public float CellSize;
    public byte[] WalkabilityData;
}

/// <summary>
/// Pacote de quando jogador entra no jogo.
/// </summary>
[MemoryPackable]
public partial struct PlayerJoinPacket
{
    public MapData MapData;
    public PlayerDto LocalPlayer;
}

/// <summary>
/// Pacote de saída de entidades.
/// </summary>
[MemoryPackable]
public partial record struct LeftPacket(int[] Ids);

/// <summary>
/// Snapshot de vitais para sincronização.
/// </summary>
[MemoryPackable]
public partial struct VitalsSnapshot
{
    public int NetworkId;
    public int CurrentHp;
    public int MaxHp;
    public int CurrentMp;
    public int MaxMp;
    public int HpRegen;
    public int MpRegen;
}

/// <summary>
/// Pacote de atualização de vitais.
/// </summary>
[MemoryPackable]
public partial record struct VitalsPacket(VitalsSnapshot[] Vitals);

/// <summary>
/// Pacote de ataque/combate.
/// </summary>
[MemoryPackable]
public partial struct AttackPacket
{
    public AttackSnapshot[] Attacks;
}

/// <summary>
/// Snapshot de um ataque individual.
/// </summary>
[MemoryPackable]
public partial struct AttackSnapshot
{
    public int AttackerId;
    public int TargetId;
    public int Damage;
    public bool IsCritical;
    public byte AttackType;
    public byte Style;
    public float AttackDuration;
    public float CooldownRemaining;
    public long Timestamp;
}
