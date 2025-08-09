using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

[Collection("OpenAI")] // serialize external calls in this group
public class RealOpenAITests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RealOpenAITests(WebApplicationFactory<Program> factory)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
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

        var metadata =
            @"entities:\n  - logicalName: account\n    attributes:\n      - name: name\n" +
            @"        type: string\n      - name: primarycontactid\n        type: lookup\n  - logicalName: contact\n" +
            @"    attributes:\n      - name: emailaddress1\n        type: string\n      - name: parentcustomerid\n        type: customer";

        var body = new
        {
            metadata_yaml = metadata,
            user_prompt = "On Account Update, if name changes, copy to all child Contacts \n" +
                          "and append domain from emailaddress1 when missing. PostOperation.",
        };

        using var resp = await client.PostAsync(
            "/v1/plugins/generate",
            new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"));

        Assert.True(resp.IsSuccessStatusCode, $"HTTP {(int)resp.StatusCode}");
        var text = await resp.Content.ReadAsStringAsync();
        var pretty = TryFormatCSharp(text);

        SaveArtifact("generate", pretty);

        Assert.Contains("IPlugin", text);
        Assert.Contains("Execute(IServiceProvider", text);
        Assert.DoesNotContain("```", text);
    }

    private static void SaveArtifact(string kind, string content)
    {
        // Resolve to tests root: tests/Results/<timestamp>
        // AppContext.BaseDirectory = tests/PluginRight.Tests/bin/Debug/netX
        var projDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var testsRoot = Path.GetFullPath(Path.Combine(projDir, ".."));
        var root = Path.Combine(testsRoot, "Results");
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var dir = Path.Combine(root, stamp);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, kind + ".cs");
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private static string TryFormatCSharp(string code)
    {
        try
        {
            // Roslyn-based whitespace normalization
            var tree = CSharpSyntaxTree.ParseText(
                code,
                new CSharpParseOptions(
                    languageVersion: LanguageVersion.CSharp8));
            var root = tree.GetRoot();
            var normalized = root.NormalizeWhitespace();
            return normalized.ToFullString();
        }
        catch
        {
            return code; // fall back to raw
        }
    }
}
