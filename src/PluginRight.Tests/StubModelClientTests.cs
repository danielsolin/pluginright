using System.Threading.Tasks;
using NUnit.Framework;
using PluginRight.Core.Models;
using PluginRight.Core.Services;

namespace PluginRight.Tests;

/// <summary>
/// Unit tests for the <see cref="StubModelClient"/> class.
/// </summary>
[TestFixture]
public class StubModelClientTests
{
    /// <summary>
    /// Verifies that <see cref="StubModelClient.GenerateLogicAsync(Spec)"/> returns the expected placeholder logic.
    /// </summary>
    [Test]
    public async Task GenerateLogicAsync_ReturnsExpectedLogic()
    {
        // Arrange
        var spec = new Spec
        {
            Entity = "account",
            Message = "Create",
            Stage = 40,
            Mode = "Sync",
            Description = "Test plugin",
            Namespace = "PluginRight.Plugins",
            Name = "TestPlugin"
        };

        var client = new StubModelClient(seed: 123);

        // Act
        var result = await client.GenerateLogicAsync(spec);

        // Assert
        Assert.That(result, Does.Contain("// TODO: Replace with AI-generated logic"));
        Assert.That(result, Does.Contain("tracingService.Trace(\"Generating logic (stub)\");"));
        Assert.That(result, Does.Contain("var target = (Entity)context.InputParameters[\"Target\"];"));
        Assert.That(result, Does.Contain("var task = new Entity(\"task\");"));
        Assert.That(result, Does.Contain("service.Create(task);"));
    }
}