#nullable enable

namespace PluginRight.Core.Models;

/// <summary>
/// Represents the specification for a plugin.
/// </summary>
public sealed record Spec
{
    /// <summary>
    /// The entity involved in the plugin (e.g., "account").
    /// </summary>
    public string Entity { get; init; } = string.Empty;

    /// <summary>
    /// The message or action triggering the plugin (e.g., "Create", "Update").
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The stage of the plugin execution pipeline (e.g., 40).
    /// </summary>
    public int? Stage { get; init; }

    /// <summary>
    /// The mode of execution (e.g., "Sync" or "Async").
    /// </summary>
    public string Mode { get; init; } = string.Empty;

    /// <summary>
    /// A description of the plugin's purpose.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// The namespace for the generated plugin.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// A short label for the plugin (e.g., "CreateTask").
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The full prompt for generating plugin logic.
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
}

#nullable disable
