using System.Collections.Generic;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ChatRequestOptions
{
    public string Model { get; init; } = "";

    public string? SystemPrompt { get; init; }

    public float Temperature { get; init; } = 0.7f;

    public IReadOnlyList<ToolDefinition>? Tools { get; init; }
}
