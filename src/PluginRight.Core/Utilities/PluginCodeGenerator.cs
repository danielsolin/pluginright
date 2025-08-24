using System;
using System.IO;
using System.Threading.Tasks;
using PluginRight.Core.Models;

namespace PluginRight.Core.Utilities;

/// <summary>
/// Provides utility methods for generating and saving plugin code.
/// </summary>
public static class PluginCodeGenerator
{
    /// <summary>
    /// Generates and saves the plugin code by combining AI-generated logic with a template.
    /// </summary>
    /// <param name="generatedLogic">The AI-generated C# logic.</param>
    /// <param name="job">The job details, used for naming the output file.</param>
    /// <param name="repoRoot">The root directory of the repository.</param>
    /// <returns>The absolute path to the generated plugin file.</returns>
    public static async Task<string> GenerateAndSavePluginCodeAsync(
        string generatedLogic,
        Job job,
        string repoRoot
    )
    {
        // Read the template file
        var templatePath = Path.Combine(repoRoot, "templates/__StandardPlugin__.cs.txt");
        var templateContent = await File.ReadAllTextAsync(templatePath);

        // Replace the placeholder with the generated logic
        var finalContent = templateContent.Replace("// [AI_LOGIC_HERE]", generatedLogic);

        // Construct the output file path
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var outputFileName = $"{job.Name ?? "GeneratedPlugin"}-{timestamp}.cs";
        var outputPath = Path.Combine(repoRoot, "orders", outputFileName);

        // Write the final content to the file
        await File.WriteAllTextAsync(outputPath, finalContent);

        return outputPath;
    }
}