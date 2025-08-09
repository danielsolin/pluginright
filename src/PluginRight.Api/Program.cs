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

// services
builder.Services.AddRateLimiter(_ =>
    _.AddFixedWindowLimiter(
        "api",
        o =>
        {
            o.PermitLimit = 30;
            o.Window = TimeSpan.FromSeconds(10);
        }
    )
);

builder.Services.AddHttpClient(
    "openai",
    c =>
    {
        c.BaseAddress = new Uri(openAiBase);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            openAiKey
        );
        c.Timeout = TimeSpan.FromSeconds(60);
    }
);

builder.Services.AddScoped<IOpenAIClient, SimpleOpenAIClient>();

var app = builder.Build();
app.UseRateLimiter();

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
            // Normalize output: strip Markdown fences and enforce header
            var code = res.Content ?? string.Empty;
            code = OutputNormalizer.SanitizeCSharp(code);
            code = OutputNormalizer.SoftWrapComments(code, 100);
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

// --- local client impl ---
sealed class SimpleOpenAIClient(IHttpClientFactory f) : IOpenAIClient
{
    private readonly IHttpClientFactory _f = f;

    public async Task<ChatResponse> CompleteAsync(
        ChatRequest r,
        CancellationToken ct = default
    )
    {
        var http = _f.CreateClient("openai");

        using var msg = new HttpRequestMessage(
            HttpMethod.Post,
            "v1/chat/completions"
        )
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new
                    {
                        model = r.Model,
                        messages = r.Messages.Select(m => new
                        {
                            role = m.Role,
                            content = m.Content,
                        }),
                    }
                ),
                Encoding.UTF8,
                new MediaTypeHeaderValue("application/json")
            ),
        };

        using var resp = await http.SendAsync(msg, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var bodyText = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI error {(int)resp.StatusCode}: {bodyText}",
                null,
                resp.StatusCode
            );
        }

        using var s = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

        var content =
            doc.RootElement.GetProperty("choices")[0]
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

// --- prompt helpers ---
static class PromptBuilder
{
    private static string? ReadPromptFile(string fileName)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "Prompts", fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }
        catch
        {
            // ignore and fallback
        }
        return null;
    }

    public static string BuildSystemPrompt()
    {
        var fromFile = ReadPromptFile("system_prompt.txt");
        if (string.IsNullOrWhiteSpace(fromFile))
        {
            throw new InvalidOperationException(
                "Missing system prompt: Prompts/system_prompt.txt"
            );
        }
        return fromFile.Trim();
    }

    public static string BuildUserPrompt(string metadataYaml, string userPrompt)
    {
        var tmpl = ReadPromptFile("user_prompt_template.txt");
        if (string.IsNullOrWhiteSpace(tmpl))
        {
            throw new InvalidOperationException(
                "Missing user prompt template: " +
                "Prompts/user_prompt_template.txt"
            );
        }
        return tmpl
            .Replace("{{METADATA_YAML}}", metadataYaml ?? string.Empty)
            .Replace("{{USER_PROMPT}}", userPrompt ?? string.Empty)
            .Trim();
    }
}

// Expose the entry point class for WebApplicationFactory in tests
public partial class Program { }

// --- output normalization ---
static class OutputNormalizer
{
    private static readonly Regex Fences = new(
        @"^```[a-zA-Z]*\s*|\s*```\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    public static string SanitizeCSharp(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;
        var trimmed = code.Trim();
        // Remove Markdown code fences if present
        trimmed = Fences.Replace(trimmed, string.Empty);
        return trimmed.Trim();
    }

    // Wraps comment-only lines to a maximum width without touching code semantics
    public static string SoftWrapComments(string code, int maxWidth)
    {
        if (string.IsNullOrEmpty(code)) return code;
        var sb = new StringBuilder(code.Length + 128);
        using var reader = new StringReader(code);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.TrimStart().StartsWith("//"))
            {
                WrapCommentLine(line, maxWidth, sb);
            }
            else
            {
                sb.AppendLine(line);
            }
        }
        return sb.ToString();
    }

    private static void WrapCommentLine(string line, int maxWidth, StringBuilder sb)
    {
        var leading = line.Substring(0, line.IndexOf(line.TrimStart()));
        var content = line.TrimStart();
        // Keep the initial // prefix
        var prefix = "//";
        var rest = content.StartsWith("//") ? content.Substring(2).TrimStart() : content;
        var words = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var w in words)
        {
            var candidate = current.Length == 0 ? w : current + " " + w;
            var fullLen = leading.Length + prefix.Length + 1 + candidate.Length;
            if (fullLen > maxWidth && current.Length > 0)
            {
                sb.AppendLine($"{leading}{prefix} {current}");
                current.Clear();
                current.Append(w);
            }
            else
            {
                if (current.Length == 0) current.Append(w);
                else { current.Append(' '); current.Append(w); }
            }
        }
        if (current.Length > 0)
        {
            sb.AppendLine($"{leading}{prefix} {current}");
        }
    }

    private static string Shorten(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
