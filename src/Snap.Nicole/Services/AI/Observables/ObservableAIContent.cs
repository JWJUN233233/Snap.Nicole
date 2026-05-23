using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Snap.Nicole.Services.AI.Observables;

internal class ObservableAIContent : ObservableObject
{
    public static ObservableAIContent? Create(AIContent content)
    {
        return content switch
        {
            TextContent textContent when !string.IsNullOrEmpty(textContent.Text) => ObservableTextContent.Create(textContent),
            TextReasoningContent reasoningContent when !string.IsNullOrEmpty(reasoningContent.Text) => ObservableTextReasoningContent.Create(reasoningContent),
            FunctionCallContent functionCallContent => ObservableFunctionCallContent.Create(functionCallContent),
            FunctionResultContent functionResultContent => ObservableFunctionResultContent.Create(functionResultContent),
            UsageContent usageContent => ObservableUsageContent.Create(usageContent),
            _ => null,
        };
    }
}
