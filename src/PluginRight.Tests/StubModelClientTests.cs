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
    /// Verifies that <see cref="StubModelClient.GenerateLogicAsync(Job)"/> returns the
    /// expected placeholder logic.
    /// </summary>
    [Test]
    public async Task GenerateLogicAsync_ReturnsExpectedLogic()
    {
        // Arrange
        var job = new Job
        {
            Entity = "account",
            Message = "Create",
            Stage = 40,
            Mode = "Sync",
            System = "You are an expert C# developer specializing in creating robust, " +
                     "efficient, and standardized Microsoft Dynamics 365 plugins.",
            User = "When a new Account is created, create a follow-up task due in 7 " +
                   "days regarding that account.",
            Namespace = "PluginRight.Plugins",
            Name = "CreateTask"
        };

        var client = new StubModelClient(seed: 123);

        // Act
        var result = await client.GenerateLogicAsync(job);

        // Assert
        Assert.That(result, Does.Contain(
            "// TODO: Replace with AI-generated logic"));
        Assert.That(result, Does.Contain(
            "tracingService.Trace(\"Generating logic (stub)\");"));
        Assert.That(result, Does.Contain(
            "var target = (Entity)context.InputParameters[\"Target\"];"));
        Assert.That(result, Does.Contain("var task = new Entity(\"task\");"));
        Assert.That(result, Does.Contain("service.Create(task);"));
    }
    }
}