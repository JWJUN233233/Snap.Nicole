using Snap.Nicole.Core.Collections.ObjectModel;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Linq;
using System.Threading;

namespace Snap.Nicole.ViewModels.Agent;

internal sealed class AgentConversationCollectionController(IAgentConversationProvider conversationProvider, IAgentService agentService, AppSettings settings)
    : IAgentConversationOwner, IDisposable
{
    private readonly IAgentConversationProvider conversationProvider = conversationProvider;
    private readonly IAgentService agentService = agentService;
    private readonly AgentConversationRuntimeCoordinator runtimeCoordinator = new(agentService);
    private readonly AgentConversationProfileCoordinator profileCoordinator = new(settings);

    private bool disposed;

    public AdvancedObservableCollection<AgentConversationViewModel> Conversations { get; } = [];

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        foreach (AgentConversationViewModel conversation in Conversations)
        {
            conversation.Dispose();
        }
    }

    public bool DeleteConversation(AgentConversationViewModel conversation)
    {
        if (!Conversations.Contains(conversation) || conversation.IsBusy)
        {
            return false;
        }

        SentryDiagnostics.AddBreadcrumb("Delete chat conversation", SentryBreadcrumbCategories.AIChat, SentryBreadcrumbTypes.UI);

        bool isCurrentConversation = ReferenceEquals(Conversations.CurrentItem, conversation);
        int oldIndex = Conversations.IndexOf(conversation);
        if (isCurrentConversation && Conversations.Count > 1)
        {
            int newIndex = Math.Clamp(oldIndex, 0, Conversations.Count - 2);
            if (newIndex < oldIndex)
            {
                Conversations.CurrentItem = Conversations[newIndex];
            }
            else
            {
                Conversations.CurrentItem = Conversations[newIndex + 1];
            }
        }

        conversationProvider.DeleteConversation(conversation.Id);
        Conversations.Remove(conversation);
        conversation.Dispose();

        if (Conversations.Count is 0)
        {
            AgentConversationViewModel newConversation = CreateConversationCore();
            Conversations.Add(newConversation);
            Conversations.CurrentItem = newConversation;
        }

        return true;
    }

    public void SaveConversation(AgentConversationViewModel conversation)
    {
        conversationProvider.SaveConversation(conversation.ToData());
    }

    public void LoadConversations()
    {
        foreach (AgentConversation conversation in conversationProvider.LoadConversations().OrderByDescending(static item => item.UpdatedAt))
        {
            AgentConversationViewModel viewModel = AgentConversationViewModel.Create(conversation, agentService, runtimeCoordinator, profileCoordinator, this);
            profileCoordinator.ResolveConversationProfile(viewModel, conversation.ModelProviderProfileId, conversation.ModelProfileId);
            Conversations.Add(viewModel);
        }

        if (Conversations.Count is 0)
        {
            Conversations.Add(CreateConversationCore());
        }

        Conversations.MoveCurrentToFirst();
    }

    public AgentConversationViewModel CreateConversation()
    {
        AgentConversationViewModel conversation = CreateConversationCore();
        Conversations.Insert(0, conversation);
        Conversations.CurrentItem = conversation;
        SaveConversation(conversation);
        return conversation;
    }

    private AgentConversationViewModel CreateConversationCore()
    {
        AgentConversationViewModel conversation = new(agentService, runtimeCoordinator, profileCoordinator, this);
        profileCoordinator.ResolveConversationProfile(conversation, null, null);
        return conversation;
    }
}
