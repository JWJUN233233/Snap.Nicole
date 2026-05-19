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
        return new()
        {
            Role = chatMessage.Role,
            CreatedAt = chatMessage.CreatedAt,
            AuthorName = chatMessage.AuthorName,
            MessageId = chatMessage.MessageId,
        };
    }

    public static ObservableChatMessage Create(ChatRole role, DateTimeOffset? createdAt, string? authorName = default)
    {
        return new ObservableChatMessage
        {
            Role = role,
            CreatedAt = createdAt,
            AuthorName = authorName,
        };
    }
}
