using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableAIContentCollection : ObservableCollection<ObservableAIContent>
{
    public ObservableAIContentCollection()
        : base()
    {
    }

    public ObservableAIContentCollection(IEnumerable<ObservableAIContent> collection)
        : base(collection)
    {
    }

    public ObservableAIContentCollection(List<ObservableAIContent> list)
        : base(list)
    {
    }

    public void Append(ObservableAIContent? newContent)
    {
        if (newContent is null)
        {
            return;
        }

        if (Count is 0)
        {
            Add(newContent);
            return;
        }

        ObservableAIContent lastContent = Items[^1];
        if (lastContent is ObservableTextReasoningContent lastReasoningContent && newContent is not ObservableTextReasoningContent)
        {
            lastReasoningContent.IsCompleted = true;
        }

        switch (lastContent)
        {
            case ObservableTextContent lastText when newContent is ObservableTextContent newText:
                lastText.Text += newText.Text;
                return;
            case ObservableTextReasoningContent lastReasoning when newContent is ObservableTextReasoningContent newReasoning:
                lastReasoning.Text += newReasoning.Text;
                return;
            case ObservableFunctionCallContent lastFunctionCall when newContent is ObservableFunctionCallContent newFunctionCall && lastFunctionCall.CallId == newFunctionCall.CallId:
                lastFunctionCall.Name = newFunctionCall.Name;
                lastFunctionCall.Arguments = newFunctionCall.Arguments;
                return;
            case ObservableFunctionResultContent lastFunctionResult when newContent is ObservableFunctionResultContent newFunctionResult && lastFunctionResult.CallId == newFunctionResult.CallId:
                lastFunctionResult.Result = newFunctionResult.Result;
                return;
            case ObservableUsageContent lastUsage when newContent is ObservableUsageContent newUsage:
                lastUsage.Update(newUsage);
                return;
            default:
                Add(newContent);
                return;
        }
    }
}