using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Text.Json;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class AgentConversationData
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public Guid? ModelProviderProfileId { get; set; }

    public Guid? ModelProfileId { get; set; }

    public ModelProviderType ProviderType { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public int MessageCount { get; set; }

    public JsonElement? SerializedSessionState { get; set; }

    public List<ChatMessage> HistoryMessages { get; set; } = [];
}
