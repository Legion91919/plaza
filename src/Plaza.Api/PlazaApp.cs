using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Plaza.Api.Auth;
using Plaza.Api.Endpoints;
using Plaza.Api.Health;
using Plaza.Api.Seeding;
using Plaza.Events;
using Plaza.Feed;
using Plaza.Groups;
using Plaza.Notifications;
using Plaza.Profiles;

namespace Plaza.Api;

/// <summary>Composition root. Tests call <see cref="Build"/> to run the real app in-process.</summary>
public static class PlazaApp
{
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        services.AddSingleton(TimeProvider.System);
        services
            .AddProfilesModule()
            .AddGroupsModule()
            .AddFeedModule()
            .AddEventsModule()
            .AddNotificationsModule();

        var enumConverter = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(enumConverter));
        services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(enumConverter));

        services.AddPlazaAuth(builder.Configuration, builder.Environment);
        services.AddProblemDetails();
        services.AddHealthChecks().AddCheck<ModulesReadyCheck>("modules", tags: ["ready"]);

        configure?.Invoke(builder);

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
        app.MapPlazaApi(AuthSetup.IsDemo(app.Configuration));
        app.MapNotificationsHub();

        if (app.Environment.IsDevelopment())
        {
            DemoSeeder.Seed(app.Services);
        }

        return app;
    }
}
