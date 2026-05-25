using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableChatMessage : ObservableObject
{
    [ObservableProperty]
    public partial string? AuthorName { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? CreatedAt { get; set; }

    [ObservableProperty]
    public partial ChatRole Role { get; set; }

    [ObservableProperty]
    public partial ObservableAIContentCollection Contents { get; set; } = new();

    [ObservableProperty]
    public partial string? MessageId { get; set; }

    public static ObservableChatMessage Create(ChatMessage chatMessage)
    {
        ObservableChatMessage observableChatMessage = new()
        {
            Role = chatMessage.Role,
            CreatedAt = chatMessage.CreatedAt,
            AuthorName = chatMessage.AuthorName,
            MessageId = chatMessage.MessageId,
        };

        foreach (AIContent content in chatMessage.Contents)
        {
            observableChatMessage.Contents.Append(ObservableAIContent.Create(content));
        }

        return observableChatMessage;
    }

    public static ObservableChatMessage Create(ChatRole role, DateTimeOffset? createdAt, string? authorName = default, ObservableAIContent? content = default)
    {
        ObservableChatMessage observableChatMessage = new()
        {
            Role = role,
            CreatedAt = createdAt,
            AuthorName = authorName,
        };

        if (content is not null)
        {
            observableChatMessage.Contents.Add(content);
        }

        return observableChatMessage;
    }
}
