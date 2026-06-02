using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sentry;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels;

internal sealed partial class AgentViewModel : ObservableObject, IDisposable
{
    private readonly IAgentService agentService;
    private readonly IAgentConversationStore conversationStore;
    private ObservableSettingsCollection<ModelProviderProfile, Guid>? modelProviderProfiles;
    private ObservableSettingsCollection<ModelProfile, Guid>? modelProfiles;

    private ObservableChatMessageCollection messages = [];
    private CancellationTokenSource? generationCts;
    private bool disposed;
    private bool isApplyingConversationSelection;

    public AgentViewModel(IServiceProvider serviceProvider)
    {
        agentService = serviceProvider.GetRequiredService<IAgentService>();
        conversationStore = serviceProvider.GetRequiredService<IAgentConversationStore>();
        Settings = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;

        Settings.PropertyChanged += OnSettingsPropertyChanged;
        SubscribeModelProviderProfiles(Settings.ModelProviderProfiles);
        SubscribeModelProfiles(Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles);

        LoadConversations();
    }

    public AppSettings Settings { get; }

    public ObservableCollection<AgentConversationViewModel> Conversations { get; } = [];

    public ObservableChatMessageCollection Messages
    {
        get => messages;
        private set => SetProperty(ref messages, value);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteConversationCommand))]
    public partial AgentConversationViewModel? SelectedConversation { get; set; }

    [ObservableProperty]
    public partial AgentHistorySummaryViewModel HistorySummary { get; set; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteConversationCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage
    {
        get
        {
            ModelProfile? modelProfile = Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles.CurrentItem;
            return !disposed
                && !IsBusy
                && SelectedConversation is not null
                && !string.IsNullOrWhiteSpace(InputText)
                && !string.IsNullOrWhiteSpace(modelProfile?.ModelId);
        }
    }

    private bool CanDeleteConversation
    {
        get
        {
            return !disposed
                && !IsBusy
                && SelectedConversation is not null;
        }
    }

    [RelayCommand]
    private void CreateConversation()
    {
        if (disposed)
        {
            return;
        }

        AgentConversationViewModel conversation = CreateConversationCore();
        Conversations.Insert(0, conversation);
        SelectedConversation = conversation;
        SaveConversation(conversation);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteConversation))]
    private void DeleteConversation()
    {
        if (!CanDeleteConversation || SelectedConversation is not AgentConversationViewModel conversation)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Delete chat conversation", "ai.chat", "ui");

        int oldIndex = Conversations.IndexOf(conversation);
        conversationStore.DeleteConversation(conversation.Id);
        Conversations.Remove(conversation);

        if (Conversations.Count is 0)
        {
            Conversations.Add(CreateConversationCore());
        }

        int newIndex = Math.Clamp(oldIndex, 0, Conversations.Count - 1);
        SelectedConversation = Conversations[newIndex];
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        if (disposed || SelectedConversation is not AgentConversationViewModel conversation)
        {
            return;
        }

        string input = InputText.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        ExtendedAgentOptions? requestOptions = CreateRequestOptions();
        if (requestOptions is null)
        {
            return;
        }

        UpdateConversationProfile(conversation, requestOptions);
        AgentSession session = await EnsureConversationSessionAsync(conversation, requestOptions, cancellationToken);

        ChatMessage userMessage = new(ChatRole.User, input)
        {
            CreatedAt = DateTimeOffset.Now,
            AuthorName = "You",
        };

        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ai.chat.send", "Send chat message");
        span.SetTag("ai.provider", requestOptions.ProviderType.ToString());
        span.SetTag("ai.model", requestOptions.ModelId);

        InputText = string.Empty;
        IsBusy = true;
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        generationCts = linkedCts;

        try
        {
            SpanStatus result = await agentService.RunStreamingAsync(userMessage, Messages, requestOptions, session, App.Current.Threading.TaskScheduler, linkedCts.Token);
            span.Finish(result);

            conversation.UpdatedAt = DateTimeOffset.Now;
            if (string.IsNullOrWhiteSpace(conversation.Title))
            {
                conversation.Title = CreateTitle(input);
            }

            if (!string.IsNullOrWhiteSpace(requestOptions.ApiKey))
            {
                await PersistConversationSessionAsync(conversation, requestOptions, session, linkedCts.Token);
            }
            else
            {
                if (ReferenceEquals(SelectedConversation, conversation))
                {
                    RebuildHistorySummary(Messages);
                }

                SaveConversation(conversation);
            }
        }
        catch (OperationCanceledException)
        {
            span.Finish(SpanStatus.Cancelled);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ai.chat.send");
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

        SentryDiagnostics.AddBreadcrumb("Stop chat generation", "ai.chat", "ui");
        generationCts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        if (disposed || SelectedConversation is not AgentConversationViewModel conversation)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Clear chat", "ai.chat", "ui");
        conversation.Session = null;
        conversation.SerializedSessionState = null;
        conversation.UpdatedAt = DateTimeOffset.Now;
        Messages.Clear();
        RebuildHistorySummary(Messages);
        SaveConversation(conversation);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        Settings.PropertyChanged -= OnSettingsPropertyChanged;
        UnsubscribeModelProviderProfiles();
        UnsubscribeModelProfiles();

        generationCts?.Cancel();
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedConversationChanged(AgentConversationViewModel? value)
    {
        if (disposed)
        {
            return;
        }

        ApplyConversationProfile(value);
        RebuildActiveConversation(value);
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    private void LoadConversations()
    {
        foreach (AgentConversation conversation in conversationStore.LoadConversations().OrderByDescending(static item => item.UpdatedAt))
        {
            AgentConversationViewModel viewModel = AgentConversationViewModel.Create(conversation);
            RefreshConversationModelId(viewModel);
            Conversations.Add(viewModel);
        }

        if (Conversations.Count is 0)
        {
            Conversations.Add(CreateConversationCore());
        }

        SelectedConversation = Conversations[0];
    }

    private AgentConversationViewModel CreateConversationCore()
    {
        AgentConversationViewModel conversation = new()
        {
            CreatedAt = DateTimeOffset.Now,
            UpdatedAt = DateTimeOffset.Now,
        };

        ModelProviderProfile? providerProfile = Settings.ModelProviderProfiles.CurrentItem;
        ModelProfile? modelProfile = providerProfile?.ModelProfiles.CurrentItem;
        if (providerProfile is not null)
        {
            conversation.ModelProviderProfileId = providerProfile.Id;
            conversation.ProviderType = providerProfile.ProviderType.Value;
        }

        if (modelProfile is not null)
        {
            conversation.ModelProfileId = modelProfile.Id;
            conversation.ModelId = modelProfile.ModelId;
        }

        return conversation;
    }

    private ExtendedAgentOptions? CreateRequestOptions()
    {
        ModelProviderProfile? providerProfile = Settings.ModelProviderProfiles.CurrentItem;
        if (providerProfile is null)
        {
            return null;
        }

        ModelProfile? modelProfile = providerProfile.ModelProfiles.CurrentItem;
        if (modelProfile is null || string.IsNullOrWhiteSpace(modelProfile.ModelId))
        {
            return null;
        }

        return new()
        {
            ProviderType = providerProfile.ProviderType.Value,
            ModelId = modelProfile.ModelId.Trim(),
            Endpoint = providerProfile.Endpoint,
            ApiKey = providerProfile.ApiKey,
            Temperature = 0.3f,
            TopP = 0.95f,
            ThinkingEnabled = true,
            OmitReasoningEffortWhenThinkingDisabled = true,
        };
    }

    private async ValueTask<AgentSession> EnsureConversationSessionAsync(AgentConversationViewModel conversation, ExtendedAgentOptions requestOptions, CancellationToken cancellationToken)
    {
        if (conversation.Session is not null)
        {
            return conversation.Session;
        }

        if (string.IsNullOrWhiteSpace(requestOptions.ApiKey))
        {
            conversation.Session = ChatClientAgentSessionCreate();
            return conversation.Session;
        }

        if (conversation.SerializedSessionState is JsonElement serializedState)
        {
            conversation.Session = await agentService.DeserializeSessionAsync(requestOptions, serializedState, cancellationToken);
        }
        else
        {
            conversation.Session = await agentService.CreateSessionAsync(requestOptions, cancellationToken);
        }

        return conversation.Session;
    }

    private async ValueTask PersistConversationSessionAsync(AgentConversationViewModel conversation, ExtendedAgentOptions requestOptions, AgentSession session, CancellationToken cancellationToken)
    {
        JsonElement serializedState = await agentService.SerializeSessionAsync(requestOptions, session, cancellationToken);
        conversation.SerializedSessionState = serializedState.Clone();

        if (ReferenceEquals(SelectedConversation, conversation))
        {
            RebuildHistorySummary(Messages);
        }

        SaveConversation(conversation);
    }

    private void SaveConversation(AgentConversationViewModel conversation)
    {
        conversationStore.SaveConversation(conversation.ToData());
    }

    private void UpdateConversationProfile(AgentConversationViewModel conversation, ExtendedAgentOptions requestOptions)
    {
        ModelProviderProfile? providerProfile = Settings.ModelProviderProfiles.CurrentItem;
        ModelProfile? modelProfile = providerProfile?.ModelProfiles.CurrentItem;

        conversation.ModelProviderProfileId = providerProfile?.Id;
        conversation.ModelProfileId = modelProfile?.Id;
        conversation.ProviderType = requestOptions.ProviderType;
        conversation.ModelId = requestOptions.ModelId;
    }

    private void RefreshConversationModelId(AgentConversationViewModel conversation)
    {
        if (!conversation.ModelProviderProfileId.HasValue || !conversation.ModelProfileId.HasValue)
        {
            conversation.ModelId = string.Empty;
            return;
        }

        Guid providerProfileId = conversation.ModelProviderProfileId.Value;
        Guid modelProfileId = conversation.ModelProfileId.Value;
        ModelProviderProfile? providerProfile = Settings.ModelProviderProfiles.FirstOrDefault(item => item.Id == providerProfileId);
        ModelProfile? modelProfile = providerProfile?.ModelProfiles.FirstOrDefault(item => item.Id == modelProfileId);
        conversation.ModelId = modelProfile?.ModelId ?? string.Empty;
    }

    private void ApplyConversationProfile(AgentConversationViewModel? conversation)
    {
        if (conversation is null)
        {
            return;
        }

        isApplyingConversationSelection = true;
        try
        {
            if (conversation.ModelProviderProfileId.HasValue)
            {
                Settings.ModelProviderProfiles.CurrentItemId = conversation.ModelProviderProfileId;
            }

            if (conversation.ModelProfileId.HasValue)
            {
                Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles.MoveCurrentTo(conversation.ModelProfileId.Value);
            }
        }
        finally
        {
            isApplyingConversationSelection = false;
        }
    }

    private void RebuildActiveConversation(AgentConversationViewModel? conversation)
    {
        if (conversation is null)
        {
            Messages = [];
            RebuildHistorySummary([]);
            return;
        }

        Messages = conversation.Messages;
        RebuildHistorySummary(Messages);
    }

    private void RebuildHistorySummary(IEnumerable<ObservableChatMessage> messages)
    {
        HistorySummary = AgentHistorySummaryViewModel.Create(messages);
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(AppSettings.ModelProviderProfiles))
        {
            SubscribeModelProviderProfiles(Settings.ModelProviderProfiles);
            SubscribeModelProfiles(Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles);
        }
    }

    private void OnModelProviderProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(ObservableSettingsCollection<,>.CurrentItem) or nameof(ObservableSettingsCollection<,>.CurrentItemId))
        {
            SubscribeModelProfiles(Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles);
            ResetSelectedRuntimeSession();
        }
    }

    private void OnModelProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(ObservableSettingsCollection<,>.CurrentItem) or nameof(ObservableSettingsCollection<,>.CurrentItemId))
        {
            ResetSelectedRuntimeSession();
        }
    }

    private void SubscribeModelProviderProfiles(ObservableSettingsCollection<ModelProviderProfile, Guid> providerProfiles)
    {
        if (disposed)
        {
            return;
        }

        if (ReferenceEquals(modelProviderProfiles, providerProfiles))
        {
            return;
        }

        UnsubscribeModelProviderProfiles();
        modelProviderProfiles = providerProfiles;
        (modelProviderProfiles as INotifyPropertyChanged).PropertyChanged += OnModelProviderProfilesPropertyChanged;
    }

    private void UnsubscribeModelProviderProfiles()
    {
        (modelProviderProfiles as INotifyPropertyChanged)?.PropertyChanged -= OnModelProviderProfilesPropertyChanged;
        modelProviderProfiles = null;
    }

    private void SubscribeModelProfiles(ObservableSettingsCollection<ModelProfile, Guid>? profiles)
    {
        if (disposed || ReferenceEquals(modelProfiles, profiles))
        {
            return;
        }

        UnsubscribeModelProfiles();
        modelProfiles = profiles;
        if (modelProfiles is not null)
        {
            (modelProfiles as INotifyPropertyChanged).PropertyChanged += OnModelProfilesPropertyChanged;
        }
    }

    private void UnsubscribeModelProfiles()
    {
        (modelProfiles as INotifyPropertyChanged)?.PropertyChanged -= OnModelProfilesPropertyChanged;
        modelProfiles = null;
    }

    private void ResetSelectedRuntimeSession()
    {
        if (isApplyingConversationSelection)
        {
            return;
        }

        if (SelectedConversation is not null)
        {
            SelectedConversation.Session = null;
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

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern ChatClientAgentSession ChatClientAgentSessionCreate();
}
