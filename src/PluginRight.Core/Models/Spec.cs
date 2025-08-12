#nullable enable

namespace PluginRight.Core.Models;

public sealed record Spec
{
    public string Entity { get; init; } = string.Empty; // e.g., "account"
    public string Message { get; init; } = string.Empty; // e.g., "Create", "Update"
    public int? Stage { get; init; } // e.g., 40
    public string Mode { get; init; } = string.Empty; // "Sync" | "Async"
    public string Description { get; init; } = string.Empty;
    public string? Namespace { get; init; }
    public string? Name { get; init; } // short purpose label, e.g., "CreateTask"
}

#nullable disable
