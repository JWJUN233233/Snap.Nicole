namespace Snap.Nicole.Services.AI.Models;

[Obsolete]
internal sealed class ToolCallMetadata
{
    public string CallId { get; init; } = "";

    public string Name { get; init; } = "";

    public string? Arguments { get; init; }
}
