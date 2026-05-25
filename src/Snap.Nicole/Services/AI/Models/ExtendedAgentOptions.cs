using Anthropic;
using Anthropic.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Snap.Nicole.Core;
using Snap.Nicole.Services.AI.Compatibility.OpenAIChatCompletion;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;

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

    public ChatClientAgent CreateAIAgent(IList<AITool>? tools, IServiceProvider serviceProvider)
    {
        return ProviderType switch
        {
            ModelProviderType.OpenAIChatCompletion => CreateOpenAIChatCompletionAgent(tools, serviceProvider),
            ModelProviderType.OpenAIResponses => CreateOpenAIResponsesAgent(tools, serviceProvider),
            ModelProviderType.Anthropic => CreateAnthropicAgent(tools, serviceProvider),
            _ => throw new NotSupportedException($"Unsupported model provider type: {ProviderType}"),
        };
    }

    private ChatClientAgent CreateOpenAIChatCompletionAgent(IList<AITool>? tools, IServiceProvider serviceProvider)
    {
        string? thinkingEnabled = ThinkingEnabled.HasValue
            ? ThinkingEnabled.Value ? "enabled" : "disabled"
            : null;

        ApiKeyCredential apiKeyCredential = new(ApiKey!);
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = Endpoint.ToUri(),
            ClientLoggingOptions = new()
            {
                LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>(),
                EnableMessageContentLogging = true,
            },
        };

        return new OpenAIClient(apiKeyCredential, clientOptions)
            .GetChatClient(ModelId)
            .AsIChatClient()
            .AsBuilder()
            .Use(innerClient => new UsageContentRectifyDelegatingChatClient(innerClient))
            .BuildAIAgent(new ChatClientAgentOptions
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
                ChatHistoryProvider = new RoundTripInMemoryChatHistoryProvider(serviceProvider.GetRequiredService<ObjectPool<StringBuilder>>()),
                RequirePerServiceCallChatHistoryPersistence = true,
            }, loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());
    }

    private ChatClientAgent CreateOpenAIResponsesAgent(IList<AITool>? tools, IServiceProvider serviceProvider)
    {
        ResponsesClient client = new OpenAIClient(new ApiKeyCredential(ApiKey!), new OpenAIClientOptions()
        {
            Endpoint = Endpoint.ToUri(),
        }).GetResponsesClient();
        return client.AsAIAgent(CreateAgentOptions(tools, new InMemoryChatHistoryProvider(null)), model: ModelId, loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());
    }

    private ChatClientAgent CreateAnthropicAgent(IList<AITool>? tools, IServiceProvider serviceProvider)
    {
        AnthropicClient client = new(new ClientOptions
        {
            ApiKey = ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(Endpoint) ? EnvironmentUrl.Production : Endpoint,
        });

        return client.AsAIAgent(CreateAgentOptions(tools, new InMemoryChatHistoryProvider(null)), loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());
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