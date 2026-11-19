using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Plaza.Tests.Support;

/// <summary>
/// A tiny SignalR JSON-protocol client over a raw WebSocket (the official .NET client is a NuGet
/// package; this keeps the test suite dependency-free). Enough to connect, invoke and receive.
/// </summary>
public sealed class RawSignalRClient : IAsyncDisposable
{
    private const char RecordSeparator = '\u001e';
    private readonly ClientWebSocket _socket = new();
    private readonly Channel<JsonElement> _messages = Channel.CreateUnbounded<JsonElement>();
    private readonly CancellationTokenSource _stop = new();
    private Task? _receiveLoop;
    private int _invocationId;

    public static async Task<RawSignalRClient> ConnectAsync(Uri baseAddress, string hubPath, string demoUser)
    {
        var client = new RawSignalRClient();
        using var http = new HttpClient { BaseAddress = baseAddress };
        var negotiate = await http.PostAsync(new Uri($"{hubPath}/negotiate?negotiateVersion=1&demoUser={demoUser}", UriKind.Relative), null);
        negotiate.EnsureSuccessStatusCode();
        var token = (await negotiate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("connectionToken").GetString();

        var ws = new UriBuilder(baseAddress) { Scheme = "ws", Path = hubPath, Query = $"id={token}&demoUser={demoUser}" }.Uri;
        await client._socket.ConnectAsync(ws, CancellationToken.None);
        await client.SendRawAsync("{\"protocol\":\"json\",\"version\":1}");
        client._receiveLoop = Task.Run(client.ReceiveLoopAsync);

        var handshake = await client.NextAsync(TimeSpan.FromSeconds(5));
        if (handshake is null || handshake.Value.TryGetProperty("error", out _))
        {
            throw new InvalidOperationException("SignalR handshake failed.");
        }

        return client;
    }

    /// <summary>Invokes a hub method and returns its result.</summary>
    public async Task<JsonElement> InvokeAsync(string target)
    {
        var id = Interlocked.Increment(ref _invocationId).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await SendRawAsync(JsonSerializer.Serialize(new { type = 1, invocationId = id, target, arguments = Array.Empty<object>() }));
        while (true)
        {
            var msg = await NextAsync(TimeSpan.FromSeconds(5)) ?? throw new TimeoutException($"No reply to {target}.");
            if (msg.GetProperty("type").GetInt32() == 3 && msg.GetProperty("invocationId").GetString() == id)
            {
                return msg.TryGetProperty("error", out var error)
                    ? throw new InvalidOperationException(error.GetString())
                    : msg.GetProperty("result");
            }
        }
    }

    /// <summary>Waits for the server to call <paramref name="method"/>; returns its first argument, or null on timeout.</summary>
    public async Task<JsonElement?> WaitForInvocationAsync(string method, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var msg = await NextAsync(deadline - DateTime.UtcNow);
            if (msg is { } m && m.TryGetProperty("type", out var type) && type.GetInt32() == 1 &&
                m.GetProperty("target").GetString() == method)
            {
                return m.GetProperty("arguments")[0];
            }
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
        }
        catch (WebSocketException)
        {
            // Server already gone.
        }

        if (_receiveLoop is not null)
        {
            try
            {
                await _receiveLoop;
            }
            catch (Exception ex) when (ex is OperationCanceledException or WebSocketException)
            {
                // Expected on shutdown.
            }
        }

        _socket.Dispose();
        _stop.Dispose();
    }

    private async Task<JsonElement?> NextAsync(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return null;
        }

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await _messages.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private Task SendRawAsync(string json) =>
        _socket.SendAsync(Encoding.UTF8.GetBytes(json + RecordSeparator), WebSocketMessageType.Text, true, CancellationToken.None);

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[16 * 1024];
        var pending = new StringBuilder();
        while (!_stop.IsCancellationRequested && _socket.State == WebSocketState.Open)
        {
            var result = await _socket.ReceiveAsync(buffer, _stop.Token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            pending.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            var text = pending.ToString();
            var last = text.LastIndexOf(RecordSeparator);
            if (last < 0)
            {
                continue;
            }

            foreach (var frame in text[..last].Split(RecordSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                await _messages.Writer.WriteAsync(JsonDocument.Parse(frame).RootElement.Clone());
            }

            pending.Clear().Append(text[(last + 1)..]);
        }
    }
}
