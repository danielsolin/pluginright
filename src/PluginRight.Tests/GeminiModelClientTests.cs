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
            try
            {
                _apiKey = await ApiKeyReader.ReadApiKeyAsync("gemini.key");
            }
            catch (InvalidOperationException ex)
            {
                Assert.Inconclusive(ex.Message);
                return;
            }

            _httpClient = new HttpClient();
            _client = new GeminiModelClient(_httpClient, _apiKey);
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
            _httpClient?.Dispose();
        }
    }
}
