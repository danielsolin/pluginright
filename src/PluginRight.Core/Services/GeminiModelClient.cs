using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PluginRight.Core.Interfaces;
using PluginRight.Core.Models;

namespace PluginRight.Core.Services;

/// <summary>
/// An implementation of <see cref="IModelClient"/> that uses Google's Gemini API
/// to generate plugin logic.
/// </summary>
public sealed class GeminiModelClient : IModelClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string Model = "gemini-1.5-flash-latest";

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiModelClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API requests.</param>
    /// <param name="apiKey">The Google AI API key.</param>
    public GeminiModelClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateLogicAsync(Job job)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = $"{job.System}\n{job.User}" } } }
            },
            generationConfig = new
            {
                temperature = 0.1
            }
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(url, requestContent);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        return jsonResponse.GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString() ?? string.Empty;
    }
}
