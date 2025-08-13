using System;
using System.IO;

namespace PluginRight.Core.Utilities
{
    /// <summary>
    /// Provides utility methods for working with file paths.
    /// </summary>
    public static class PathUtilities
    {
        /// <summary>
        /// Finds the root directory of the repository by locating the ".git" folder.
        /// </summary>
        /// <returns>The absolute path to the repository root.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the repository root cannot be found.</exception>
        public static string GetRepositoryRoot()
        {
            var currentDir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(currentDir))
            {
                if (Directory.Exists(Path.Combine(currentDir, ".git")))
                {
                    return currentDir;
                }

                currentDir = Directory.GetParent(currentDir)?.FullName;
            }

            throw new InvalidOperationException("Repository root not found.");
        }
    }
}
