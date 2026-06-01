using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Snap.Nicole.ViewModels;

internal sealed class AgentHistoryMessageViewModel
{
    public int Index { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? AuthorName { get; set; }

    public string? CreatedAt { get; set; }

    public string? MessageId { get; set; }

    public string Preview { get; set; } = string.Empty;

    public int ContentCount { get; set; }

    public long? TotalTokenCount { get; set; }

    public long? InputTokenCount { get; set; }

    public long? OutputTokenCount { get; set; }

    public long? ReasoningTokenCount { get; set; }

    public ObservableCollection<AgentHistoryContentViewModel> Contents { get; } = [];

    public static AgentHistoryMessageViewModel Create(int index, ChatMessage message)
    {
        AgentHistoryMessageViewModel viewModel = new()
        {
            Index = index,
            Role = message.Role.ToString(),
            AuthorName = message.AuthorName,
            CreatedAt = message.CreatedAt?.ToString("G"),
            MessageId = message.MessageId,
            Preview = BuildPreview(message),
            ContentCount = message.Contents.Count,
        };

        foreach (AIContent content in message.Contents)
        {
            viewModel.Contents.Add(AgentHistoryContentViewModel.Create(content));
            if (content is UsageContent usageContent)
            {
                viewModel.AddUsage(usageContent.Details);
            }
        }

        return viewModel;
    }

    private void AddUsage(UsageDetails? details)
    {
        if (details is null)
        {
            return;
        }

        TotalTokenCount = AddCounts(TotalTokenCount, details.TotalTokenCount);
        InputTokenCount = AddCounts(InputTokenCount, details.InputTokenCount);
        OutputTokenCount = AddCounts(OutputTokenCount, details.OutputTokenCount);
        ReasoningTokenCount = AddCounts(ReasoningTokenCount, details.ReasoningTokenCount);
    }

    private static long? AddCounts(long? current, long? value)
    {
        if (value is not long count || count <= 0)
        {
            return current;
        }

        return (current ?? 0) + count;
    }

    private static string BuildPreview(ChatMessage message)
    {
        IEnumerable<string> textParts = message.Contents.Select(static content =>
        {
            return content switch
            {
                TextContent textContent => textContent.Text,
                TextReasoningContent reasoningContent => reasoningContent.Text,
                FunctionCallContent functionCallContent => functionCallContent.Name,
                FunctionResultContent functionResultContent => functionResultContent.Result?.ToString() ?? string.Empty,
                UsageContent => nameof(UsageContent),
                _ => content.ToString() ?? string.Empty,
            };
        });

        string preview = string.Join(" ", textParts.Where(static part => !string.IsNullOrWhiteSpace(part))).ReplaceLineEndings(" ").Trim();
        const int maxLength = 96;
        if (preview.Length <= maxLength)
        {
            return preview;
        }

        return preview[..maxLength] + "...";
    }
}
