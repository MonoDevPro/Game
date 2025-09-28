using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Specifications;

/// <summary>
/// Encontra um personagem que pode ser apagado pelo utilizador atual.
/// A regra permite a ação se o utilizador for o dono OU se for um Administrador.
/// </summary>
public class DeletableCharacterSpec : BaseSpecification<Character>
{
    public DeletableCharacterSpec(int characterId, IUser user)
        : base(c => c.Id == characterId && (c.OwnerId == user.Id || (user.Roles != null && user.Roles.Contains(Roles.Administrator))))
    {
    }
}
