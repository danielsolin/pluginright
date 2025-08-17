using System.IO;
using System.Text.Json;
using PluginRight.Core.Models;

namespace PluginRight.Core.Utilities
{
    public static class JobReader
    {
        public static Job? ReadJobFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Job>(json, JsonOptions.Default);
        }
    }
}
