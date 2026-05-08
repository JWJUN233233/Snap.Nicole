namespace Snap.Nicole.Services.AI.Models;

internal sealed class ToolDefinition
{
    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public string ParametersJsonSchema { get; init; } = "{}";
}
