#nullable enable

using System.Text;
using System.Threading.Tasks;
using PluginRight.Core.Interfaces;
using PluginRight.Core.Models;

namespace PluginRight.Core.Services;

/// <summary>
/// A stub implementation of <see cref="IModelClient"/> for generating placeholder
/// plugin logic.
/// </summary>
public sealed class StubModelClient : IModelClient
{
    /// <summary>
    /// The deterministic seed for generating logic.
    /// </summary>
    private readonly int _seed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubModelClient"/> class.
    /// </summary>
    /// <param name="seed">The deterministic seed for generating logic.</param>
    public StubModelClient(int seed) => _seed = seed;

    /// <inheritdoc/>
    public Task<string> GenerateLogicAsync(Job job)
    {
        // Deterministic, minimal placeholder logic for MVP;
        // replace with real OpenAI call later.
        var sb = new StringBuilder();
        sb.AppendLine("// TODO: Replace with AI-generated logic");
        sb.AppendLine("tracingService.Trace(\"Generating logic (stub)\");");
        sb.AppendLine("// Example: create a task for new account");
        sb.AppendLine("var target = (Entity)context.InputParameters[\"Target\"]; ");
        sb.AppendLine("var name = target.GetAttributeValue<string>(\"name\") ?? \"\";");
        sb.AppendLine("var task = new Entity(\"task\");");
        sb.AppendLine("task[\"subject\"] = $\"Follow-up with {name}\";");
        sb.AppendLine("task[\"regardingobjectid\"] = target.ToEntityReference();");
        sb.AppendLine("task[\"scheduledend\"] = DateTime.UtcNow.AddDays(7);");
        sb.AppendLine("service.Create(task);");
        return Task.FromResult(sb.ToString());
    }
}

#nullable disable
