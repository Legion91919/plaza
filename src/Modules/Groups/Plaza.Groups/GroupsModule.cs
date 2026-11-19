using Microsoft.Extensions.DependencyInjection;
using Plaza.Groups.Contracts;

namespace Plaza.Groups;

/// <summary>The only public entry point of the Groups module.</summary>
public static class GroupsModule
{
    public static IServiceCollection AddGroupsModule(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryGroupStore>();
        services.AddSingleton<IGroupDirectory>(sp => sp.GetRequiredService<InMemoryGroupStore>());
        services.AddSingleton<IGroupAdministration>(sp => sp.GetRequiredService<InMemoryGroupStore>());
        return services;
    }
}
