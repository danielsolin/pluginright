using System.CommandLine;
using System.Text.Json;
using System.Text;
using PluginRight.Core;
using PluginRight.Core.Models;
using PluginRight.Core.Interfaces;
using PluginRight.Core.Services;

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

                var job = JsonSerializer.Deserialize<Job>(
                    specJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
                if (job == null)
                    throw new InvalidOperationException(
                        "Job deserialization returned null."
                    );
                else
                    ValidateJob(job);

                // 2) Load template file
                var templatePath = Path.Combine(templatesDir.FullName, templateName);
                if (!File.Exists(templatePath))
                    throw new FileNotFoundException(
                        $"Template not found: {templatePath}",
                        templatePath
                    );

                var template = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

                // 3) Resolve substitutions (class name etc.)
                var className = DeriveClassName(job);
                var rendered = template
                    .Replace("{{CLASS_NAME}}", className)
                    .Replace("{{NAMESPACE}}", job.Namespace ?? "PluginRight.Plugins");

                // 4) Ask model for business logic (stubbed for MVP)
                IModelClient model = new StubModelClient(seed);
                var logic = await model.GenerateLogicAsync(job);

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

    private static void ValidateJob(Job j)
    {
        if (string.IsNullOrWhiteSpace(j.Entity))
            throw new("Job.Entity required");

        if (string.IsNullOrWhiteSpace(j.Message))
            throw new("Job.Message required");

        if (j.Stage is null)
            throw new("Job.Stage required");

        if (string.IsNullOrWhiteSpace(j.Mode))
            throw new("Job.Mode required");

        if (string.IsNullOrWhiteSpace(j.System))
            throw new("Job.System required");

        if (string.IsNullOrWhiteSpace(j.User))
            throw new("Job.User required");
    }

    private static string DeriveClassName(Job j)
    {
        // e.g., Account_PostCreate_CreateTask
        var ent = Capitalize(j.Entity);
        var msg = Capitalize(j.Message);
        return $"{ent}{msg}_{j.Name ?? "Generated"}";
    }

    private static string Capitalize(string text)
        => string.IsNullOrEmpty(text) ? text :
            char.ToUpperInvariant(text[0]) + text[1..];
}