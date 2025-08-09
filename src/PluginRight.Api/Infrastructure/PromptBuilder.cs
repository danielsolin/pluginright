using System.Text;

namespace PluginRight.Api.Infrastructure;

static class PromptBuilder
{
    private static string? ReadPromptFile(string fileName)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "Prompts", fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }
        catch
        {
            // ignore and fallback
        }
        return null;
    }

    public static string BuildSystemPrompt()
    {
        var fromFile = ReadPromptFile("system_prompt.txt");
        if (string.IsNullOrWhiteSpace(fromFile))
        {
            throw new InvalidOperationException(
                "Missing system prompt: Prompts/system_prompt.txt"
            );
        }
        return fromFile.Trim();
    }

    public static string BuildUserPrompt(string metadataYaml, string userPrompt)
    {
        var tmpl = ReadPromptFile("user_prompt_template.txt");
        if (string.IsNullOrWhiteSpace(tmpl))
        {
            throw new InvalidOperationException(
                "Missing user prompt template: " +
                "Prompts/user_prompt_template.txt"
            );
        }
        return tmpl
            .Replace("{{METADATA_YAML}}", metadataYaml ?? string.Empty)
            .Replace("{{USER_PROMPT}}", userPrompt ?? string.Empty)
            .Trim();
    }
}
