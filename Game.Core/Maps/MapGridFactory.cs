using Game.Domain.Entities;
using Game.ECS.Services;

namespace Game.Core.Maps;

/// <summary>
/// Configuração para a factory de MapGrid.
/// </summary>
public sealed class MapGridFactoryOptions
{
    /// <summary>
    /// Tamanho mínimo (width * height) para usar Morton optimization.
    /// Default: 10,000 tiles (ex: 100x100)
    /// </summary>
    public int MortonThreshold { get; set; } = 10_000;

    /// <summary>
    /// Se true, sempre usa Morton independente do tamanho.
    /// </summary>
    public bool ForceMorton { get; set; } = false;

    /// <summary>
    /// Se true, nunca usa Morton (sempre MapGrid simples).
    /// </summary>
    public bool DisableMorton { get; set; } = false;

    /// <summary>
    /// Threshold de memória estimada (em MB) para considerar Morton.
    /// Se o mapa for muito pequeno mas tiver muitas layers, pode compensar usar Morton.
    /// Default: 1 MB
    /// </summary>
    public double MemoryThresholdMb { get; set; } = 1.0;

    /// <summary>
    /// Fator de peso para queries de área vs queries pontuais.
    /// Valores maiores favorecem Morton (melhor para range queries).
    /// Range: 0.0 (só queries pontuais) a 1.0 (muitas range queries)
    /// Default: 0.5 (balanceado)
    /// </summary>
    public double RangeQueryWeight { get; set; } = 0.5;

    public static MapGridFactoryOptions Default => new();

    /// <summary>
    /// Preset otimizado para servidor (muitas queries de área, pathfinding).
    /// </summary>
    public static MapGridFactoryOptions Server => new()
    {
        MortonThreshold = 5_000,  // Threshold mais baixo
        RangeQueryWeight = 0.8,    // Favorece range queries
        MemoryThresholdMb = 0.5
    };

    /// <summary>
    /// Preset otimizado para cliente (rendering, menos pathfinding).
    /// </summary>
    public static MapGridFactoryOptions Client => new()
    {
        MortonThreshold = 15_000,  // Threshold mais alto
        RangeQueryWeight = 0.3,     // Menos range queries
        MemoryThresholdMb = 2.0
    };

    /// <summary>
    /// Preset para desenvolvimento/debug (sempre simples para facilitar debug).
    /// </summary>
    public static MapGridFactoryOptions Development => new()
    {
        DisableMorton = true
    };
}

/// <summary>
/// Informações sobre a decisão da factory.
/// </summary>
public sealed class MapGridCreationInfo
{
    public required string ImplementationType { get; init; }
    public required int MapWidth { get; init; }
    public required int MapHeight { get; init; }
    public required int MapLayers { get; init; }
    public required int TotalTiles { get; init; }
    public required double EstimatedMemoryMB { get; init; }
    public required string Reason { get; init; }
    public required TimeSpan CreationTime { get; init; }

    public override string ToString()
    {
        return $"{ImplementationType} | {MapWidth}x{MapHeight}x{MapLayers} ({TotalTiles:N0} tiles) | " +
               $"{EstimatedMemoryMB:F2} MB | {CreationTime.TotalMilliseconds:F2}ms | {Reason}";
    }
}

/// <summary>
/// Factory para criar instâncias de IMapGrid otimizadas baseadas no tamanho e uso do mapa.
/// </summary>
public static class MapGridFactory
{
    private static MapGridFactoryOptions _defaultOptions = MapGridFactoryOptions.Default;

    /// <summary>
    /// Define as opções padrão para a factory.
    /// </summary>
    public static void SetDefaultOptions(MapGridFactoryOptions options)
    {
        _defaultOptions = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Cria uma instância de IMapGrid usando as opções padrão.
    /// </summary>
    public static IMapGrid Create(Map map)
    {
        return Create(map, _defaultOptions, out _);
    }

    /// <summary>
    /// Cria uma instância de IMapGrid e retorna informações sobre a criação.
    /// </summary>
    public static IMapGrid Create(Map map, out MapGridCreationInfo info)
    {
        return Create(map, _defaultOptions, out info);
    }

    /// <summary>
    /// Cria uma instância de IMapGrid com opções customizadas.
    /// </summary>
    public static IMapGrid Create(Map map, MapGridFactoryOptions options)
    {
        return Create(map, options, out _);
    }

    /// <summary>
    /// Cria uma instância de IMapGrid com opções customizadas e retorna informações sobre a criação.
    /// </summary>
    public static IMapGrid Create(Map map, MapGridFactoryOptions options, out MapGridCreationInfo info)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var startTime = DateTime.UtcNow;
        
        var decision = ShouldUseMorton(map, options);
        IMapGrid grid;

        if (decision.useMorton)
        {
            grid = new MapGridMorton(map);
        }
        else
        {
            grid = new MapGrid(map);
        }

        var creationTime = DateTime.UtcNow - startTime;

        info = new MapGridCreationInfo
        {
            ImplementationType = decision.useMorton ? "MapGridMorton" : "MapGrid",
            MapWidth = map.Width,
            MapHeight = map.Height,
            MapLayers = map.Layers,
            TotalTiles = map.Count,
            EstimatedMemoryMB = decision.estimatedMemoryMB,
            Reason = decision.reason,
            CreationTime = creationTime
        };

        return grid;
    }

    /// <summary>
    /// Analisa um mapa e retorna recomendações sem criar a instância.
    /// </summary>
    public static MapGridCreationInfo Analyze(Map map, MapGridFactoryOptions? options = null)
    {
        options ??= _defaultOptions;
        
        var startTime = DateTime.UtcNow;
        var decision = ShouldUseMorton(map, options);
        var analysisTime = DateTime.UtcNow - startTime;

        return new MapGridCreationInfo
        {
            ImplementationType = decision.useMorton ? "MapGridMorton (Recommended)" : "MapGrid (Recommended)",
            MapWidth = map.Width,
            MapHeight = map.Height,
            MapLayers = map.Layers,
            TotalTiles = map.Count,
            EstimatedMemoryMB = decision.estimatedMemoryMB,
            Reason = decision.reason,
            CreationTime = analysisTime
        };
    }

    private static (bool useMorton, double estimatedMemoryMB, string reason) ShouldUseMorton(
        Map map, 
        MapGridFactoryOptions options)
    {
        // Forçar decisão se configurado
        if (options.ForceMorton)
            return (true, EstimateMemory(map, true), "Forced by options.ForceMorton");
        
        if (options.DisableMorton)
            return (false, EstimateMemory(map, false), "Disabled by options.DisableMorton");

        int tileCount = map.Width * map.Height;
        int totalTiles = map.Count;

        // Critério 1: Tamanho do mapa (área 2D)
        if (tileCount < options.MortonThreshold)
        {
            return (false, EstimateMemory(map, false), 
                $"Map size {tileCount} below threshold {options.MortonThreshold}");
        }

        // Critério 2: Memória estimada
        double memorySimple = EstimateMemory(map, false);
        double memoryMorton = EstimateMemory(map, true);
        
        if (memoryMorton < options.MemoryThresholdMb)
        {
            // Mapa muito pequeno em memória, não vale a pena a complexidade
            return (false, memorySimple, 
                $"Estimated memory {memoryMorton:F2}MB below threshold {options.MemoryThresholdMb}MB");
        }

        // Critério 3: Densidade de layers
        // Mapas com muitas layers se beneficiam mais do cache de colisão
        double layerDensity = map.Layers / Math.Max(1.0, Math.Sqrt(tileCount));
        if (layerDensity > 0.1) // Muitas layers relativo ao tamanho
        {
            return (true, memoryMorton, 
                $"High layer density {layerDensity:F2} benefits from Morton optimization");
        }

        // Critério 4: Peso de range queries
        // Se o uso esperado tem muitas range queries, Morton é vantajoso
        if (options.RangeQueryWeight >= 0.6)
        {
            return (true, memoryMorton, 
                $"High range query weight {options.RangeQueryWeight:F2} favors Morton");
        }

        // Critério 5: Mapas grandes (acima do threshold) sempre se beneficiam
        if (tileCount >= options.MortonThreshold * 2)
        {
            return (true, memoryMorton, 
                $"Large map {tileCount} tiles (2x threshold) benefits from cache locality");
        }

        // Critério 6: Mapas quadrados ou próximos se beneficiam mais
        double aspectRatio = (double)Math.Max(map.Width, map.Height) / Math.Min(map.Width, map.Height);
        if (aspectRatio <= 2.0 && tileCount >= options.MortonThreshold)
        {
            return (true, memoryMorton, 
                $"Balanced aspect ratio {aspectRatio:F2} with {tileCount} tiles favors Morton");
        }

        // Default: usar simples se não atendeu nenhum critério forte
        return (false, memorySimple, 
            $"No strong criteria met for Morton optimization (size={tileCount}, aspect={aspectRatio:F2})");
    }

    private static double EstimateMemory(Map map, bool morton)
    {
        // Tamanho base do Map (Tile[] array)
        const int tileSize = 2; // TileType (1 byte) + CollisionMask (1 byte)
        double baseMemory = map.Count * tileSize;

        if (morton)
        {
            // MapGridMorton adiciona:
            // - int[] _posToRank: width * height * 4 bytes
            // - (int,int)[] _rankToPos: width * height * 8 bytes
            // - byte[][] _collisionByLayer: layers * (width * height * 1 byte)
            
            int area = map.Width * map.Height;
            double mappingMemory = area * (4 + 8); // posToRank + rankToPos
            double collisionCache = area * map.Layers * 1; // collision cache per layer
            
            return (baseMemory + mappingMemory + collisionCache) / (1024.0 * 1024.0);
        }
        else
        {
            // MapGrid simples apenas mantém referência ao Map
            return baseMemory / (1024.0 * 1024.0);
        }
    }

    /// <summary>
    /// Helper para comparar performance estimada entre implementações.
    /// </summary>
    public static string CompareImplementations(Map map, MapGridFactoryOptions? options = null)
    {
        options ??= _defaultOptions;

        var simple = EstimateMemory(map, false);
        var morton = EstimateMemory(map, true);
        var decision = ShouldUseMorton(map, options);

        return $"""
            Map Analysis: {map.Name} ({map.Width}x{map.Height}x{map.Layers})
            ═══════════════════════════════════════════════════════
            Total Tiles: {map.Count:N0}
            
            MapGrid (Simple):
              - Memory: {simple:F2} MB
              - Initialization: ~instant
              - Best for: Small maps, frequent updates
              
            MapGridMorton:
              - Memory: {morton:F2} MB ({(morton/simple):F2}x overhead)
              - Initialization: ~{EstimateInitTime(map):F0}ms
              - Best for: Large maps, range queries, pathfinding
            
            Recommendation: {(decision.useMorton ? "MapGridMorton" : "MapGrid")}
            Reason: {decision.reason}
            """;
    }

    private static double EstimateInitTime(Map map)
    {
        // Estimativa grosseira baseada no número de tiles
        // ~0.001ms por tile para build Morton mapping + collision cache
        return map.Count * 0.001;
    }
}