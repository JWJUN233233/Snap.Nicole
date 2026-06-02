using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Agents.AI;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Nicole.ViewModels;

internal sealed partial class AgentConversationViewModel : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public AgentConversationViewModel()
    {
        Messages.CollectionChanged += OnMessagesCollectionChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleDisplay))]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreatedAtDisplay))]
    public partial DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdatedAtDisplay))]
    public partial DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial Guid? ModelProviderProfileId { get; set; }

    [ObservableProperty]
    public partial Guid? ModelProfileId { get; set; }

    [ObservableProperty]
    public partial ModelProviderType ProviderType { get; set; }

    [ObservableProperty]
    public partial string ModelId { get; set; } = string.Empty;

    [JsonIgnore]
    public ChatClientAgent? Agent { get; set; }

    [JsonIgnore]
    public ExtendedAgentOptions? AgentOptions { get; set; }

    [JsonIgnore]
    public AgentSession? Session { get; set; }

    [JsonIgnore]
    public JsonElement? SerializedSessionState { get; set; }

    public ObservableChatMessageCollection Messages { get; } = [];

    public int MessageCount
    {
        get
        {
            return Messages.Count;
        }
    }

    public string TitleDisplay
    {
        get
        {
            return string.IsNullOrWhiteSpace(Title) ? SR.UIXamlPagesAgentPageLabelNewConversation : Title;
        }
    }

    public string CreatedAtDisplay
    {
        get
        {
            return CreatedAt.ToString("G");
        }
    }

    public string UpdatedAtDisplay
    {
        get
        {
            return UpdatedAt.ToString("G");
        }
    }

    public void SetMessages(IEnumerable<ObservableChatMessage> messages)
    {
        Messages.Clear();
        foreach (ObservableChatMessage message in messages)
        {
            Messages.Add(message);
        }
    }

    public AgentConversation ToData()
    {
        return new()
        {
            Id = Id,
            Title = Title,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            ModelProviderProfileId = ModelProviderProfileId,
            ModelProfileId = ModelProfileId,
            ProviderType = ProviderType,
            SerializedSessionState = SerializedSessionState?.Clone(),
            Messages = [.. Messages],
        };
    }

    public static AgentConversationViewModel Create(AgentConversation data)
    {
        AgentConversationViewModel viewModel = new()
        {
            Id = data.Id,
            Title = data.Title,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            ModelProviderProfileId = data.ModelProviderProfileId,
            ModelProfileId = data.ModelProfileId,
            ProviderType = data.ProviderType,
            SerializedSessionState = data.SerializedSessionState?.Clone(),
        };

        if (data.Messages is { Count: > 0 } messages)
        {
            viewModel.SetMessages(messages);
        }
        return viewModel;
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MessageCount));
    }
}
