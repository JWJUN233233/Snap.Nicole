using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal sealed class AgentService(IServiceProvider serviceProvider) : IAgentService
{
    private readonly ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    public AgentSession CreateSession(ExtendedAgentOptions options)
    {
        return options.AsAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], loggerFactory).CreateSessionAsync().AsTask().GetAwaiter().GetResult();
    }

    public async IAsyncEnumerable<ExtendedAgentResponseUpdate> RunStreamingAsync(
        ExtendedAgentResponseUpdate message,
        ExtendedAgentOptions options,
        AgentSession session,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            yield return new(ChatRoleKind.Assistant, SR.UIXamlPagesChatPageMessageConfigureApiKey);
            yield break;
        }

        ChatClientAgent agent = options.AsAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], loggerFactory);
        List<ChatMessage> inputMessages = BuildInputMessages(message);

        string contentBuffer = "";
        string reasoningBuffer = "";
        List<ExtendedAgentContentSegment> segments = [];
        ExtendedAIContentKind? previousKind = null;

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(inputMessages, session, options: options.AsAgentRunOptions(), cancellationToken: cancellationToken))
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

    public async ValueTask RunStreamingAsync(ChatMessage message, ObservableChatMessageCollection collection, ExtendedAgentOptions options, TaskScheduler taskScheduler, CancellationToken cancellationToken = default)
    {

    }

    private static List<ChatMessage> BuildInputMessages(ExtendedAgentResponseUpdate update)
    {
        List<ChatMessage> chatMessages = [];

        switch (update.RoleKind)
        {
            case ChatRoleKind.System:
                chatMessages.Add(new(ChatRole.System, update.Content));
                break;
            case ChatRoleKind.User:
                chatMessages.Add(new(ChatRole.User, update.Content));
                break;
        }

        return chatMessages;
    }
}
