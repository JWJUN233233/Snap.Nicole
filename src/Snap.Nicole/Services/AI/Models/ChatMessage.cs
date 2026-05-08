using System;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ChatMessage
{
    public ChatRole Role { get; init; }

    public string Content { get; init; } = "";

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public IReadOnlyList<ToolCall>? ToolCalls { get; init; }

    public string? ToolCallId { get; init; }

    public string? ModelId { get; init; }
}
