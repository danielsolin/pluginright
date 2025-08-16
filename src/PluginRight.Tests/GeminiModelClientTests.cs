using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using PluginRight.Core.Models;
using PluginRight.Core.Services;
using PluginRight.Core.Utilities;

namespace PluginRight.Tests
{
    [TestFixture]
    public class GeminiModelClientTests
    {
        private HttpClient _httpClient;
        private GeminiModelClient _client;
        private string _apiKey;

        [SetUp]
        public async Task SetUp()
        {
            var repoRoot = PathUtilities.GetRepositoryRoot();
            var apiKeyPath = Path.Combine(repoRoot + "/keys", "gemini.key");

            if (!File.Exists(apiKeyPath))
            {
                Assert.Inconclusive("API key file not found: gemini.key.");
            }

            _apiKey = await File.ReadAllTextAsync(apiKeyPath);
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Assert.Inconclusive("API key file is empty. Skipping test.");
            }

            _httpClient = new HttpClient();
            _client = new GeminiModelClient(_httpClient, _apiKey.Trim());
        }

        [Test]
        public async Task GenerateLogicAsync_SavesResponseToFile()
        {
            // Arrange
            var repoRoot = PathUtilities.GetRepositoryRoot();
            var jobFilePath = Path.Combine(repoRoot, "orders/test-job1.json");
            var job = JobReader.ReadJobFromFile(jobFilePath);
            if (job == null)
            {
                Assert.Fail("Failed to deserialize job JSON.");
                return; // for nullable analysis
            }

            // Act
            var result = await _client.GenerateLogicAsync(job);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);

            // Save the response to a timestamped file
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var responseFilePath = Path.Combine(
                repoRoot,
                $"orders/test-job1-gemini-response-{timestamp}.json"
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
            _httpClient?.Dispose();
        }
    }
}
