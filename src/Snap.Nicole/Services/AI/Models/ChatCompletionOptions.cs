using Microsoft.Extensions.AI;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ChatCompletionOptions
{
    public string Model { get; init; } = "";

    public string? SystemPrompt { get; init; }

    public float Temperature { get; init; } = 0.3f;

    public float TopP { get; init; } = 0.95f;

    public ReasoningEffort? ReasoningEffort { get; init; }
}
