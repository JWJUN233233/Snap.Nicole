using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;

namespace Snap.Nicole.ViewModels;

internal sealed class AgentHistorySummaryViewModel
{
    public int MessageCount { get; set; }

    public int ContentCount { get; set; }

    public int TextContentCount { get; set; }

    public int ReasoningContentCount { get; set; }

    public int ToolCallCount { get; set; }

    public int ToolResultCount { get; set; }

    public long? TotalTokenCount { get; set; }

    public long? InputTokenCount { get; set; }

    public long? OutputTokenCount { get; set; }

    public long? ReasoningTokenCount { get; set; }

    public long? CachedInputTokenCount { get; set; }

    public static AgentHistorySummaryViewModel Create(IEnumerable<ChatMessage> messages)
    {
        AgentHistorySummaryViewModel summary = new()
        {
            MessageCount = messages.Count(),
        };

        foreach (ChatMessage message in messages)
        {
            foreach (AIContent content in message.Contents)
            {
                summary.ContentCount++;
                switch (content)
                {
                    case TextContent:
                        summary.TextContentCount++;
                        break;
                    case TextReasoningContent:
                        summary.ReasoningContentCount++;
                        break;
                    case ToolCallContent:
                        summary.ToolCallCount++;
                        break;
                    case ToolResultContent:
                        summary.ToolResultCount++;
                        break;
                    case UsageContent usageContent:
                        summary.AddUsage(usageContent.Details);
                        break;
                }
            }
        }

        return summary;
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
        CachedInputTokenCount = AddCounts(CachedInputTokenCount, details.CachedInputTokenCount);
    }

    private static long? AddCounts(long? current, long? value)
    {
        if (value is not long count || count <= 0)
        {
            return current;
        }

        return (current ?? 0) + count;
    }
}
