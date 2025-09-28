using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using GameWeb.Application.Common.Exceptions;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Security;

namespace GameWeb.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUser _user;
    private readonly IIdentityService _identityService;
    private readonly ILogger<AuthorizationBehaviour<TRequest, TResponse>>? _logger;

    // Cache por Type do request -> metadados de autorização (roles normalizadas e policies)
    private static readonly ConcurrentDictionary<Type, AuthorizeMetadata> MetadataCache = new();

    public AuthorizationBehaviour(
        IUser user,
        IIdentityService identityService,
        ILogger<AuthorizationBehaviour<TRequest, TResponse>>? logger = null)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();

        var metadata = MetadataCache.GetOrAdd(requestType, BuildMetadataForType);

        // If no authorize requirements, continue
        if (!metadata.HasRequirements)
        {
            return await next();
        }

        // Must be authenticated
        if (_user.Id == null)
        {
            _logger?.LogWarning("Unauthorized request of type {RequestType}: user not authenticated.", requestType.FullName);
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        // Role-based authorization
        if (metadata.Roles != null && metadata.Roles.Count > 0)
        {
            var userRoles = (_user.Roles ?? Enumerable.Empty<string>())
                .Select(r => r?.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // If user has no roles -> forbidden
            if (userRoles.Count == 0)
            {
                _logger?.LogWarning("Forbidden request of type {RequestType}: user {UserId} has no roles.", requestType.FullName, _user.Id);
                throw new ForbiddenAccessException();
            }

            // check if any required role intersects with user's roles
            var hasRole = metadata.Roles.Any(requiredRole => userRoles.Contains(requiredRole));
            if (!hasRole)
            {
                _logger?.LogWarning("Forbidden request of type {RequestType}: user {UserId} missing required roles ({RequiredRoles}).",
                    requestType.FullName, _user.Id, string.Join(',', metadata.Roles));
                throw new ForbiddenAccessException();
            }
        }

        // Policy-based authorization
        if (metadata.Policies != null && metadata.Policies.Count > 0)
        {
            foreach (var policy in metadata.Policies)
            {
                var authorized = await _identityService.AuthorizeAsync(_user.Id, policy, cancellationToken).ConfigureAwait(false);
                if (!authorized)
                {
                    _logger?.LogWarning("Forbidden request of type {RequestType}: user {UserId} failed policy '{Policy}'.",
                        requestType.FullName, _user.Id, policy);
                    throw new ForbiddenAccessException();
                }
            }
        }

        // Authorized
        return await next();
    }

    private static AuthorizeMetadata BuildMetadataForType(Type requestType)
    {
        // Get attributes on the request type (including inherited)
        var attrs = requestType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();

        if (attrs.Length == 0)
        {
            return AuthorizeMetadata.None;
        }

        var rolesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var policies = new List<string>();

        foreach (var a in attrs)
        {
            if (!string.IsNullOrWhiteSpace(a.Roles))
            {
                var parts = a.Roles.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(p => p.Trim())
                                   .Where(p => !string.IsNullOrEmpty(p));
                foreach (var p in parts)
                {
                    rolesSet.Add(p);
                }
            }

            if (!string.IsNullOrWhiteSpace(a.Policy))
            {
                var policy = a.Policy.Trim();
                if (policy.Length > 0)
                    policies.Add(policy);
            }
        }

        return new AuthorizeMetadata(
            roles: rolesSet.Count > 0 ? rolesSet : null,
            policies: policies.Count > 0 ? policies : null
        );
    }

    // Small internal metadata holder to avoid repeated parsing/reflection
    private sealed class AuthorizeMetadata
    {
        public static readonly AuthorizeMetadata None = new(null, null);

        public IReadOnlyCollection<string>? Roles { get; }
        public IReadOnlyCollection<string>? Policies { get; }

        public bool HasRequirements => (Roles != null && Roles.Count > 0) || (Policies != null && Policies.Count > 0);

        public AuthorizeMetadata(IReadOnlyCollection<string>? roles, IReadOnlyCollection<string>? policies)
        {
            Roles = roles;
            Policies = policies;
        }
    }
}
