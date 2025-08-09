using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

[Collection("OpenAI")] // serialize external calls in this group
public class RealOpenAITests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RealOpenAITests(WebApplicationFactory<Program> factory)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
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
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["OpenAI:ApiKey"] = key,
                        // Avoid fixed port binding when running tests
                        ["Urls"] = "http://127.0.0.1:0"
                    });
                });
            });
    }

    [OpenAIRequiredFact]
    public async Task Generate_Real_OpenAI_Saves_Artifact()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev");

        var metadata = @"entities:\n  - logicalName: account\n    attributes:\n      - name: name\n        type: string\n      - name: primarycontactid\n        type: lookup\n  - logicalName: contact\n    attributes:\n      - name: emailaddress1\n        type: string\n      - name: parentcustomerid\n        type: customer";

        var body = new
        {
            metadata_yaml = metadata,
            user_prompt = "On Account Update, if name changes, copy to all child Contacts \n" +
                          "and append domain from emailaddress1 when missing. PostOperation.",
        };

        using var resp = await client.PostAsync(
            "/v1/plugins/generate",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        Assert.True(resp.IsSuccessStatusCode, $"HTTP {(int)resp.StatusCode}");
        var text = await resp.Content.ReadAsStringAsync();

        SaveArtifact("generate", text);

        Assert.Contains("IPlugin", text);
        Assert.Contains("Execute(IServiceProvider", text);
        Assert.DoesNotContain("```", text);
    }

    [OpenAIRequiredFact]
    public async Task Stream_Real_OpenAI_Saves_Artifact()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev");

        var payload = new
        {
            model = (string?)null,
            messages = new[]
            {
                new { role = "system", content = "Output only raw C# code." },
                new { role = "user", content = "Create an empty C# class Foo in namespace Company.Test" }
            },
            stream = true
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/ai/stream")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        Assert.True(resp.IsSuccessStatusCode, $"HTTP {(int)resp.StatusCode}");

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var sb = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: ")) sb.Append(line.Substring(6));
        }

        var text = sb.ToString();
        SaveArtifact("stream", text);

        Assert.Contains("namespace Company.Test", text);
        Assert.Contains("class Foo", text);
    }

    private static void SaveArtifact(string kind, string content)
    {
        // Resolve to the test project directory:
        // bin/Debug/netX -> back to project folder
        var projDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var root = Path.Combine(projDir, "Results");
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var dir = Path.Combine(root, stamp);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, kind + ".cs");
        File.WriteAllText(path, content, Encoding.UTF8);
    }
}
