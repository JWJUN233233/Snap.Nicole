using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI;

internal interface IAgentConversationStore
{
    IReadOnlyList<AgentConversation> LoadConversations();

    void SaveConversation(AgentConversation conversation);

    void DeleteConversation(Guid id);
}
