using Microsoft.Extensions.DependencyInjection;
using Plaza.Events.Contracts;

namespace Plaza.Events;

/// <summary>The only public entry point of the Events module.</summary>
public static class EventsModule
{
    public static IServiceCollection AddEventsModule(this IServiceCollection services)
    {
        services.AddSingleton<IEventCalendar, StubEventCalendar>();
        return services;
    }
}
