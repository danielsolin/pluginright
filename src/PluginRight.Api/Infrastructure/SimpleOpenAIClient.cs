using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PluginRight.Core.OpenAI;

namespace PluginRight.Api.Infrastructure;

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
