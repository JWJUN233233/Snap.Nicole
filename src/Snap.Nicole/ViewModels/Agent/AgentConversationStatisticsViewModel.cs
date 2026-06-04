using Snap.Nicole.Services.AI.Observables;
using System.Collections.Generic;

namespace Snap.Nicole.ViewModels.Agent;

internal sealed class AgentConversationStatisticsViewModel
{
    public int MessageCount { get; set; }

    public int ContentCount { get; set; }

    public int TextContentCount { get; set; }

    public int ReasoningContentCount { get; set; }

    public int ToolCallCount { get; set; }

    public int ToolResultCount { get; set; }

    public long TotalTokenCount { get; set; }

    public long InputTokenCount { get; set; }

    public long OutputTokenCount { get; set; }

    public long ReasoningTokenCount { get; set; }

    public long CachedInputTokenCount { get; set; }

    public static AgentConversationStatisticsViewModel Create(IEnumerable<ObservableChatMessage> messages)
    {
        AgentConversationStatisticsViewModel statistics = new();

        foreach (ObservableChatMessage message in messages)
        {
            statistics.MessageCount++;
            foreach (ObservableAIContent content in message.Contents)
            {
                statistics.ContentCount++;
                switch (content)
                {
                    case ObservableTextContent:
                        statistics.TextContentCount++;
                        break;
                    case ObservableTextReasoningContent:
                        statistics.ReasoningContentCount++;
                        break;
                    case ObservableToolCallContent:
                        statistics.ToolCallCount++;
                        break;
                    case ObservableToolResultContent:
                        statistics.ToolResultCount++;
                        break;
                    case ObservableUsageContent usageContent:
                        statistics.AddUsage(usageContent);
                        break;
                }
            }
        }

        return statistics;
    }

    private void AddUsage(ObservableUsageContent usageContent)
    {
        TotalTokenCount = AddCounts(TotalTokenCount, usageContent.TotalTokenCount);
        InputTokenCount = AddCounts(InputTokenCount, usageContent.InputTokenCount);
        OutputTokenCount = AddCounts(OutputTokenCount, usageContent.OutputTokenCount);
        ReasoningTokenCount = AddCounts(ReasoningTokenCount, usageContent.ReasoningTokenCount);
        CachedInputTokenCount = AddCounts(CachedInputTokenCount, usageContent.CachedInputTokenCount);
    }

    private static long AddCounts(long current, long? value)
    {
        if (value is not long count || count <= 0)
        {
            return current;
        }

        return current + count;
    }
}
