namespace Snap.Nicole.ViewModels.Agent;

internal sealed class AgentCurrentConversationChangedMessage
{
    public AgentCurrentConversationChangedMessage(AgentConversationViewModel? conversation)
    {
        Conversation = conversation;
    }

    public AgentConversationViewModel? Conversation { get; }
}
