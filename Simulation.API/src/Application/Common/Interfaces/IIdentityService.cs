using System.Security.Claims;

namespace GameWeb.Application.Common.Interfaces;

/// <summary>
/// Define a contract for identity-related services, such as user creation,
/// authorization, and claims management.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Gets the username for a specified user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The username, or null if the user is not found.</returns>
    Task<string?> GetUserNameAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new user with the specified details.
    /// </summary>
    /// <param name="userName">The desired username.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple containing the result of the operation and the new user's ID.</returns>
    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string email, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a user is in a specified role.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="role">The name of the role.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the user is in the role; otherwise, false.</returns>
    Task<bool> IsInRoleAsync(string userId, string role, CancellationToken cancellationToken);

    /// <summary>
    /// Programmatically checks if a user is authorized against a specific policy.
    /// </summary>
    /// <param name="userId">The user ID to authorize.</param>
    /// <param name="policyName">The name of the authorization policy.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if authorization is successful; otherwise, false.</returns>
    Task<bool> AuthorizeAsync(string userId, string policyName, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a user by their ID.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the deletion operation.</returns>
    Task<Result> DeleteUserAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the value of a specific claim for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="claimType">The type of the claim to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The value of the first claim of the specified type, or null if not found.</returns>
    Task<string?> GetClaimValueAsync(string userId, string claimType, CancellationToken cancellationToken);

    /// <summary>
    /// Adds or updates a claim for a user. If a claim with the same type exists, it's replaced.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="claimType">The type of the claim.</param>
    /// <param name="claimValue">The value of the claim.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> SetClaimAsync(string userId, string claimType, string claimValue, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a claim of a specific type from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="claimType">The type of the claim to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> RemoveClaimAsync(string userId, string claimType, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all claims for a specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of claims for the user.</returns>
    Task<IList<Claim>> GetUserClaimsAsync(string userId, CancellationToken cancellationToken);
}
