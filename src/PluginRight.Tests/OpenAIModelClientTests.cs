using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using PluginRight.Core.Services;
using PluginRight.Core.Models;
using PluginRight.Core.Utilities;

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
        string apiKey;
        try
        {
            apiKey = await ApiKeyReader.ReadApiKeyAsync("openai.key");
        }
        catch (InvalidOperationException ex)
        {
            Assert.Inconclusive(ex.Message);
            return;
        }

        _httpClient = new HttpClient();
        _client = new OpenAIModelClient(_httpClient, apiKey);
    }

    [Test]
    public async Task GenerateLogicAsync_SavesResponseToFile()
    {
        // Arrange
        var job = JobReader.ReadJobFromFile("test-job1.json");
        if (job == null)
        {
            Assert.Fail("Failed to deserialize job JSON.");
            return; // for nullable analysis
        }

        // Act
        var generatedLogic = await _client.GenerateLogicAsync(job);

        // Assert
        Assert.That(generatedLogic, Is.Not.Null.And.Not.Empty);

        // Save the generated plugin code using the new utility
        var repoRoot = PathUtilities.GetRepositoryRoot(); // repoRoot is still needed here
        var generatedFilePath = await PluginCodeGenerator.GenerateAndSavePluginCodeAsync(
            generatedLogic,
            job,
            repoRoot
        );

        Assert.That(
            File.Exists(generatedFilePath),
            Is.True,
            "Generated plugin file was not created."
        );
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }
}
