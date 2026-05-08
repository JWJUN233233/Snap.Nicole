using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using Snap.Nicole.Core;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

namespace Snap.Nicole.Services.AI;

internal sealed class OpenAIChatService(IServiceProvider serviceProvider) : IChatService
{
    private readonly IOptionsMonitor<AppSettings> settings = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();

    public async IAsyncEnumerable<ExtendedAgentResponseUpdate> StreamCompletionAsync(
        IReadOnlyList<ExtendedAgentResponseUpdate> messages,
        ChatCompletionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AppSettings current = settings.CurrentValue;
        if (string.IsNullOrWhiteSpace(current.OpenAIApiKey))
        {
            yield return new()
            {
                RoleKind = ChatRoleKind.Assistant,
                Content = "Please configure your OpenAI API key in Settings.",
            };

            yield break;
        }

        AIAgent agent = CreateAgent(current, options);
        List<ChatMessage> messageList = [];

        if (!string.IsNullOrWhiteSpace(options.SystemPrompt))
        {
            messageList.Add(new ChatMessage(ChatRole.System, options.SystemPrompt));
        }

        foreach (ExtendedAgentResponseUpdate msg in messages)
        {
            string messageContent = GetMessageContent(msg);
            switch (msg.RoleKind)
            {
                case ChatRoleKind.System:
                    messageList.Add(new(ChatRole.System, messageContent));
                    break;
                case ChatRoleKind.User:
                    messageList.Add(new(ChatRole.User, messageContent));
                    break;
                case ChatRoleKind.Assistant:
                    messageList.Add(new(ChatRole.Assistant, messageContent));
                    break;
                case ChatRoleKind.Tool:
                    messageList.Add(new(ChatRole.Tool, messageContent));
                    break;
            }
        }

        string contentBuffer = "";
        List<ExtendedAgentContentSegment> segments = [];
        ExtendedAgentContentKind? previousKind = null;

        AgentRunOptions runOptions = CreateOptions(options);

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messageList, options: runOptions, cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool hasChanges = false;
            foreach (AIContent aiContent in update.Contents)
            {
                ExtendedAgentContentKind? kind = null;
                string text = "";

                switch (aiContent)
                {
                    case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                        kind = ExtendedAgentContentKind.Text;
                        text = textContent.Text;
                        contentBuffer += text;
                        break;
                    case TextReasoningContent reasoningContent when !string.IsNullOrEmpty(reasoningContent.Text):
                        kind = ExtendedAgentContentKind.Reasoning;
                        text = reasoningContent.Text;
                        break;
                    case FunctionCallContent functionCallContent:
                        kind = ExtendedAgentContentKind.ToolCall;
                        text = $"{functionCallContent.Name}: {JsonSerializer.Serialize(functionCallContent.Arguments)}";
                        break;
                    case FunctionResultContent functionResultContent:
                        kind = ExtendedAgentContentKind.ToolResult;
                        text = JsonSerializer.Serialize(functionResultContent.Result);
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
                    };
                }
                else
                {
                    segments.Add(new ExtendedAgentContentSegment
                    {
                        Kind = kind.Value,
                        Content = text,
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
                Segments = [.. segments],
            };
        }
    }

    private static string GetMessageContent(ExtendedAgentResponseUpdate message)
    {
        if (message.Segments.Count == 0)
        {
            return message.Content;
        }

        return string.Concat(message.Segments.Select(static segment => segment.Content));
    }

    private static ChatClientAgent CreateAgent(AppSettings settings, ChatCompletionOptions options)
    {
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = settings.OpenAIBaseUrl.ToUri(),
        };

        OpenAIClient client = new(new ApiKeyCredential(settings.OpenAIApiKey!), clientOptions);

        return OpenAI.Chat.OpenAIChatClientExtensions.AsAIAgent(client.GetChatClient(options.Model),
            instructions: options.SystemPrompt,
            tools: [AIFunctionFactory.Create(GetCurrentTime)]);
    }

    private static ChatClientAgentRunOptions CreateOptions(ChatCompletionOptions options)
    {
        return new(new()
        {
            Temperature = options.Temperature,
            TopP = options.TopP,
            ToolMode = ChatToolMode.Auto,
            Reasoning = new()
            {
                Effort = options.ReasoningEffort,
            }
        });
    }

    [Description("Get the current local time.")]
    private static string GetCurrentTime()
    {
        return DateTimeOffset.Now.ToString("O");
    }
}
