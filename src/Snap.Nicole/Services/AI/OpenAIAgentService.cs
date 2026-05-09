using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

namespace Snap.Nicole.Services.AI;

internal sealed class OpenAIAgentService(IServiceProvider serviceProvider) : IAgentService
{
    public async IAsyncEnumerable<ExtendedAgentResponseUpdate> RunStreamingAsync(
        IReadOnlyList<ExtendedAgentResponseUpdate> updates,
        ExtendedAgentOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            yield return new(ChatRoleKind.Assistant, SR.UIXamlPagesChatPageMessageConfigureApiKey);
            yield break;
        }

        List<ChatMessage> chatMessages = BuildChatMessages(updates, options.SystemPrompt);

        string contentBuffer = "";
        string reasoningBuffer = "";
        List<ExtendedAgentContentSegment> segments = [];
        ExtendedAIContentKind? previousKind = null;

        ChatClientAgent agent = options.AsAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)]);
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(chatMessages, options: options.AsAgentRunOptions(), cancellationToken: cancellationToken))
        {
            bool hasChanges = false;
            foreach (AIContent aiContent in update.Contents)
            {
                ExtendedAIContentKind? kind = null;
                string text = "";
                ToolCallMetadata? metadata = null;

                switch (aiContent)
                {
                    case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                        kind = ExtendedAIContentKind.Text;
                        text = textContent.Text;
                        contentBuffer += text;
                        break;
                    case TextReasoningContent reasoningContent when !string.IsNullOrEmpty(reasoningContent.Text):
                        kind = ExtendedAIContentKind.TextReasoning;
                        text = reasoningContent.Text;
                        reasoningBuffer += text;
                        break;
                    case FunctionCallContent functionCallContent:
                        kind = ExtendedAIContentKind.ToolCall;
                        text = $"{functionCallContent.Name}: {JsonSerializer.Serialize(functionCallContent.Arguments)}";
                        metadata = new()
                        {
                            CallId = functionCallContent.CallId,
                            Name = functionCallContent.Name,
                            Arguments = JsonSerializer.Serialize(functionCallContent.Arguments),
                        };
                        break;
                    case FunctionResultContent functionResultContent:
                        kind = ExtendedAIContentKind.ToolResult;
                        text = JsonSerializer.Serialize(functionResultContent.Result);
                        metadata = new()
                        {
                            CallId = functionResultContent.CallId,
                        };
                        break;
                }

                if (kind is null || string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (previousKind == kind && segments.Count > 0)
                {
                    ExtendedAgentContentSegment last = segments[^1];
                    segments[^1] = new ExtendedAgentContentSegment
                    {
                        Kind = last.Kind,
                        Content = last.Content + text,
                        Metadata = last.Metadata ?? metadata,
                    };
                }
                else
                {
                    segments.Add(new ExtendedAgentContentSegment
                    {
                        Kind = kind.Value,
                        Content = text,
                        Metadata = metadata,
                    });
                }

                previousKind = kind;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                continue;
            }

            yield return new()
            {
                RoleKind = ChatRoleKind.Assistant,
                Content = contentBuffer,
                ReasoningContent = reasoningBuffer,
                Segments = [.. segments],
            };
        }
    }

    private static List<ChatMessage> BuildChatMessages(IReadOnlyList<ExtendedAgentResponseUpdate> updates, string? systemPrompt)
    {
        List<ChatMessage> chatMessages = [];

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }

        foreach (ExtendedAgentResponseUpdate update in updates)
        {
            if (update.Segments.Count > 0)
            {
                BuildMessagesFromSegments(chatMessages, update);
            }
            else
            {
                switch (update.RoleKind)
                {
                    case ChatRoleKind.System:
                        chatMessages.Add(new(ChatRole.System, update.Content));
                        break;
                    case ChatRoleKind.User:
                        chatMessages.Add(new(ChatRole.User, update.Content));
                        break;
                    case ChatRoleKind.Assistant:
                        chatMessages.Add(new(ChatRole.Assistant, update.Content));
                        break;
                }
            }
        }

        return chatMessages;
    }

    private static void BuildMessagesFromSegments(List<ChatMessage> chatMessages, ExtendedAgentResponseUpdate update)
    {
        List<AIContent> assistantContents = [];

        foreach (ExtendedAgentContentSegment segment in update.Segments)
        {
            switch (segment.Kind)
            {
                case ExtendedAIContentKind.Text:
                    assistantContents.Add(new TextContent(segment.Content));
                    break;
                case ExtendedAIContentKind.TextReasoning:
                    assistantContents.Add(new TextReasoningContent(segment.Content));
                    break;
                case ExtendedAIContentKind.ToolCall:
                    if (segment.Metadata is not null)
                    {
                        assistantContents.Add(new FunctionCallContent(
                            segment.Metadata.CallId,
                            segment.Metadata.Name,
                            segment.Metadata.Arguments is not null
                                ? JsonSerializer.Deserialize<IDictionary<string, object?>>(segment.Metadata.Arguments)
                                : null));
                    }
                    break;
                case ExtendedAIContentKind.ToolResult:
                    chatMessages.Add(new ChatMessage(ChatRole.Assistant, [.. assistantContents]));
                    assistantContents.Clear();

                    string callId = segment.Metadata?.CallId ?? "";
                    object? result = null;
                    try
                    {
                        result = JsonSerializer.Deserialize<object>(segment.Content);
                    }
                    catch
                    {
                        result = segment.Content;
                    }
                    chatMessages.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(callId, result)]));
                    break;
            }
        }

        if (assistantContents.Count > 0)
        {
            chatMessages.Add(new ChatMessage(ChatRole.Assistant, [.. assistantContents]));
        }
    }
}
