using Anthropic;
using Anthropic.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Snap.Nicole.Core;
using Snap.Nicole.ViewModels;
using System.ClientModel;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ExtendedAgentOptions
{
    // ModelProviderProfile
    public ModelProviderType ProviderType { get; init; } = ModelProviderType.OpenAIChatCompletion;

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public string ModelId { get; init; } = string.Empty;

    // Extended options
    // TODO: Create a separate class for these options and configure them in UI
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
            ModelId = ModelId,
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

    public ChatClientAgent CreateAIAgent(IList<AITool>? tools = default, ILoggerFactory? loggerFactory = default)
    {
        return ProviderType switch
        {
            ModelProviderType.OpenAIChatCompletion => CreateOpenAIChatCompletionAgent(tools, loggerFactory),
            ModelProviderType.OpenAIResponses => CreateOpenAIResponsesAgent(tools, loggerFactory),
            ModelProviderType.Anthropic => CreateAnthropicAgent(tools, loggerFactory),
            _ => throw new NotSupportedException($"Unsupported model provider type: {ProviderType}"),
        };
    }

    private ChatClientAgent CreateOpenAIChatCompletionAgent(IList<AITool>? tools, ILoggerFactory? loggerFactory)
    {
        string? thinkingEnabled = ThinkingEnabled.HasValue
            ? ThinkingEnabled.Value ? "enabled" : "disabled"
            : null;

        ChatClient client = CreateOpenAIClient().GetChatClient(ModelId);
        return client.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new()
            {
                RawRepresentationFactory = client =>
                {
                    ChatCompletionOptions options = new();
                    if (!string.IsNullOrEmpty(thinkingEnabled))
                    {
                        // "thinking": { "type": "enabled" } | "thinking": { "type": "disabled" }
                        options.Patch.Set("$.thinking.type"u8, thinkingEnabled);
                    }

                    return options;
                },
                Instructions = SystemPrompt,
                Tools = tools,
            },
            ChatHistoryProvider = new OpenAIChatCompletionInMemoryChatHistoryProvider(),
            RequirePerServiceCallChatHistoryPersistence = true,
        }, loggerFactory: loggerFactory);
    }

    private ChatClientAgent CreateOpenAIResponsesAgent(IList<AITool>? tools, ILoggerFactory? loggerFactory)
    {
        ResponsesClient client = CreateOpenAIClient().GetResponsesClient();
        return client.AsAIAgent(CreateAgentOptions(tools, new InMemoryChatHistoryProvider(null)), model: ModelId, loggerFactory: loggerFactory);
    }

    private ChatClientAgent CreateAnthropicAgent(IList<AITool>? tools, ILoggerFactory? loggerFactory)
    {
        AnthropicClient client = new(new ClientOptions
        {
            ApiKey = ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(Endpoint) ? EnvironmentUrl.Production : Endpoint,
        });

        return client.AsAIAgent(CreateAgentOptions(tools, new InMemoryChatHistoryProvider(null)), loggerFactory: loggerFactory);
    }

    private OpenAIClient CreateOpenAIClient()
    {
        return new(new ApiKeyCredential(ApiKey!), new OpenAIClientOptions()
        {
            Endpoint = Endpoint.ToUri(),
        });
    }

    private ChatClientAgentOptions CreateAgentOptions(IList<AITool>? tools, ChatHistoryProvider chatHistoryProvider)
    {
        return new()
        {
            ChatOptions = new()
            {
                ModelId = ModelId,
                Instructions = SystemPrompt,
                Tools = tools,
            },
            ChatHistoryProvider = chatHistoryProvider,
            RequirePerServiceCallChatHistoryPersistence = true,
        };
    }
}
