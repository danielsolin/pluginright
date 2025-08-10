using System.Text;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0 ||
            (args.Length == 1 && IsHelp(args[0])))
        {
            PrintHelp();
            return 0;
        }

        if (args.Length >= 2 &&
            args[0] == "new" && args[1] == "plugin")
        {
            return NewPlugin(args.Skip(2).ToArray());
        }

        Console.Error.WriteLine("Unknown command. Use --help for usage.");
        return 1;
    }

    private static bool IsHelp(string arg) =>
        arg is "-h" or "--help" or "help";

    private static void PrintHelp()
    {
        Console.WriteLine("PluginRight CLI\n");
        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  pluginright new plugin --name <Name> " +
            "[--namespace <Ns>] [--entity <logical>] " +
            "[--message <Msg>] [--stage <Stage>] [--out <dir>]\n");
        Console.WriteLine("Examples:");
        Console.WriteLine(
            "  pluginright new plugin --name AccountCreate " +
            "--namespace Company.Dataverse.Plugins --entity account " +
            "--message Create --stage PostOperation " +
            "--out ./out/AccountCreate");
    }

    private static int NewPlugin(string[] args)
    {
        var opts = ParseOptions(args);
        if (!opts.TryGetValue("name", out var name) ||
            string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("--name is required");
            return 2;
        }

        var ns = opts.GetValueOrDefault(
            "namespace", "PluginRight.Generated");
        var entity = opts.GetValueOrDefault("entity", "entity");
        var message = opts.GetValueOrDefault("message", "Create");
        var stage = opts.GetValueOrDefault("stage", "PostOperation");

        var outDir = opts.GetValueOrDefault(
            "out", Path.Combine(".", "out", name));
        Directory.CreateDirectory(outDir);

        var templateRoot = Path.Combine(
            AppContext.BaseDirectory, "templates", "csharp-plugin");
        if (!Directory.Exists(templateRoot))
        {
            Console.Error.WriteLine(
                $"Templates not found: {templateRoot}");
            return 3;
        }

        var tokens =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__ASSEMBLY_NAME__"] = name,
                ["__NAMESPACE__"] = ns,
                ["__CLASS_NAME__"] = MakeValidIdentifier(name) + "Plugin",
                ["__DESCRIPTION__"] =
                $"{message} {entity} at {stage}",
                ["__MESSAGE__"] = message,
                ["__STAGE__"] = stage,
                ["__ENTITY__"] = entity,
            };

        CopyTemplateWithTokens(templateRoot, outDir, tokens);

        Console.WriteLine(
            $"Generated plugin project at: " +
            $"{Path.GetFullPath(outDir)}\n");

        var csprojPath = Path.Combine(outDir, "Plugin.csproj");

        Console.WriteLine("Next steps:");
        Console.WriteLine(
            $"  dotnet add \"{csprojPath}\" package " +
            "Microsoft.CrmSdk.CoreAssemblies");
        Console.WriteLine(
            $"  dotnet build \"{csprojPath}\"");
        return 0;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var dict = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("--")) continue;
            var key = a.Substring(2);
            string? value = null;
            if (i + 1 < args.Length &&
                !args[i + 1].StartsWith("--"))
            {
                value = args[++i];
            }
            dict[key] = value ?? "true";
        }
        return dict;
    }

    private static string MakeValidIdentifier(string input)
    {
        var sb = new StringBuilder(input.Length);
        // First char: must be letter or '_'
        char first = input.Length > 0 ? input[0] : '_';
        sb.Append(char.IsLetter(first) || first == '_' ? first : '_');
        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i];
            sb.Append(
                char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }
        return sb.ToString();
    }

    private static void CopyTemplateWithTokens(
        string sourceDir,
        string destDir,
        IDictionary<string, string> tokens)
    {
        foreach (var dir in Directory.GetDirectories(
                     sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(
                destDir, ReplaceTokens(relative, tokens)));
        }

        foreach (var file in Directory.GetFiles(
                     sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var targetRelative = ReplaceTokens(relative, tokens);
            var targetPath = Path.Combine(destDir, targetRelative);
            var text = File.ReadAllText(file);
            text = ReplaceTokens(text, tokens);
            File.WriteAllText(
                targetPath,
                text,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));
        }
    }

    private static string ReplaceTokens(
        string text,
        IDictionary<string, string> tokens)
    {
        foreach (var kv in tokens)
        {
            text = text.Replace(
                kv.Key, kv.Value, StringComparison.Ordinal);
        }
        return text;
    }
}