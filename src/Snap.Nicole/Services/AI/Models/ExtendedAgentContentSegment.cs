namespace Snap.Nicole.Services.AI.Models;

[Obsolete]
internal sealed class ExtendedAgentContentSegment
{
    public ExtendedAIContentKind Kind { get; init; }

    public string Content { get; init; } = "";

    public ToolCallMetadata? Metadata { get; init; }
}
