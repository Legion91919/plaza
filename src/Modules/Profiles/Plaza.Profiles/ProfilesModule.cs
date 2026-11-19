using Microsoft.Extensions.DependencyInjection;
using Plaza.Profiles.Contracts;

namespace Plaza.Profiles;

/// <summary>The only public entry point of the Profiles module.</summary>
public static class ProfilesModule
{
    public static IServiceCollection AddProfilesModule(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryProfileStore>();
        services.AddSingleton<IProfileDirectory>(sp => sp.GetRequiredService<InMemoryProfileStore>());
        services.AddSingleton<IProfileRegistration>(sp => sp.GetRequiredService<InMemoryProfileStore>());
        return services;
    }
}
