using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Globalization;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableUsageContent : ObservableAIContent
{
    private UsageDetails? details;

    public UsageDetails? Details
    {
        get => details;
        set
        {
            if (SetProperty(ref details, value))
            {
                OnPropertyChanged(nameof(Text));
            }
        }
    }

    public string Text => FormatDetails(Details);

    private static string FormatDetails(UsageDetails? details)
    {
        if (details is null)
        {
            return string.Empty;
        }

        List<string> parts = [];

        AppendCount(parts, "Input", details.InputTokenCount);
        AppendCount(parts, "Output", details.OutputTokenCount);
        AppendCount(parts, "Total", details.TotalTokenCount);
        AppendCount(parts, "Cached Input", details.CachedInputTokenCount);
        AppendCount(parts, "Reasoning", details.ReasoningTokenCount);
        AppendCount(parts, "Input Audio", details.InputAudioTokenCount);
        AppendCount(parts, "Input Text", details.InputTextTokenCount);
        AppendCount(parts, "Output Audio", details.OutputAudioTokenCount);
        AppendCount(parts, "Output Text", details.OutputTextTokenCount);

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
