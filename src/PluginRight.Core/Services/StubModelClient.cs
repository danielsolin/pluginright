#nullable enable

using System.Text;
using System.Threading.Tasks;
using PluginRight.Core.Interfaces;
using PluginRight.Core.Models;

namespace PluginRight.Core.Services;

public sealed class StubModelClient : IModelClient
{
    private readonly int _seed;
    public StubModelClient(int seed) => _seed = seed;

    public Task<string> GenerateLogicAsync(PluginRight.Core.Models.Spec spec)
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
