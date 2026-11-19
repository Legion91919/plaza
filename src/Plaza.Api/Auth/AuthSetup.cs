using Microsoft.AspNetCore.Authentication;

namespace Plaza.Api.Auth;

/// <summary>Chooses the authentication scheme. The only file that changes when OIDC arrives.</summary>
public static class AuthSetup
{
    public const string DemoMode = "Demo";

    public static bool IsDemo(IConfiguration configuration) =>
        string.Equals(configuration["Auth:Mode"], DemoMode, StringComparison.OrdinalIgnoreCase);

    public static IServiceCollection AddPlazaAuth(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (!IsDemo(configuration))
        {
            throw new InvalidOperationException(
                "No production sign-in is configured yet. Set Auth:Mode=Demo in Development; OIDC arrives in a later slice.");
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Auth:Mode=Demo has no secret and is only allowed in the Development environment.");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ICallerIdentity, ClaimsCallerIdentity>();
        services
            .AddAuthentication(DemoUserAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DemoUserAuthenticationHandler>(DemoUserAuthenticationHandler.SchemeName, null);
        services.AddAuthorization();
        return services;
    }
}
