namespace Snap.Nicole.ViewModels.Agent;

internal interface IAgentConversationOwner
{
    void SaveConversation(AgentConversationViewModel conversation);

    bool DeleteConversation(AgentConversationViewModel conversation);
}
