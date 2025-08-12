#nullable enable

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PluginRight.Core.Interfaces;
using PluginRight.Core.Models;

namespace PluginRight.Core.Services;

/// <summary>
/// An implementation of <see cref="IModelClient"/> that uses OpenAI's API to
/// generate plugin logic.
/// </summary>
public sealed class OpenAIModelClient : IModelClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIModelClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API requests.</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    public OpenAIModelClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateLogicAsync(Job job)
    {
        var requestBody = new
        {
            model = "gpt-4.1",
            messages = new[]
            {
                new { role = "system", content = job.System },
                new { role = "user", content = job.User }
            },
            temperature = 0.1
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            requestContent
        );
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        return jsonResponse.GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString() ?? string.Empty;
    }
}

#nullable disable
