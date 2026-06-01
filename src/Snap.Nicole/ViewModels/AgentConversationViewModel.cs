using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Nicole.ViewModels;

internal sealed partial class AgentConversationViewModel : ObservableObject
{
    private readonly List<ChatMessage> historyMessages = [];

    public Guid Id { get; set; } = Guid.NewGuid();

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
    public partial string Endpoint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ModelId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int MessageCount { get; set; }

    public AgentSession? Session { get; set; }

    [JsonIgnore]
    public JsonElement? SerializedSessionState { get; set; }

    public IReadOnlyList<ChatMessage> HistoryMessages
    {
        get => historyMessages;
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

    public void SetHistoryMessages(IEnumerable<ChatMessage> messages)
    {
        historyMessages.Clear();
        historyMessages.AddRange(messages);
        MessageCount = historyMessages.Count;
    }

    public AgentConversationData ToData()
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
            Endpoint = Endpoint,
            ModelId = ModelId,
            MessageCount = MessageCount,
            SerializedSessionState = SerializedSessionState?.Clone(),
            HistoryMessages = [.. historyMessages],
        };
    }

    public static AgentConversationViewModel Create(AgentConversationData data)
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
            Endpoint = data.Endpoint,
            ModelId = data.ModelId,
            MessageCount = data.MessageCount,
            SerializedSessionState = data.SerializedSessionState?.Clone(),
        };

        viewModel.SetHistoryMessages(data.HistoryMessages);
        return viewModel;
    }
}
