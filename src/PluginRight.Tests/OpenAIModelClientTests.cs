using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using PluginRight.Core.Services;
using PluginRight.Core.Models;
using PluginRight.Core.Utilities;
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
        var apiKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../openai.key"
        );

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
        var jobFilePath = Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../orders/test-job1.json"
        );
        var jobJson = await File.ReadAllTextAsync(jobFilePath);
        var job = JsonSerializer.Deserialize<Job>(jobJson, JsonOptions.Default);
        if (job == null)
        {
            Assert.Fail("Failed to deserialize job JSON.");
        }

        // Act
        var result = await _client.GenerateLogicAsync(job);

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);

        // Save the response to a timestamped file
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var responseFilePath = Path.Combine(
            AppContext.BaseDirectory,
            $"../../../../../orders/test-job1-response-{timestamp}.json"
        );
        await File.WriteAllTextAsync(responseFilePath, result);

        Assert.That(
            File.Exists(responseFilePath),
            Is.True,
            "Response file was not created."
        );
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }
}