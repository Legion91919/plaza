using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Plaza.Profiles.Contracts;

namespace Plaza.Api.Auth;

/// <summary>
/// DEVELOPMENT-ONLY stand-in for real sign-in. The caller says who they are with the
/// <c>X-Demo-User</c> header (or the <c>demoUser</c> query parameter, needed for WebSockets).
/// There is no secret, so this must never run outside Development; <see cref="AuthSetup"/> enforces that.
/// </summary>
internal sealed class DemoUserAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IProfileDirectory profiles) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DemoUser";
    public const string HeaderName = "X-Demo-User";
    public const string QueryName = "demoUser";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? userId = Request.Headers[HeaderName];
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = Request.Query[QueryName];
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var profile = profiles.Find(userId.Trim());
        if (profile is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown demo user."));
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, profile.UserId),
            new(ClaimTypes.Name, profile.DisplayName),
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
