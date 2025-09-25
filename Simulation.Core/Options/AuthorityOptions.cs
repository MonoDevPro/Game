namespace Simulation.Core.Options;

public enum Authority
{
    Server,
    Client
}

public class AuthorityOptions
{
    public const string SectionName = "Authority";
    
    public Authority Authority { get; set; } = Authority.Client;
}