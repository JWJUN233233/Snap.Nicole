using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI;

internal interface IAgentConversationStore
{
    IReadOnlyList<AgentConversationData> LoadConversations();

    void SaveConversation(AgentConversationData conversation);

    void DeleteConversation(Guid id);
}
