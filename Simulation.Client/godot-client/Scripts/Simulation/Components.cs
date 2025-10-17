namespace GodotClient.Simulation;

/// <summary>
/// Marca uma entidade como jogador controlado localmente (com predição clientside)
/// </summary>
public struct LocalPlayer { public int PlayerId; }

/// <summary>
/// Marca uma entidade como jogador remoto (sincronizado via servidor)
/// </summary>
public struct RemotePlayer { public int PlayerId; }

/// <summary>
/// Armazena o estado de predição do jogador local
/// Usado para reconciliação quando o servidor responde
/// </summary>
public struct PredictionState
{
    public float PredictedX;
    public float PredictedY;
}

/// <summary>
/// Buffer circular para armazenar últimas posições/frames para interpolação
/// </summary>
public struct InterpolationState
{
    public float InterpX;
    public float InterpY;
}
