using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sentry;
using Snap.Nicole.Core.Collections.ObjectModel;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Core.Threading;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels.Agent;

internal sealed partial class AgentViewModel : ObservableObject, IDisposable,
    IRecipient<AgentConversationProfileChangedMessage>,
    IRecipient<AgentCurrentConversationChangedMessage>
{
    private readonly Guid messengerToken = Guid.NewGuid();
    private readonly IMessenger messenger;
    private readonly IAgentService agentService;
    private readonly AgentConversationRuntimeCoordinator conversationRuntimeCoordinator;
    private readonly AgentConversationProfileCoordinator conversationProfileCoordinator;
    private readonly AgentConversationCollectionController conversationCollectionController;

    private CancellationTokenSource? generationCts;
    private bool disposed;

    public AgentViewModel(IServiceProvider serviceProvider)
    {
        messenger = serviceProvider.GetRequiredService<IMessenger>();
        agentService = serviceProvider.GetRequiredService<IAgentService>();
        Settings = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;

        conversationRuntimeCoordinator = new(agentService);
        conversationProfileCoordinator = new(Settings, messenger, messengerToken);
        conversationCollectionController = new(serviceProvider.GetRequiredService<IAgentConversationProvider>(), conversationProfileCoordinator, messenger, messengerToken);
        Conversations = conversationCollectionController.Conversations;

        messenger.RegisterAll(this, messengerToken);

        conversationCollectionController.LoadConversations();
    }

    public AppSettings Settings { get; }

    public AdvancedObservableCollection<AgentConversationViewModel> Conversations { get; }

    [ObservableProperty]
    public partial ObservableChatMessageCollection Messages { get; private set; } = [];

    [ObservableProperty]
    public partial AgentConversationStatisticsViewModel ConversationStatistics { get; set; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand), nameof(DeleteConversationCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage
    {
        get
        {
            return !disposed
                && !IsBusy
                && Conversations.CurrentItem is not null
                && !string.IsNullOrWhiteSpace(InputText)
                && conversationProfileCoordinator.HasSelectedModel;
        }
    }

    private bool CanDeleteConversation { get => !disposed && !IsBusy && Conversations.CurrentItem is not null; }

    [RelayCommand]
    private void CreateConversation()
    {
        if (disposed)
        {
            return;
        }

        conversationCollectionController.CreateConversation();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteConversation))]
    private void DeleteConversation()
    {
        if (!CanDeleteConversation)
        {
            return;
        }

        conversationCollectionController.DeleteCurrentConversation();
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        if (disposed || Conversations.CurrentItem is not AgentConversationViewModel conversation)
        {
            return;
        }

        string input = InputText.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        ExtendedAgentOptions? requestOptions = conversationProfileCoordinator.CreateRequestOptions();
        if (requestOptions is null)
        {
            return;
        }

        conversationProfileCoordinator.UpdateConversationProfile(conversation, requestOptions);
        ChatClientAgent? agent = null;
        AgentSession? session = null;
        if (!string.IsNullOrWhiteSpace(requestOptions.ApiKey))
        {
            agent = await conversationRuntimeCoordinator.EnsureConversationAgentAsync(conversation, requestOptions, cancellationToken);
            session = await conversationRuntimeCoordinator.EnsureConversationSessionAsync(conversation, agent, cancellationToken);
        }
        else
        {
            AgentConversationRuntimeCoordinator.ResetConversationRuntime(conversation);
        }

        ChatMessage userMessage = new(ChatRole.User, input)
        {
            CreatedAt = DateTimeOffset.Now,
            AuthorName = "You",
        };

        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.AIChatSend, "Send chat message");
        span.SetTag(SentryTags.AIProvider, requestOptions.ProviderType.ToString());
        span.SetTag(SentryTags.AIModel, requestOptions.ModelId);

        InputText = string.Empty;
        IsBusy = true;
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        generationCts = linkedCts;

        try
        {
            TaskScheduler taskScheduler = App.Current.Threading.TaskScheduler;
            SpanStatus result;
            if (string.IsNullOrWhiteSpace(requestOptions.ApiKey))
            {
                result = await AddMissingApiKeyMessagesAsync(input, userMessage.CreatedAt, userMessage.AuthorName, Messages, taskScheduler, linkedCts.Token);
            }
            else
            {
                if (agent is null || session is null)
                {
                    throw new InvalidOperationException("Agent runtime must be initialized before streaming chat.");
                }

                result = await agentService.RunStreamingAsync(agent, userMessage, Messages, requestOptions, session, taskScheduler, linkedCts.Token);
            }

            span.Finish(result);

            conversation.UpdatedAt = DateTimeOffset.Now;
            if (string.IsNullOrWhiteSpace(conversation.Title))
            {
                conversation.Title = CreateTitle(input);
            }

            if (agent is not null && session is not null)
            {
                await conversationRuntimeCoordinator.PersistConversationSessionAsync(conversation, agent, session, linkedCts.Token);
                if (ReferenceEquals(Conversations.CurrentItem, conversation))
                {
                    RebuildConversationStatistics(Messages);
                }

                conversationCollectionController.SaveConversation(conversation);
            }
            else
            {
                if (ReferenceEquals(Conversations.CurrentItem, conversation))
                {
                    RebuildConversationStatistics(Messages);
                }

                conversationCollectionController.SaveConversation(conversation);
            }
        }
        catch (OperationCanceledException)
        {
            span.Finish(SpanStatus.Cancelled);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.AIChatSend);
            throw;
        }
        finally
        {
            if (ReferenceEquals(generationCts, linkedCts))
            {
                generationCts = null;
            }

            linkedCts.Dispose();
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        if (disposed)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Stop chat generation", SentryBreadcrumbCategories.AIChat, SentryBreadcrumbTypes.UI);
        generationCts?.Cancel();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        messenger.UnregisterAll(this, messengerToken);
        conversationCollectionController.Dispose();
        conversationProfileCoordinator.Dispose();

        generationCts?.Cancel();
        SendMessageCommand.NotifyCanExecuteChanged();
        DeleteConversationCommand.NotifyCanExecuteChanged();
    }

    public void Receive(AgentConversationProfileChangedMessage message)
    {
        OnCurrentProfileChanged();
    }

    public void Receive(AgentCurrentConversationChangedMessage message)
    {
        OnCurrentConversationChanged(message.Conversation);
    }

    private void OnCurrentConversationChanged(AgentConversationViewModel? value)
    {
        if (disposed)
        {
            return;
        }

        conversationProfileCoordinator.ApplyConversationProfile(value);
        RebuildActiveConversation(value);
        SendMessageCommand.NotifyCanExecuteChanged();
        DeleteConversationCommand.NotifyCanExecuteChanged();
    }

    private static async ValueTask<SpanStatus> AddMissingApiKeyMessagesAsync(string input, DateTimeOffset? createdAt, string? authorName, ObservableChatMessageCollection collection, TaskScheduler taskScheduler, CancellationToken cancellationToken)
    {
        SentryDiagnostics.AddBreadcrumb("Chat blocked by missing API key", SentryBreadcrumbCategories.AIChat, SentryBreadcrumbTypes.UI);

        ObservableChatMessage inputMessage = ObservableChatMessage.Create(ChatRole.User, createdAt, authorName, ObservableTextContent.Create(input));
        await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, inputMessage, cancellationToken);

        // ObservableTextContent stores a text snapshot, so this message cannot hot-switch after it is added.
        ObservableChatMessage configurationMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, content: ObservableTextContent.Create(StringResourceProxy.Default[SRName.UIXamlPagesAgentPageMessageConfigureApiKey]));
        await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, configurationMessage, cancellationToken);
        return SpanStatus.FailedPrecondition;
    }

    private void RebuildActiveConversation(AgentConversationViewModel? conversation)
    {
        if (conversation is null)
        {
            Messages = [];
            RebuildConversationStatistics([]);
            return;
        }

        Messages = conversation.Messages;
        RebuildConversationStatistics(Messages);
    }

    private void RebuildConversationStatistics(IEnumerable<ObservableChatMessage> messages)
    {
        ConversationStatistics = AgentConversationStatisticsViewModel.Create(messages);
    }

    private void OnCurrentProfileChanged()
    {
        if (disposed)
        {
            return;
        }

        if (Conversations.CurrentItem is not null)
        {
            AgentConversationRuntimeCoordinator.ResetConversationRuntime(Conversations.CurrentItem);
        }

        SendMessageCommand.NotifyCanExecuteChanged();
    }

    private static string CreateTitle(string input)
    {
        string title = input.ReplaceLineEndings(" ").Trim();
        const int maxLength = 40;
        if (title.Length <= maxLength)
        {
            return title;
        }

        return title[..maxLength] + "...";
    }
}
