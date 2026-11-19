using System.Security.Claims;

namespace Plaza.Api.Auth;

/// <summary>
/// Who is calling. Endpoints depend on this, never on a specific auth scheme, so swapping the
/// demo header for OIDC (planned slice) only changes <see cref="AuthSetup"/>.
/// </summary>
public interface ICallerIdentity
{
    /// <summary>The authenticated user's id, or null for anonymous calls.</summary>
    string? UserId { get; }
}

internal sealed class ClaimsCallerIdentity(IHttpContextAccessor accessor) : ICallerIdentity
{
    public string? UserId => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
