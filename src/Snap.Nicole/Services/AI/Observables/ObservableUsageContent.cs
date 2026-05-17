using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Globalization;

namespace Snap.Nicole.Services.AI.Observables;

// TODO: separate text into multiple properties for better UI binding and formatting control
internal sealed partial class ObservableUsageContent : ObservableAIContent
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Text))]
    public partial UsageDetails? Details { get; set; }

    public string Text { get => FormatDetails(Details); }

    private static string FormatDetails(UsageDetails? details)
    {
        if (details is null)
        {
            return string.Empty;
        }

        List<string> parts = [];

        AppendCount(parts, "Total", details.TotalTokenCount);
        AppendCount(parts, "Input", details.InputTokenCount);
        AppendCount(parts, "Input Audio", details.InputAudioTokenCount);
        AppendCount(parts, "Input Text", details.InputTextTokenCount);
        AppendCount(parts, "Reasoning", details.ReasoningTokenCount);
        AppendCount(parts, "Output", details.OutputTokenCount);
        AppendCount(parts, "Output Audio", details.OutputAudioTokenCount);
        AppendCount(parts, "Output Text", details.OutputTextTokenCount);
        AppendCount(parts, "Cached Input", details.CachedInputTokenCount);

        if (details.AdditionalCounts is not null)
        {
            foreach (KeyValuePair<string, long> count in details.AdditionalCounts)
            {
                AppendCount(parts, count.Key, count.Value);
            }
        }

        return string.Join(" | ", parts);
    }

    private static void AppendCount(List<string> parts, string label, long? value)
    {
        if (value is not long count || count == 0)
        {
            return;
        }

        parts.Add($"{label}: {count.ToString("N0", CultureInfo.CurrentCulture)}");
    }
}
