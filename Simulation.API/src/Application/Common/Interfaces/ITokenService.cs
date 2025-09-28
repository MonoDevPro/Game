namespace GameWeb.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Issues an access token for the given user id.
    /// </summary>
    /// <param name="userId">The application user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token string (e.g., JWT).</returns>
    Task<string> IssueAccessTokenAsync(string userId, CancellationToken cancellationToken = default);
}
