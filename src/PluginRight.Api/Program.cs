using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.RateLimiting;
using PluginRight.Core.OpenAI;
using PluginRight.Api.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// logging
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

// config from appsettings
var cfg = builder.Configuration;
var urls = cfg["Urls"] ?? "http://0.0.0.0:5000";
builder.WebHost.UseUrls(urls);

// settings
var apiKey = cfg["Api:ApiKey"] ?? "dev";
var openAiKey = cfg["OpenAI:ApiKey"] ?? "";
if (string.IsNullOrWhiteSpace(openAiKey))
{
    // Try repo-root Secrets/openai.key (not checked in)
    var repoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")
    );
    var keyPath = Path.Combine(repoRoot, "Secrets", "openai.key");
    if (File.Exists(keyPath))
    {
        openAiKey =
            File.ReadAllLines(keyPath)
                .Select(l => l.Trim())
                .FirstOrDefault(l => !string.IsNullOrEmpty(l)) ?? "";
    }
}
var defaultModel = cfg["OpenAI:DefaultModel"] ?? "gpt-4o-mini";
var openAiBase = cfg["OpenAI:BaseUrl"] ?? "https://api.openai.com/";
var openAiTimeoutSeconds =
    int.TryParse(cfg["OpenAI:HttpTimeoutSeconds"], out var t) && t > 0 ? t : 60;

builder.Services.AddHttpClient(
    "openai",
    c =>
    {
        c.BaseAddress = new Uri(openAiBase);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            openAiKey
        );
        c.Timeout = TimeSpan.FromSeconds(openAiTimeoutSeconds);
    }
);

builder.Services.AddScoped<IOpenAIClient, SimpleOpenAIClient>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { ok = true }));

app.MapGet(
    "/health/ready",
    () => Results.Ok(new { ok = !string.IsNullOrWhiteSpace(openAiKey) })
);

app.MapPost(
    "/v1/ai/complete",
    async (ChatRequest payload, IOpenAIClient client, HttpContext ctx) =>
    {
        if (
            !ctx.Request.Headers.TryGetValue("X-Api-Key", out var k)
            || k != apiKey
        )
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
                payload.Messages?.Count ?? 0
            );

            var res = await client.CompleteAsync(
                payload with
                {
                    Model = model,
                },
                ctx.RequestAborted
            );

            var elapsed = DateTime.UtcNow - start;
            Log.Information(
                "AI response tokens p={PT} c={CT} in {Ms}ms",
                res.PromptTokens,
                res.CompletionTokens,
                (int)elapsed.TotalMilliseconds
            );

            return Results.Json(res);
        }
        catch (TaskCanceledException ex)
        {
            Log.Warning(ex, "AI request timed out");
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            var code = ex.StatusCode.Value;
            Log.Warning(ex, "OpenAI HTTP error {Status}", (int)code);

            return code switch
            {
                HttpStatusCode.TooManyRequests => Results.StatusCode(
                    StatusCodes.Status429TooManyRequests
                ),
                HttpStatusCode.Unauthorized => Results.StatusCode(
                    StatusCodes.Status401Unauthorized
                ),
                HttpStatusCode.Forbidden => Results.StatusCode(
                    StatusCodes.Status403Forbidden
                ),
                HttpStatusCode.BadRequest => Results.BadRequest(),
                _ => Results.StatusCode(StatusCodes.Status502BadGateway),
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AI request failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
);

// Plugin generation
app.MapPost(
    "/v1/plugins/generate",
    async (IOpenAIClient client, HttpContext ctx) =>
    {
        if (
            !ctx.Request.Headers.TryGetValue("X-Api-Key", out var k)
            || k != apiKey
        )
        {
            return Results.Unauthorized();
        }

        // Expect JSON
        // { metadata_yaml: string, user_prompt: string, model?: string }
        using var doc = await JsonDocument.ParseAsync(
            ctx.Request.Body,
            cancellationToken: ctx.RequestAborted
        );

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        var metadataYaml =
            root.TryGetProperty("metadata_yaml", out var my)
            && my.ValueKind == JsonValueKind.String
                ? my.GetString() ?? string.Empty
                : string.Empty;

        var userPrompt =
            root.TryGetProperty("user_prompt", out var up)
            && up.ValueKind == JsonValueKind.String
                ? up.GetString() ?? string.Empty
                : string.Empty;

        var model =
            root.TryGetProperty("model", out var m)
            && m.ValueKind == JsonValueKind.String
                ? (m.GetString() ?? string.Empty)
                : string.Empty;

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return Results.BadRequest(
                new { error = "user_prompt is required" }
            );
        }

        var sys = PromptBuilder.BuildSystemPrompt();
        var user = PromptBuilder.BuildUserPrompt(metadataYaml, userPrompt);

        var req = new ChatRequest(
            string.IsNullOrWhiteSpace(model) ? defaultModel : model,
            new[]
            {
                new ChatMessage("system", sys),
                new ChatMessage("user", user),
            }
        );

        Log.Information(
            "Gen request model={Model} metaLen={Meta} promptLen={PL}",
            req.Model,
            metadataYaml?.Length ?? 0,
            userPrompt.Length
        );

        try
        {
            var res = await client.CompleteAsync(req, ctx.RequestAborted);
            var code = res.Content ?? string.Empty;
            return Results.Text(code, "text/plain", Encoding.UTF8);
        }
        catch (TaskCanceledException)
        {
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            return Results.StatusCode((int)ex.StatusCode!.Value);
        }
    }
);

app.Run();

// Expose the entry point class for WebApplicationFactory in tests
public partial class Program { }