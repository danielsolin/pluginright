using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using PluginRight.Core.Services;

namespace PluginRight.Tests;

/// <summary>
/// Unit tests for the <see cref="OpenAIModelClient"/> class.
/// </summary>
[TestFixture]
public class OpenAIModelClientTests
{
    private const string ApiKey = "test-api-key";
    private HttpClient _httpClient;
    private OpenAIModelClient _client;

    [SetUp]
    public void SetUp()
    {
        _httpClient = new HttpClient(new MockHttpMessageHandler());
        _client = new OpenAIModelClient(_httpClient, ApiKey);
    }

    [Test]
    public async Task GenerateLogicAsync_ReturnsExpectedResponse()
    {
        // Arrange
        var prompt = "Generate a simple plugin logic.";

        // Act
        var result = await _client.GenerateLogicAsync(prompt);

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Does.Contain("private")); // Example assertion
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// A mock HTTP message handler for simulating API responses.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"choices\":[{\"text\":\"private void Execute() { }\"}]}")
        };

        return Task.FromResult(response);
    }
}
