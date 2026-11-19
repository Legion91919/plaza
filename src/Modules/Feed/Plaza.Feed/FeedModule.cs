using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Plaza.Feed.Contracts;
using Plaza.Feed.Infrastructure;

[assembly: InternalsVisibleTo("Plaza.Tests")]

namespace Plaza.Feed;

/// <summary>The only public entry point of the Feed module.</summary>
public static class FeedModule
{
    public static IServiceCollection AddFeedModule(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPostStore, InMemoryPostStore>();
        services.AddSingleton<IModerationLog, InMemoryModerationLog>();
        services.AddSingleton<FeedService>();
        services.AddSingleton<IFeedService>(sp => sp.GetRequiredService<FeedService>());
        services.AddSingleton<IFeedImport>(sp => sp.GetRequiredService<FeedService>());
        return services;
    }
}
