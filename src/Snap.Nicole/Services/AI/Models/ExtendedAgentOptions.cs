using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Snap.Nicole.Core;
using Snap.Nicole.ViewModels;
using System.ClientModel;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ExtendedAgentOptions
{
    public string Model { get; init; } = string.Empty;

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public float Temperature { get; init; } = 0.3f;

    public float TopP { get; init; } = 0.95f;

    public ReasoningEffort? ReasoningEffort { get; init; }

    public bool? ThinkingEnabled { get; init; }

    public bool OmitReasoningEffortWhenThinkingDisabled { get; init; }

    public string? SystemPrompt { get; init; } = """
        You are an interactive agent that helps users with software engineering tasks.
        """;

    public ChatClientAgentRunOptions AsAgentRunOptions()
    {
        ChatOptions chatOptions = new()
        {
            Temperature = Temperature,
            TopP = TopP,
            ToolMode = ChatToolMode.Auto,
        };

        if (!OmitReasoningEffortWhenThinkingDisabled)
        {
            chatOptions.Reasoning = new()
            {
                Effort = ReasoningEffort,
            };
        }

        return new(chatOptions);
    }

    public ChatClientAgent AsAIAgent(IList<AITool>? tools = default, ILoggerFactory? loggerFactory = default)
    {
        OpenAIClient client = new(new ApiKeyCredential(ApiKey!), new OpenAIClientOptions()
        {
            Endpoint = Endpoint.ToUri(),
        });

        string? thinkingEnabled = ThinkingEnabled.HasValue
            ? ThinkingEnabled.Value ? "enabled" : "disabled"
            : null;

        return client.GetChatClient(Model).AsIChatClient().AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new()
            {
                RawRepresentationFactory = client =>
                {
                    ChatCompletionOptions options = new();
                    if (!string.IsNullOrEmpty(thinkingEnabled))
                    {
                        options.Patch.Set("$.thinking.type"u8, thinkingEnabled);
                    }

                    return options;
                },
                Instructions = SystemPrompt,
                Tools = tools,
            },
            ChatHistoryProvider = new OpenAIInMemoryChatHistoryProvider(),
            RequirePerServiceCallChatHistoryPersistence = true,
        }, loggerFactory: loggerFactory);
    }
}
