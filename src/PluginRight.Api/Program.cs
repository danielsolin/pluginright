using System;
using System.Linq;
using System.Net;
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

// config from appsettings
var cfg = builder.Configuration;
var urls = cfg["Urls"] ?? "http://0.0.0.0:5000";
builder.WebHost.UseUrls(urls);

// settings
var apiKey = cfg["Api:ApiKey"] ?? "dev";
var openAiKey = cfg["OpenAI:ApiKey"] ?? "";
var defaultModel = cfg["OpenAI:DefaultModel"] ?? "gpt-4o-mini";
var openAiBase = cfg["OpenAI:BaseUrl"] ?? "https://api.openai.com/";

// services
builder.Services.AddRateLimiter(_ =>
    _.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 30;
        o.Window = TimeSpan.FromSeconds(10);
    }));

builder.Services.AddHttpClient("openai", c =>
{
    c.BaseAddress = new Uri(openAiBase);
    c.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", openAiKey);
    c.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<IOpenAIClient, SimpleOpenAIClient>();

var app = builder.Build();
app.UseRateLimiter();

app.MapGet("/health/live",
    () => Results.Ok(new { ok = true }));

app.MapGet("/health/ready",
    () => Results.Ok(new { ok = !string.IsNullOrWhiteSpace(openAiKey) }));

// Non-streaming completion
app.MapPost(
    "/v1/ai/complete",
    async (ChatRequest payload,
           IOpenAIClient client,
           HttpContext ctx) =>
    {
        if (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var k)
            || k != apiKey)
        {
            return Results.Unauthorized();
        }

        var model = string.IsNullOrWhiteSpace(payload.Model)
            ? defaultModel
            : payload.Model;

        var start = DateTime.UtcNow;
        try
        {
            Log.Information(
                "AI request model={Model} messages={Count}",
                model,
                payload.Messages?.Count ?? 0);

            var res = await client.CompleteAsync(
                payload with { Model = model, Stream = false },
                ctx.RequestAborted);

            var elapsed = DateTime.UtcNow - start;
            Log.Information(
                "AI response tokens p={PT} c={CT} in {Ms}ms",
                res.PromptTokens,
                res.CompletionTokens,
                (int)elapsed.TotalMilliseconds);

            return Results.Json(res);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            var code = ex.StatusCode.Value;
            Log.Warning(
                ex,
                "OpenAI HTTP error {Status}",
                (int)code);

            return code switch
            {
                HttpStatusCode.TooManyRequests
                    => Results.StatusCode(StatusCodes.Status429TooManyRequests),
                HttpStatusCode.Unauthorized
                    => Results.StatusCode(StatusCodes.Status401Unauthorized),
                HttpStatusCode.Forbidden
                    => Results.StatusCode(StatusCodes.Status403Forbidden),
                HttpStatusCode.BadRequest
                    => Results.BadRequest(),
                _ => Results.StatusCode(StatusCodes.Status502BadGateway)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AI request failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    });

// Streaming SSE endpoint
app.MapPost(
    "/v1/ai/stream",
    async (ChatRequest payload,
           IHttpClientFactory factory,
           HttpContext ctx) =>
    {
        if (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var k)
            || k != apiKey)
        {
            return Results.Unauthorized();
        }

        var model = string.IsNullOrWhiteSpace(payload.Model)
            ? defaultModel
            : payload.Model;

        var http = factory.CreateClient("openai");
        using var msg = new HttpRequestMessage(
            HttpMethod.Post,
            "v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model,
                    messages = payload.Messages.Select(m => new
                    {
                        role = m.Role,
                        content = m.Content
                    }),
                    stream = true
                }),
                Encoding.UTF8,
                new MediaTypeHeaderValue("application/json"))
        };

        var resp = await http.SendAsync(
            msg,
            HttpCompletionOption.ResponseHeadersRead,
            ctx.RequestAborted);

        if (!resp.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)resp.StatusCode);
        }

        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";
        ctx.Response.ContentType = "text/event-stream";

        await using var stream = await resp.Content
            .ReadAsStreamAsync(ctx.RequestAborted);
        using var reader = new StreamReader(stream);

        while (!ctx.RequestAborted.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;

            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("data:"))
            {
                var data = line[5..].Trim();
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var hasDelta = doc.RootElement
                        .GetProperty("choices")[0]
                        .TryGetProperty("delta", out var delta);
                    if (!hasDelta) continue;

                    var chunk = delta.TryGetProperty("content", out var c)
                        ? c.GetString()
                        : null;

                    if (!string.IsNullOrEmpty(chunk))
                    {
                        var sse = $"data: {chunk}\n\n";
                        var bytes = Encoding.UTF8.GetBytes(sse);
                        await ctx.Response.Body.WriteAsync(
                            bytes,
                            0,
                            bytes.Length,
                            ctx.RequestAborted);
                        await ctx.Response.Body.FlushAsync(
                            ctx.RequestAborted);
                    }
                }
                catch
                {
                    // Ignore malformed chunks
                }
            }
        }

        return Results.Empty;
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

        if (!resp.IsSuccessStatusCode)
        {
            var bodyText = await resp.Content
                .ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI error {(int)resp.StatusCode}: {bodyText}",
                null,
                resp.StatusCode);
        }

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
