using System.CommandLine;
using System.Text.Json;
using System.Text;

namespace PluginRight.CLI;

internal static class Program
{
    private const string AiLogicMarker = "// [AI_LOGIC_HERE]";

    public static async Task<int> Main(string[] args)
    {
        var specOpt = new Option<FileInfo>
        (
            name: "--spec",
            description: "Path to spec JSON."
        )
        { IsRequired = true };

        var templateNameOpt = new Option<string>
        (
            name: "--template",
            description: "Template name (file in templates dir)."
        )
        { IsRequired = true };

        var templateOpt = new Option<DirectoryInfo>
        (
            name: "--templates",
            () => new DirectoryInfo("templates"),
            description: "Templates directory."
        );

        var outOpt = new Option<FileInfo?>
        (
            name: "--out",
            description: "Write artifact to path instead of stdout."
        );

        var seedOpt = new Option<int>
        (
            name: "--seed",
            getDefaultValue: () => 0,
            description: "Deterministic seed (for model)."
        );

        var quietOpt = new Option<bool>
        (
            name: "--quiet",
            description: "Suppress non-error stderr logs."
        );

        var generate = new Command("generate", "Generate plugin code from template")
        {
            specOpt, templateOpt, templateNameOpt, outOpt, seedOpt, quietOpt
        };

        generate.SetHandler(async (FileInfo specFile, DirectoryInfo templatesDir,
            string templateName, FileInfo? outPath, int seed, bool quiet) =>
        {
            try
            {
                var log = new Action<string>(
                    m =>
                    {
                        if (!quiet) Console.Error.WriteLine(m);
                    }
                );

                log($"pluginright generate — seed={seed}");

                // 1) Load & parse spec
                if (!specFile.Exists)
                {
                    throw new FileNotFoundException(
                        "Spec file not found: {specFile.FullName}",
                        specFile.FullName
                    );
                }

                var specJson = await File.ReadAllTextAsync(
                    specFile.FullName,
                    Encoding.UTF8
                );

                var spec = JsonSerializer.Deserialize<Spec>(
                    specJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
                if (spec == null)
                    throw new InvalidOperationException(
                        "Spec deserialization returned null."
                    );
                else
                    ValidateSpec(spec);

                // 2) Load template file
                var templatePath = Path.Combine(templatesDir.FullName, templateName);
                if (!File.Exists(templatePath))
                    throw new FileNotFoundException(
                        $"Template not found: {templatePath}",
                        templatePath
                    );

                var template = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

                // 3) Resolve substitutions (class name etc.)
                var className = DeriveClassName(spec);
                var rendered = template
                    .Replace("{{CLASS_NAME}}", className)
                    .Replace("{{NAMESPACE}}", spec.Namespace ?? "PluginRight.Plugins");

                // 4) Ask model for business logic (stubbed for MVP)
                IModelClient model = new StubModelClient(seed);
                var logic = await model.GenerateLogicAsync(spec);

                if (!rendered.Contains(AiLogicMarker))
                    throw new InvalidOperationException(
                        $"Template missing marker '{AiLogicMarker}'."
                    );

                rendered = rendered.Replace(AiLogicMarker, logic);

                // 5) Emit to stdout OR file
                if (outPath is null)
                {
                    Console.Out.Write(rendered);
                }
                else
                {
                    Directory.CreateDirectory(outPath.DirectoryName!);
                    await File.WriteAllTextAsync(
                        outPath.FullName, rendered, Encoding.UTF8
                    );

                    // When writing to file, keep stdout empty; log to stderr
                    log($"Wrote: {outPath.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }, specOpt, templateOpt, templateNameOpt, outOpt, seedOpt, quietOpt);

        var root = new RootCommand("PluginRight CLI");
        root.AddCommand(generate);
        return await root.InvokeAsync(args);
    }

    private static void ValidateSpec(Spec s)
    {
        if (string.IsNullOrWhiteSpace(s.Entity))
            throw new("Spec.Entity required");
        if (string.IsNullOrWhiteSpace(s.Message))
            throw new("Spec.Message required");
        if (s.Stage is null)
            throw new("Spec.Stage required");
        if (string.IsNullOrWhiteSpace(s.Mode))
            throw new("Spec.Mode required");
        if (string.IsNullOrWhiteSpace(s.Description))
            throw new("Spec.Description required");
    }

    private static string DeriveClassName(Spec s)
    {
        // e.g., Account_PostCreate_CreateTask
        var ent = Capitalize(s.Entity);
        var msg = Capitalize(s.Message);
        return $"{ent}{msg}_{s.Name ?? "Generated"}";
    }

    private static string Capitalize(string text)
        => string.IsNullOrEmpty(text) ? text :
            char.ToUpperInvariant(text[0]) + text[1..];
}

internal sealed record Spec
{
    public string Entity { get; init; } = string.Empty; // e.g., "account"
    public string Message { get; init; } = string.Empty; // e.g., "Create", "Update"
    public int? Stage { get; init; } // e.g., 40
    public string Mode { get; init; } = string.Empty; // "Sync" | "Async"
    public string Description { get; init; } = string.Empty;
    public string? Namespace { get; init; }
    public string? Name { get; init; } // short purpose label, e.g., "CreateTask"
}

internal interface IModelClient
{
    Task<string> GenerateLogicAsync(Spec spec);
}

internal sealed class StubModelClient : IModelClient
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