namespace PluginRight.Core.Models;

/// <summary>
/// Represents the job for a plugin.
/// </summary>
public sealed record Job
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
    /// The namespace for the generated plugin.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// A short label for the plugin (e.g., "CreateTask").
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The system message for the OpenAI API.
    /// </summary>
    public string System { get; init; } = string.Empty;

    /// <summary>
    /// The user message for the OpenAI API.
    /// </summary>
    public string User { get; init; } = string.Empty;
}
