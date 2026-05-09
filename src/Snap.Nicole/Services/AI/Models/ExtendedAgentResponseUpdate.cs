using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ExtendedAgentResponseUpdate
{
    [JsonConstructor]
    public ExtendedAgentResponseUpdate()
    {
    }

    public ExtendedAgentResponseUpdate(ChatRoleKind roleKind, string content)
    {
        RoleKind = roleKind;
        Content = content;
    }

    public ChatRoleKind RoleKind { get; init; }

    public string Content { get; init; } = "";

    public string? ReasoningContent { get; init; }

    public IReadOnlyList<ExtendedAgentContentSegment> Segments { get; init; } = [];

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public string? ModelId { get; init; }
}
