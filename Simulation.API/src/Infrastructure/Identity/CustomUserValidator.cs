using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace GameWeb.Infrastructure.Identity
{
    /// <summary>
    /// Validador customizado para UserName e Email que aplica as regras de negócio
    /// descritas: obrigatoriedade, tamanho, regex e unicidade.
    /// </summary>
    public class CustomUserNameValidator : IUserValidator<ApplicationUser>
    {
        private static readonly Regex UserNameRegex = new Regex("^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var errors = new List<IdentityError>();
            
            // USERNAME: required, length, pattern, unique
            var userName = user.UserName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userName))
                errors.Add(new IdentityError { Code = "UsernameRequired", Description = "Username is required." });
            else
            {
                if (userName.Length < 3)
                    errors.Add(new IdentityError { Code = "UsernameTooShort", Description = "Username must be at least 3 characters long." });

                if (userName.Length > 30)
                    errors.Add(new IdentityError { Code = "UsernameTooLong", Description = "Username must not exceed 30 characters." });

                if (!UserNameRegex.IsMatch(userName))
                    errors.Add(new IdentityError { Code = "UsernameInvalid", Description = "Username must start with a letter and can only contain letters, numbers, and underscores." });

                // unicidade (comparação por normalized name é feita pelo UserManager internamente em FindByNameAsync)
                var existing = await manager.FindByNameAsync(userName);
                if (existing != null && !string.Equals(existing.Id, user.Id, StringComparison.OrdinalIgnoreCase))
                    errors.Add(new IdentityError { Code = "UsernameTaken", Description = "The specified username is already in use." });
            }

            return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
        }
    }
}
