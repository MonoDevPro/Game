using GameWeb.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GameWeb.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    // Relação: Um usuário pode ter muitos personagens
    public ICollection<Character> Characters { get; set; } = new List<Character>();
}
