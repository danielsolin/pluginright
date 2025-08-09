using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

public class StreamingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StreamingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient("openai")
                    .ConfigurePrimaryHttpMessageHandler(
                        () => new FakeSseHandler());
            });
        });
    }

    [Fact]
    public async Task Stream_Returns_SSE_Wrapped_Tokens()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev");

        var payload = new
        {
            model = "test-model",
            messages = new[] { new { role = "user", content = "hello" } },
            stream = true
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/ai/stream")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        using var resp = await client.SendAsync(
            req,
            HttpCompletionOption.ResponseHeadersRead);

        Assert.True(resp.IsSuccessStatusCode,
            $"Expected 2xx, got {(int)resp.StatusCode}");

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var sb = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: "))
            {
                sb.Append(line.Substring("data: ".Length));
            }
        }

        var text = sb.ToString();
        Assert.Contains("public ", text);
        Assert.Contains("class Foo", text);
    }

    private sealed class FakeSseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = string.Join("\n\n", new[]
            {
                "data: {\"choices\":[{\"delta\":{\"content\":\"public \"}}]}",
                "data: {\"choices\":[{\"delta\":{\"content\":\"class Foo\"}}]}",
                "data: [DONE]"
            }) + "\n\n";

            var msg = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8)
            };
            msg.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    "text/event-stream");
            return Task.FromResult(msg);
        }
    }
}
