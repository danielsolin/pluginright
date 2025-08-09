using Xunit;

public sealed class OpenAIRequiredFactAttribute : FactAttribute
{
    public OpenAIRequiredFactAttribute()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var keyPath = Path.Combine(repoRoot, "Secrets", "openai.key");
        string? key = null;
        if (File.Exists(keyPath))
        {
            key = File.ReadAllLines(keyPath)
                .Select(l => l.Trim())
                .FirstOrDefault(l => !string.IsNullOrEmpty(l));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            Skip = "No OpenAI key. Create Secrets/openai.key at repo root " +
                "with your API key (first line).";
        }
    }
}
