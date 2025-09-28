namespace Application.Models.Options;

public enum Authority
{
    Server,
    Client
}

public class AuthorityOptions
{
    public const string SectionName = "Authority";
    
    public Authority Authority { get; set; } = Authority.Client;
    
    public override string ToString()
    {
        return $"Authority={Authority}";
    }
}
