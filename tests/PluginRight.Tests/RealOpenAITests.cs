using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;

[Collection("OpenAI")] // serialize external calls in this group
public class RealOpenAITests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RealOpenAITests(WebApplicationFactory<Program> factory)
    {
        var repoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")
        );
        var keyPath = Path.Combine(repoRoot, "Secrets", "openai.key");
        string? key = null;
        if (File.Exists(keyPath))
        {
            key = File.ReadAllLines(keyPath)
                .Select(l => l.Trim())
                .FirstOrDefault(l => !string.IsNullOrEmpty(l));
        }

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(
                (ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["OpenAI:ApiKey"] = key,
                            // Avoid fixed port binding when running tests
                            ["Urls"] = "http://127.0.0.1:0",
                        }
                    );
                }
            );
        });
    }

    [OpenAIRequiredFact]
    public async Task Generate_Real_OpenAI_Saves_Artifact()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev");

        var metadata = @"
entities:
  - logicalName: ""account""
    attributes:
      - name: ""name""
        type: ""string""
      - name: ""primarycontactid""
        type: ""lookup""
  - logicalName: ""contact""
    attributes:
      - name: ""emailaddress1""
        type: ""string""
      - name: ""parentcustomerid""
        type: ""customer""
";

        var body = new
        {
            metadata_yaml = metadata,
            user_prompt = @"
On Account Update (PostOperation), find all related contacts whose emailaddress1
is missing a domain part and append the domain from the account's primary
contact's email, or use “example.com” if not available.",
        };

        var requestJson = JsonSerializer.Serialize(body);

        // Prepare results directory once per test and have the API write raw artifacts
        var resultsDir = CreateResultsDir();
        client.DefaultRequestHeaders.Add("X-Artifact-Dir", resultsDir);

        using var resp = await client.PostAsync(
            "/v1/plugins/generate",
            new StringContent(requestJson, Encoding.UTF8, "application/json")
        );

        Assert.True(resp.IsSuccessStatusCode, $"HTTP {(int)resp.StatusCode}");
        var text = await resp.Content.ReadAsStringAsync();

        Assert.True(!string.IsNullOrWhiteSpace(text));
    }

    private static string CreateResultsDir()
    {
        // Resolve to tests root: tests/Results/<timestamp>
        // AppContext.BaseDirectory = tests/PluginRight.Tests/bin/Debug/netX
        var projDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
        );
        var testsRoot = Path.GetFullPath(Path.Combine(projDir, ".."));
        var root = Path.Combine(testsRoot, "Results");
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var dir = Path.Combine(root, stamp);
        Directory.CreateDirectory(dir);
        return dir;
    }
}
