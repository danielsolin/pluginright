#nullable enable

using System.Text;
using System.Threading.Tasks;

namespace PluginRight.Core;

public sealed record Spec
{
    public string Entity { get; init; } = string.Empty; // e.g., "account"
    public string Message { get; init; } = string.Empty; // e.g., "Create", "Update"
    public int? Stage { get; init; } // e.g., 40
    public string Mode { get; init; } = string.Empty; // "Sync" | "Async"
    public string Description { get; init; } = string.Empty;
    public string? Namespace { get; init; }
    public string? Name { get; init; } // short purpose label, e.g., "CreateTask"
}

public interface IModelClient
{
    Task<string> GenerateLogicAsync(Spec spec);
}

public sealed class StubModelClient : IModelClient
{
    private readonly int _seed;
    public StubModelClient(int seed) => _seed = seed;

    public Task<string> GenerateLogicAsync(Spec spec)
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