using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Snap.Nicole.Core;
using Snap.Nicole.ViewModels;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;

namespace Snap.Nicole.Services.AI.Models;

internal sealed class ExtendedAgentOptions
{
    public string Model { get; init; } = string.Empty;

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public float Temperature { get; init; } = 0.3f;

    public float TopP { get; init; } = 0.95f;

    public ReasoningEffort? ReasoningEffort { get; init; }

    public string? SystemPrompt { get; init; }

    public ChatClientAgentRunOptions AsAgentRunOptions()
    {
        return new(new()
        {
            Temperature = Temperature,
            TopP = TopP,
            ToolMode = ChatToolMode.Auto,
            Reasoning = new()
            {
                Effort = ReasoningEffort,
            }
        });
    }

    public ChatClientAgent AsAIAgent(IList<AITool>? tools = default, ILoggerFactory? loggerFactory = default)
    {
        OpenAIClient client = new(new ApiKeyCredential(ApiKey!), new OpenAIClientOptions()
        {
            Endpoint = Endpoint.ToUri(),
        });

        return client.GetChatClient(Model).AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new()
            {
                Instructions = SystemPrompt,
                Tools = tools,
            },
            ChatHistoryProvider = new OpenAIInMemoryChatHistoryProvider(),
            RequirePerServiceCallChatHistoryPersistence = true,
        }, loggerFactory: loggerFactory);
    }
}
