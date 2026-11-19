using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Plaza.Notifications.Contracts;

namespace Plaza.Notifications;

/// <summary>The only public entry point of the Notifications module.</summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<ILiveNotifier, SignalRLiveNotifier>();
        return services;
    }

    public static IEndpointConventionBuilder MapNotificationsHub(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHub<NotificationsHub>(NotificationChannels.HubPath);
}
