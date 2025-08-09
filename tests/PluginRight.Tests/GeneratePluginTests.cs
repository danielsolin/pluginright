using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PluginRight.Core.OpenAI;

public class GeneratePluginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GeneratePluginTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IOpenAIClient with a fake for deterministic tests
                services.AddSingleton<IOpenAIClient, FakeOpenAIClient>();
            });
        });
    }

    [Fact]
    public async Task Generate_ComplexPlugin_Returns_CSharp_File()
    {
        var client = _factory.CreateClient();

        // Use appsettings ApiKey default "dev"
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev");

        var metadata =
            @"entities:
  - logicalName: account
    attributes:
      - name: name
        type: string
      - name: creditlimit
        type: money
  - logicalName: contact
    attributes:
      - name: emailaddress1
        type: string
      - name: parentcustomerid
        type: customer";

        var body = new
        {
            metadata_yaml = metadata,
            user_prompt = "On Account Update, when creditlimit changes, "
                + "propagate a flag to all related Contacts via parentcustomerid; "
                + "if emailaddress1 is empty, log a warning. Use PostOperation.",
            model = "test-model",
        };

        var resp = await client.PostAsync(
            "/v1/plugins/generate",
            new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            )
        );

        Assert.True(
            resp.IsSuccessStatusCode,
            $"Expected 2xx, got {(int)resp.StatusCode}"
        );

        var text = await resp.Content.ReadAsStringAsync();

        // Basic assertions that we got plausible C# code back
        Assert.Contains("namespace", text);
        Assert.Contains("class", text);
        Assert.Contains("IPlugin", text);
        Assert.Contains(
            "public void Execute(IServiceProvider serviceProvider)",
            text
        );

        // Ensure no markdown fences or explanations leaked
        Assert.DoesNotContain("```", text);
        Assert.DoesNotContain("Output only", text);
    }

    private sealed class FakeOpenAIClient : IOpenAIClient
    {
        public Task<ChatResponse> CompleteAsync(
            ChatRequest r,
            CancellationToken ct = default
        )
        {
            // Return deterministic minimal plugin code that meets constraints
            var code =
                @"using System;
using Microsoft.Xrm.Sdk;

namespace Company.Plugins
{
    // Purpose: Test plugin; PostOperation on account update.
    public class AccountCreditPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            if (context.MessageName != ""Update"" || context.PrimaryEntityName != ""account"")
                return;

            if (!context.InputParameters.Contains(""Target"") || !(context.InputParameters[""Target""] is Entity target))
                return;

            tracing?.Trace(""AccountCreditPlugin: start"");

            // fake logic placeholder
            if (!target.Attributes.Contains(""creditlimit""))
                return;

            tracing?.Trace(""AccountCreditPlugin: done"");
        }
    }
}";

            return Task.FromResult(new ChatResponse(code, 10, 20));
        }
    }
}
