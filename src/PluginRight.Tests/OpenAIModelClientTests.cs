using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using PluginRight.Core.Services;
using System.Text.Json;

namespace PluginRight.Tests;

/// <summary>
/// Unit tests for the <see cref="OpenAIModelClient"/> class.
/// </summary>
[TestFixture]
public class OpenAIModelClientTests
{
    private HttpClient _httpClient;
    private OpenAIModelClient _client;

    [SetUp]
    public async Task SetUp()
    {
        var apiKeyPath = Path.Combine(AppContext.BaseDirectory, "../../../../../openai.key");
        if (!File.Exists(apiKeyPath))
        {
            Assert.Fail("API key file not found: openai.key");
        }

        var apiKey = await File.ReadAllTextAsync(apiKeyPath);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Fail("API key file is empty.");
        }

        _httpClient = new HttpClient();
        _client = new OpenAIModelClient(_httpClient, apiKey.Trim());
    }

    [Test]
    public async Task GenerateLogicAsync_SavesResponseToFile()
    {
        // Arrange
        var jobFilePath = Path.Combine(AppContext.BaseDirectory, "../../../../../orders/test-job1.json");
        var jobJson = await File.ReadAllTextAsync(jobFilePath);
        var job = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jobJson);
        var prompt = job.GetProperty("prompt").GetString();

        if (string.IsNullOrEmpty(prompt))
        {
            Assert.Fail("Prompt is missing or empty in the job file.");
        }

        // Act
        var result = await _client.GenerateLogicAsync(prompt);

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);

        // Save the response to a timestamped file
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var responseFilePath = Path.Combine(AppContext.BaseDirectory, $"../../../../../orders/test-job1-response-{timestamp}.json");
        await File.WriteAllTextAsync(responseFilePath, result);

        Assert.That(File.Exists(responseFilePath), Is.True, "Response file was not created.");
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }
}