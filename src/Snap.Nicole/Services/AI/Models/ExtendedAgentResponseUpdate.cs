using System;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ExtendedAgentResponseUpdate
{
    public ChatRoleKind RoleKind { get; init; }

    public string Content { get; init; } = "";

    public IReadOnlyList<ExtendedAgentContentSegment> Segments { get; init; } = [];

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public string? ModelId { get; init; }
}

internal sealed class ExtendedAgentContentSegment
{
    public ExtendedAgentContentKind Kind { get; init; }

    public string Content { get; init; } = "";
}

internal enum ExtendedAgentContentKind
{
    Text,
    Reasoning,
    ToolCall,
    ToolResult,
}
