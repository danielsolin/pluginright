using System;
using System.IO;
using System.Threading.Tasks;

namespace PluginRight.Core.Utilities;

/// <summary>
/// Provides utility methods for reading API keys from files.
/// </summary>
public static class ApiKeyReader
{
    /// <summary>
    /// Reads an API key from a specified file within the repository's 'keys' directory.
    /// </summary>
    /// <param name="keyFileName">The name of the API key file (e.g., "openai.key", "gemini.key").</param>
    /// <returns>The content of the API key file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the API key file is not found or is empty.</exception>
    public static async Task<string> ReadApiKeyAsync(string keyFileName)
    {
        var repoRoot = PathUtilities.GetRepositoryRoot();
        var apiKeyPath = Path.Combine(repoRoot + "/keys", keyFileName);

        if (!File.Exists(apiKeyPath))
        {
            throw new InvalidOperationException($"API key file not found: {keyFileName}.");
        }

        var apiKey = await File.ReadAllTextAsync(apiKeyPath);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"API key file is empty: {keyFileName}.");
        }

        return apiKey.Trim();
    }
}