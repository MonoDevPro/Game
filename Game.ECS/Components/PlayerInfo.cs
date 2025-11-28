namespace Game.ECS.Components;

// ============================================
// Info - Informações básicas da entidade
// ============================================

/// <summary>
/// Combined player information component containing gender and vocation IDs.
/// </summary>
public struct PlayerInfo
{
    public byte GenderId;
    public byte VocationId;
}

/// <summary>
/// Gender identifier component.
/// </summary>
public struct GenderId { public byte Value; } // 0 = Masculino, 1 = Feminino

/// <summary>
/// Vocation/class identifier component.
/// </summary>
public struct VocationId { public byte Value; } // 0 = Guerreiro, 1 = Mago, 2 = Arqueiro, etc.
