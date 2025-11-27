using Arch.Core;
using Game.ECS.Components;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Services;

/// <summary>
/// Sistema responsável por sincronizar mudanças de Position com o MapSpatial.
/// Usa componente PositionChanged para detectar mudanças de forma eficiente.
/// 
/// Vantagens:
/// - Só processa entidades que realmente mudaram (query otimizada)
/// - Não precisa cache/dictionary de posições anteriores
/// - Remove automaticamente o componente após processar
/// - Zero overhead para entidades estáticas
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-15
/// </summary>
public sealed class SpatialService(IMapService mapService, ILogger<SpatialService>? logger = null)
{
    
}