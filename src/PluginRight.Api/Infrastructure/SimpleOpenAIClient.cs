using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;
using PluginRight.Core.OpenAI;

namespace PluginRight.Api.Infrastructure;

sealed class SimpleOpenAIClient(IHttpClientFactory f, IHttpContextAccessor ctx) : IOpenAIClient
{
    private readonly IHttpClientFactory _f = f;
    private readonly IHttpContextAccessor _ctx = ctx;

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
                "application/json"
            ),
        };

        // If tests pass a destination directory via header, save the raw request JSON
        try
        {
            var dir = _ctx.HttpContext?.Request?.Headers?["X-Artifact-Dir"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir!);
                var reqJson = await msg.Content.ReadAsStringAsync(ct);
                File.WriteAllText(Path.Combine(dir!, "request.json"), reqJson, Encoding.UTF8);
            }
        }
        catch
        {
            // non-fatal diagnostics
        }

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

        // Read raw JSON response as text
        var raw = await resp.Content.ReadAsStringAsync(ct);

        // Save raw response JSON if requested
        try
        {
            var dir = _ctx.HttpContext?.Request?.Headers?["X-Artifact-Dir"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir!);
                File.WriteAllText(Path.Combine(dir!, "response.json"), raw, Encoding.UTF8);
            }
        }
        catch
        {
            // non-fatal diagnostics
        }

        using var doc = JsonDocument.Parse(raw);

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
