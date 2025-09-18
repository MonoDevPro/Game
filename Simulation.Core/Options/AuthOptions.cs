namespace Simulation.Core.Options;

public class AuthOptions
{
    public static string SectionName = "Auth";

    /// <summary>
    /// Intervalo de limpeza de sessões expiradas em minutos.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 5;
    
    /// <summary>
    /// Tempo de vida padrão para uma sessão em minutos.
    /// </summary>
    /// <returns></returns>
    public int DefaultSessionLifetimeMinutes { get; set; } = 60;

    public override string ToString()
    {
        return $"CleanupIntervalMinutes={CleanupIntervalMinutes}" +
               $", DefaultSessionLifetimeMinutes={DefaultSessionLifetimeMinutes}";
    }
}