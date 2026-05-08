namespace Snap.Nicole.Services.AI.Models;

internal sealed class ToolCall
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Arguments { get; init; } = "";
}
