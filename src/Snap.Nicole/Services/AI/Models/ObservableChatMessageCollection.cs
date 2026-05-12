using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ObservableChatMessageCollection : Collection<ObservableChatMessage>, INotifyCollectionChanged, INotifyPropertyChanged
{
    public ObservableChatMessageCollection()
    {
        
    }
}

[DebuggerDisplay("[{Role}] {ContentForDebuggerDisplay}{EllipsesForDebuggerDisplay,nq}")]
internal sealed class ObservableChatMessage : INotifyPropertyChanged
{
    public string? AuthorName { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public ChatRole Role { get; set; }

    public ObservableAIContentCollection Contents { get; set; }

    public string? MessageId { get; set; }

    public object? RawRepresentation { get; set; }
}

internal sealed class ObservableAIContentCollection : Collection<ObservableAIContent>, INotifyCollectionChanged, INotifyPropertyChanged
{
}

internal class ObservableAIContent : ObservableObject
{
    public object? RawRepresentation { get; set; }
}

internal sealed class ObservableDataContent : ObservableAIContent;

internal sealed class ObservableErrorContent : ObservableAIContent
{
    public string Message { get; set; }

    public string? ErrorCode { get; set; }

    public string? Details { get; set; }
}

internal sealed class ObservableFunctionCallContent : ObservableToolCallContent
{
    public string Name { get; set; }

    public IDictionary<string, object?>? Arguments { get; set; }

    public Exception? Exception { get; set; }

    public bool InformationalOnly { get; set; }
}

internal sealed class ObservableFunctionResultContent : ObservableToolResultContent
{
    public object? Result { get; set; }

    public Exception? Exception { get; set; }
}

internal sealed class ObservableHostedFileContent : ObservableAIContent;

internal sealed class ObservableHostedVectorStoreContent : ObservableAIContent;

internal sealed class ObservableTextContent : ObservableAIContent
{
    public string Text { get; set; }
}

internal sealed class ObservableTextReasoningContent : ObservableAIContent
{
    public string Text { get; set; }
}

internal sealed class ObservableUriContent : ObservableAIContent
{
    public Uri Uri { get; set; }

    public string MediaType { get; set; }
}

internal sealed class ObservableUsageContent : ObservableAIContent
{
    public UsageDetails Details { get; set; }
}

internal class ObservableToolCallContent : ObservableAIContent
{
    public string CallId { get; set; }
}

internal class ObservableToolResultContent : ObservableAIContent
{
    public string CallId { get; set; }
}

internal class ObservableInputRequestContent : ObservableAIContent
{
    public string RequestId { get; set; }
}

internal class ObservableInputResponseContent : ObservableAIContent
{
    public string RequestId { get; set; }
}

internal sealed class ObservableToolApprovalRequestContent : ObservableInputRequestContent
{
    public ObservableToolCallContent ToolCall { get; set; }
}

internal sealed class ObservableToolApprovalResponseContent : ObservableInputResponseContent
{
    public bool Approved { get; set; }

    public ObservableToolCallContent ToolCall { get; set; }

    public string? Reason { get; set; }
}

internal sealed class ObservableMcpServerToolCallContent : ObservableToolCallContent
{
    public string Name { get; set; }

    public string? ServerName { get; set; }

    public IDictionary<string, object?>? Arguments { get; set; }
}

internal sealed class ObservableMcpServerToolResultContent : ObservableToolResultContent
{
    public ObservableAIContentCollection? Outputs { get; set; }
}

internal sealed class ObservableImageGenerationToolCallContent : ObservableToolCallContent
{
}

internal sealed class ObservableImageGenerationToolResultContent : ObservableToolResultContent
{
    public ObservableAIContentCollection? Outputs { get; set; }
}

internal sealed class ObservableCodeInterpreterToolCallContent : ObservableToolCallContent
{
    public ObservableAIContentCollection? Inputs { get; set; }
}

internal sealed class ObservableCodeInterpreterToolResultContent : ObservableToolResultContent
{
    public ObservableAIContentCollection? Outputs { get; set; }
}

internal sealed class ObservableWebSearchToolCallContent : ObservableToolCallContent
{
    public ObservableCollection<string>? Queries { get; set; }
}

internal sealed class ObservableWebSearchToolResultContent : ObservableToolResultContent
{
    public ObservableAIContentCollection? Outputs { get; set; }
}