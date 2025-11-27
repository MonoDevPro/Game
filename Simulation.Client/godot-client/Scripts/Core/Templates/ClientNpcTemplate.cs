namespace GodotClient.Core.Templates;

/// <summary>
/// Versão otimizada para o Cliente.
/// Remove lógica de AI e Math (Server-side) e foca em Assets e Dados de UI.
/// </summary>
public class ClientNpcTemplate
{
    // Identidade (Deve bater com o Server)
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    
    // Dados para UI (Barras de Vida/Mana)
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    
    // Dados para Animação
    // Usado para sincronizar a velocidade da animação de walk com a velocidade real
    public float MovementSpeed { get; set; } 
    public float AttackSpeed { get; set; } // Usado para acelerar animação de ataque

    // Configurações Visuais (Exclusivo do Client)
    public NpcVisualConfig Visuals { get; set; } = new();
    
    // Configurações de Áudio (Exclusivo do Client)
    public NpcAudioConfig Audio { get; set; } = new();
}

public class NpcVisualConfig
{
    // Referência para o Atlas ou Prefab (ex: "orc_warrior_red")
    public string AssetKey { get; set; } = string.Empty;
    
    // Ajustes finos de renderização
    public float Scale { get; set; } = 1.0f;
    public float YOffset { get; set; } = 0.0f;
    
    // Efeitos visuais específicos (ex: Partículas ao andar)
    public bool HasShadow { get; set; } = true;
}

public class NpcAudioConfig
{
    public string? HitSound { get; set; }
    public string? AttackSound { get; set; }
    public string? DieSound { get; set; }
    public string? IdleSound { get; set; }
}