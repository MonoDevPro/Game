using MemoryPack;

namespace Game.DTOs.Map;

/// <summary>
/// Informações básicas do mapa.  Enviado uma vez quando o jogador entra no mapa.
/// </summary>
[MemoryPackable]
public sealed partial class MapMetadataDto
{
    /// <summary>ID único do mapa. </summary>
    public int MapId { get; init; }
    
    /// <summary>Nome do mapa para exibição.</summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>Largura em tiles.</summary>
    public ushort Width { get; init; }
    
    /// <summary>Altura em tiles.</summary>
    public ushort Height { get; init; }
    
    /// <summary>Número de camadas Z.</summary>
    public byte Layers { get; init; }
    
    /// <summary>Flags globais do mapa.</summary>
    public byte Flags { get; init; }
    
    /// <summary>Música de fundo (ID ou nome do arquivo).</summary>
    public string? BgmId { get; init; }
    
    /// <summary>Posição de spawn padrão. </summary>
    public int DefaultSpawnX { get; init; }
    public int DefaultSpawnY { get; init; }
    public int DefaultSpawnZ { get; init; }
}