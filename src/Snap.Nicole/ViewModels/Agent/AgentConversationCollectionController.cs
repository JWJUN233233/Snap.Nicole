using CommunityToolkit.Mvvm.Messaging;
using Snap.Nicole.Core.Collections.ObjectModel;
using Snap.Nicole.Core.ComponentModel;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Snap.Nicole.ViewModels.Agent;

internal sealed class AgentConversationCollectionController : IDisposable
{
    private readonly IMessenger messenger;
    private readonly Guid messengerToken;
    private readonly IAgentConversationProvider conversationStore;
    private readonly AgentConversationProfileCoordinator profileCoordinator;
    private readonly NotifyPropertyChangedEventRevoker conversationsChangedEventRevoker;
    private bool disposed;

    public AgentConversationCollectionController(IAgentConversationProvider conversationStore, AgentConversationProfileCoordinator profileCoordinator, IMessenger messenger, Guid messengerToken)
    {
        this.messenger = messenger;
        this.messengerToken = messengerToken;
        this.conversationStore = conversationStore;
        this.profileCoordinator = profileCoordinator;

        conversationsChangedEventRevoker = NotifyPropertyChangedEvents.AutoRevoke(Conversations, OnConversationsPropertyChanged);
    }

    public AdvancedObservableCollection<AgentConversationViewModel> Conversations { get; } = [];

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        conversationsChangedEventRevoker.Dispose();
    }

    public void LoadConversations()
    {
        foreach (AgentConversation conversation in conversationStore.LoadConversations().OrderByDescending(static item => item.UpdatedAt))
        {
            AgentConversationViewModel viewModel = AgentConversationViewModel.Create(conversation);
            profileCoordinator.RefreshConversationModelId(viewModel);
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

    public bool DeleteCurrentConversation()
    {
        if (Conversations.CurrentItem is not AgentConversationViewModel conversation)
        {
            return false;
        }

        SentryDiagnostics.AddBreadcrumb("Delete chat conversation", SentryBreadcrumbCategories.AIChat, SentryBreadcrumbTypes.UI);

        int oldIndex = Conversations.IndexOf(conversation);
        AgentConversationViewModel? nextConversation = GetNextConversationAfterDelete(oldIndex);
        Conversations.CurrentItem = nextConversation;
        conversationStore.DeleteConversation(conversation.Id);
        Conversations.Remove(conversation);

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
        conversationStore.SaveConversation(conversation.ToData());
    }

    private AgentConversationViewModel CreateConversationCore()
    {
        AgentConversationViewModel conversation = new()
        {
            CreatedAt = DateTimeOffset.Now,
            UpdatedAt = DateTimeOffset.Now,
        };

        profileCoordinator.InitializeConversationProfile(conversation);
        return conversation;
    }

    private AgentConversationViewModel? GetNextConversationAfterDelete(int oldIndex)
    {
        if (Conversations.Count <= 1)
        {
            return null;
        }

        int newIndex = Math.Clamp(oldIndex, 0, Conversations.Count - 2);
        if (newIndex < oldIndex)
        {
            return Conversations[newIndex];
        }

        return Conversations[newIndex + 1];
    }

    private void OnConversationsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(AdvancedObservableCollection<>.CurrentItem))
        {
            messenger.Send(new AgentCurrentConversationChangedMessage(Conversations.CurrentItem), messengerToken);
        }
    }
}
