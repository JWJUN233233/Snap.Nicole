namespace Snap.Nicole.Services.AI.Models;

internal sealed class ExtendedAgentContentSegment
{
    public ExtendedAIContentKind Kind { get; init; }

    public string Content { get; init; } = "";

    public ToolCallMetadata? Metadata { get; init; }
}

internal sealed class ToolCallMetadata
{
    public string CallId { get; init; } = "";

    public string Name { get; init; } = "";

    public string? Arguments { get; init; }
}
