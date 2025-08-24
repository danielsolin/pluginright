using System.IO;
using System.Text.Json;
using PluginRight.Core.Models;

namespace PluginRight.Core.Utilities
{
    public static class JobReader
    {
        public static Job? ReadJobFromFile(string jobFileName)
        {
            var repoRoot = PathUtilities.GetRepositoryRoot();
            var filePath = Path.Combine(repoRoot, "orders", jobFileName);

            if (!File.Exists(filePath))
                return null;
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Job>(json, JsonOptions.Default);
        }
    }
}
