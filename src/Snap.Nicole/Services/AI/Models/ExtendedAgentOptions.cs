using Anthropic;
using Anthropic.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
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
    public ModelProviderType ProviderType { get; init; } = ModelProviderType.OpenAIChatCompletion;

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public string ModelId { get; init; } = string.Empty;

    public float? Temperature { get; init; }

    public float? TopP { get; init; }

    public ReasoningEffort? ReasoningEffort { get; init; }

    public bool? ThinkingEnabled { get; init; }

    public bool OmitReasoningEffortWhenThinkingDisabled { get; init; }

    public int? MaxInputTokens { get; init; }

    public int? MaxOutputTokens { get; init; }

    public string? SystemPrompt { get; init; }

    public static ExtendedAgentOptions Create(ModelProviderProfile providerProfile, ModelProfile modelProfile)
    {
        ModelProfileAgentOptions agentOptions = modelProfile.AgentOptions;
        return new()
        {
            ProviderType = providerProfile.ProviderType.Value,
            Endpoint = providerProfile.Endpoint,
            ApiKey = providerProfile.ApiKey,
            ModelId = modelProfile.ModelId.Trim(),
            Temperature = agentOptions.Temperature,
            TopP = agentOptions.TopP,
            ReasoningEffort = agentOptions.ReasoningEffort,
            ThinkingEnabled = agentOptions.ThinkingEnabled,
            OmitReasoningEffortWhenThinkingDisabled = agentOptions.OmitReasoningEffortWhenThinkingDisabled,
            MaxInputTokens = NormalizeTokenLimit(agentOptions.MaxInputTokens),
            MaxOutputTokens = NormalizeTokenLimit(agentOptions.MaxOutputTokens),
            SystemPrompt = NormalizeSystemPrompt(agentOptions.SystemPrompt),
        };
    }

    public ChatClientAgentRunOptions AsAgentRunOptions()
    {
        // Most models will ignore temperature and top_p when reasoning is enabled, but we set them here anyway.
        ChatOptions chatOptions = new()
        {
            ModelId = ModelId,
            Temperature = Temperature,
            TopP = TopP,
            MaxOutputTokens = MaxOutputTokens,
            ToolMode = ChatToolMode.Auto,
        };

        if (ReasoningEffort.HasValue && (ThinkingEnabled is not false || !OmitReasoningEffortWhenThinkingDisabled))
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

    public ChatHistoryProvider CreateChatHistoryProvider(IServiceProvider serviceProvider)
    {
        return ProviderType switch
        {
            ModelProviderType.OpenAIChatCompletion => new RoundTripInMemoryChatHistoryProvider(serviceProvider.GetRequiredService<ObjectPool<StringBuilder>>()),
            ModelProviderType.OpenAIResponses or ModelProviderType.Anthropic => new InMemoryChatHistoryProvider(),
            _ => throw new NotSupportedException($"Unsupported model provider type: {ProviderType}"),
        };
    }

    private ChatClientAgent CreateOpenAIChatCompletionAgent(IList<AITool>? tools, IServiceProvider serviceProvider)
    {
        string? thinkingEnabled = ThinkingEnabled.HasValue
            ? ThinkingEnabled.Value ? "enabled" : "disabled"
            : null;

        return CreateOpenAIClient(serviceProvider)
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
                ChatHistoryProvider = CreateChatHistoryProvider(serviceProvider),
                AIContextProviders = CreateAIContextProviders(serviceProvider),
                RequirePerServiceCallChatHistoryPersistence = true,
            }, loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());
    }

    private ChatClientAgent CreateOpenAIResponsesAgent(IList<AITool>? tools, IServiceProvider serviceProvider)
    {
        ResponsesClient client = CreateOpenAIClient().GetResponsesClient();
        return client.AsAIAgent(CreateAgentOptions(tools, CreateChatHistoryProvider(serviceProvider), serviceProvider), model: ModelId, loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());
    }

    private ChatClientAgent CreateAnthropicAgent(IList<AITool>? tools, IServiceProvider serviceProvider)
    {
        AnthropicClient client = CreateAnthropicClient();

        return client.AsAIAgent(CreateAgentOptions(tools, CreateChatHistoryProvider(serviceProvider), serviceProvider), loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());
    }

    private ChatClientAgentOptions CreateAgentOptions(IList<AITool>? tools, ChatHistoryProvider chatHistoryProvider, IServiceProvider serviceProvider)
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
            AIContextProviders = CreateAIContextProviders(serviceProvider),
            RequirePerServiceCallChatHistoryPersistence = true,
        };
    }

    private IEnumerable<AIContextProvider>? CreateAIContextProviders(IServiceProvider serviceProvider)
    {
        if (MaxInputTokens is not int maxInputTokens)
        {
            return null;
        }

        CompactionStrategy strategy = CreateContextWindowCompactionStrategy(CreateSummarizationChatClient(serviceProvider), maxInputTokens);
        return [new CompactionProvider(strategy, loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>())];
    }

    private IChatClient CreateSummarizationChatClient(IServiceProvider serviceProvider)
    {
        return ProviderType switch
        {
            ModelProviderType.OpenAIChatCompletion => CreateOpenAIClient(serviceProvider).GetChatClient(ModelId).AsIChatClient(),
            ModelProviderType.OpenAIResponses => CreateOpenAIClient().GetResponsesClient().AsIChatClient(ModelId),
            ModelProviderType.Anthropic => CreateAnthropicClient().AsIChatClient(ModelId, MaxOutputTokens),
            _ => throw new NotSupportedException($"Unsupported model provider type: {ProviderType}"),
        };
    }

    private static CompactionStrategy CreateContextWindowCompactionStrategy(IChatClient summarizationChatClient, int maxInputTokens)
    {
        CompactionTrigger trigger = CompactionTriggers.TokensExceed(maxInputTokens);
        CompactionStrategy[] strategies =
        [
            new SummarizationCompactionStrategy(summarizationChatClient, trigger),
            new TruncationCompactionStrategy(trigger, minimumPreservedGroups: 2),
        ];

        return new PipelineCompactionStrategy(strategies);
    }

    private OpenAIClient CreateOpenAIClient(IServiceProvider? serviceProvider = null)
    {
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = Endpoint.ToUri(),
        };

        if (serviceProvider is not null)
        {
            clientOptions.ClientLoggingOptions = new()
            {
                LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>(),
                EnableMessageContentLogging = true,
            };
        }

        return new(new ApiKeyCredential(ApiKey!), clientOptions);
    }

    private AnthropicClient CreateAnthropicClient()
    {
        return new(new ClientOptions
        {
            ApiKey = ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(Endpoint) ? EnvironmentUrl.Production : Endpoint,
        });
    }

    private static int? NormalizeTokenLimit(int? value)
    {
        return value is > 0 ? value : null;
    }

    private static string? NormalizeSystemPrompt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
