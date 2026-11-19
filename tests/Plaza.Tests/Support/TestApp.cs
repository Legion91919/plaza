using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plaza.Api;

namespace Plaza.Tests.Support;

/// <summary>Runs the real Plaza API on a random local port with seeded demo data and a manual clock.</summary>
public sealed class TestApp : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly List<HttpClient> _clients = [];

    private TestApp(WebApplication app, ManualTimeProvider clock, Uri baseAddress)
    {
        _app = app;
        Clock = clock;
        BaseAddress = baseAddress;
    }

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web);

    public ManualTimeProvider Clock { get; }

    public Uri BaseAddress { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<TestApp> StartAsync(Action<IServiceCollection>? services = null)
    {
        var clock = new ManualTimeProvider(ManualTimeProvider.DefaultStart);
        var app = PlazaApp.Build(["--environment", "Development", "--Auth:Mode", "Demo"], builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton<TimeProvider>(clock);
            services?.Invoke(builder.Services);
        });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new TestApp(app, clock, new Uri(address));
    }

    /// <summary>An HTTP client acting as the given demo user (or anonymous when null).</summary>
    public HttpClient ClientFor(string? userId)
    {
        var client = new HttpClient { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(10) };
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Demo-User", userId);
        }

        _clients.Add(client);
        return client;
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Json))!;

    public async ValueTask DisposeAsync()
    {
        foreach (var c in _clients)
        {
            c.Dispose();
        }

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
