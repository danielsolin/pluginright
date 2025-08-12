#nullable enable

using System.Threading.Tasks;
using PluginRight.Core.Models;

namespace PluginRight.Core.Interfaces;

/// <summary>
/// Defines a client for generating plugin logic.
/// </summary>
public interface IModelClient
{
    /// <summary>
    /// Generates the business logic for a plugin based on the provided specification.
    /// </summary>
    /// <param name="spec">The specification for the plugin.</param>
    /// <returns>A task that represents the asynchronous operation, containing the
    /// generated logic as a string.</returns>
    Task<string> GenerateLogicAsync(Spec spec);
}

#nullable disable
