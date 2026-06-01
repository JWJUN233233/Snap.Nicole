using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Text.Json;

namespace Snap.Nicole.ViewModels;

internal sealed class AgentHistoryContentViewModel
{
    public string Kind { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public static AgentHistoryContentViewModel Create(AIContent content)
    {
        return content switch
        {
            TextContent textContent => new AgentHistoryContentViewModel { Kind = nameof(TextContent), Summary = TrimForPreview(textContent.Text), Detail = textContent.Text },
            TextReasoningContent reasoningContent => new AgentHistoryContentViewModel { Kind = nameof(TextReasoningContent), Summary = TrimForPreview(reasoningContent.Text), Detail = reasoningContent.Text },
            FunctionCallContent functionCallContent => new AgentHistoryContentViewModel { Kind = nameof(FunctionCallContent), Summary = functionCallContent.Name, Detail = SerializeValue(functionCallContent) },
            FunctionResultContent functionResultContent => new AgentHistoryContentViewModel { Kind = nameof(FunctionResultContent), Summary = TrimForPreview(functionResultContent.Result?.ToString() ?? string.Empty), Detail = SerializeValue(functionResultContent) },
            UsageContent usageContent => new AgentHistoryContentViewModel { Kind = nameof(UsageContent), Summary = BuildUsageSummary(usageContent.Details), Detail = SerializeValue(usageContent) },
            _ => new AgentHistoryContentViewModel { Kind = content.GetType().Name, Summary = TrimForPreview(content.ToString() ?? string.Empty), Detail = SerializeValue(content) },
        };
    }

    private static string BuildUsageSummary(UsageDetails? details)
    {
        if (details is null)
        {
            return string.Empty;
        }

        List<string> parts = [];
        AddCount(parts, "total", details.TotalTokenCount);
        AddCount(parts, "input", details.InputTokenCount);
        AddCount(parts, "output", details.OutputTokenCount);
        AddCount(parts, "reasoning", details.ReasoningTokenCount);
        AddCount(parts, "cached", details.CachedInputTokenCount);
        return string.Join(", ", parts);
    }

    private static void AddCount(List<string> parts, string label, long? value)
    {
        if (value is long count && count > 0)
        {
            parts.Add($"{label}: {count}");
        }
    }

    private static string SerializeValue(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value, value.GetType(), AIJsonUtilities.DefaultOptions);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    private static string TrimForPreview(string value)
    {
        string normalized = value.ReplaceLineEndings(" ").Trim();
        const int maxLength = 120;
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..maxLength] + "...";
    }
}
