using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using PluginRight.Core.OpenAI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Force Kestrel to always listen on port 5000
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// config
var apiKey = Environment.GetEnvironmentVariable("API_KEY")
    ?? "dev";
var openAiKey = Environment
    .GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

// services
builder.Services.AddRateLimiter(_ =>
    _.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 30;
        o.Window = TimeSpan.FromSeconds(10);
    }));

builder.Services.AddHttpClient("openai", c =>
{
    c.BaseAddress = new Uri("https://api.openai.com/");
    c.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", openAiKey);
});

builder.Services.AddScoped<IOpenAIClient, SimpleOpenAIClient>();

var app = builder.Build();
app.UseRateLimiter();

app.MapGet("/health/live",
    () => Results.Ok(new { ok = true }));

app.MapGet("/health/ready",
    () => Results.Ok(new { ok = openAiKey != "" }));

app.MapPost(
    "/v1/ai/complete",
    async (HttpRequest req, IOpenAIClient client) =>
    {
        if (!req.Headers.TryGetValue("X-Api-Key", out var k)
            || k != apiKey)
        {
            return Results.Unauthorized();
        }

        var payload = await JsonSerializer
            .DeserializeAsync<ChatRequest>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (payload is null)
        {
            return Results.BadRequest();
        }

        var res = await client.CompleteAsync(
            payload,
            req.HttpContext.RequestAborted);

        return Results.Json(res);
    });

app.Run();

// --- local client impl ---
sealed class SimpleOpenAIClient(IHttpClientFactory f) : IOpenAIClient
{
    private readonly IHttpClientFactory _f = f;

    public async Task<ChatResponse> CompleteAsync(
        ChatRequest r,
        CancellationToken ct = default)
    {
        var http = _f.CreateClient("openai");

        using var msg = new HttpRequestMessage(
            HttpMethod.Post,
            "v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new
                    {
                        model = r.Model,
                        messages = r.Messages.Select(m => new
                        {
                            role = m.Role,
                            content = m.Content
                        }),
                        stream = false
                    }),
                Encoding.UTF8,
                new MediaTypeHeaderValue("application/json"))
        };

        using var resp = await http.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();

        using var s = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(
            s,
            cancellationToken: ct);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        int? pt = doc.RootElement.TryGetProperty("usage", out var u)
            ? u.GetProperty("prompt_tokens").GetInt32()
            : (int?)null;

        int? ctoks = doc.RootElement.TryGetProperty("usage", out var u2)
            ? u2.GetProperty("completion_tokens").GetInt32()
            : (int?)null;

        return new ChatResponse(content, pt, ctoks);
    }
}
