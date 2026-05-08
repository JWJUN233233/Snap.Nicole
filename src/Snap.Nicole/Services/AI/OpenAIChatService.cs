using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Snap.Nicole.Services.Settings;
using System;
using System.Collections.Generic;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Snap.Nicole.Services.AI;

internal sealed class OpenAIChatService : IChatService
{
    private readonly IOptionsMonitor<AppSettings> settings;

    public OpenAIChatService(IServiceProvider serviceProvider)
    {
        settings = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
    }

    public async IAsyncEnumerable<Models.ChatMessage> StreamCompletionAsync(
        IReadOnlyList<Models.ChatMessage> messages,
        Models.ChatRequestOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AppSettings current = settings.CurrentValue;
        if (string.IsNullOrWhiteSpace(current.OpenAIApiKey))
        {
            yield return new Models.ChatMessage
            {
                Role = Models.ChatRole.Assistant,
                Content = "Please configure your OpenAI API key in Settings.",
            };
            yield break;
        }

        OpenAIClient client = CreateClient(current);
        ChatClient chatClient = client.GetChatClient(options.Model);

        List<ChatMessage> messageList = [];

        if (!string.IsNullOrWhiteSpace(options.SystemPrompt))
        {
            messageList.Add(ChatMessage.CreateSystemMessage(options.SystemPrompt));
        }

        foreach (Models.ChatMessage msg in messages)
        {
            switch (msg.Role)
            {
                case Models.ChatRole.System:
                    messageList.Add(ChatMessage.CreateSystemMessage(msg.Content));
                    break;
                case Models.ChatRole.User:
                    messageList.Add(ChatMessage.CreateUserMessage(msg.Content));
                    break;
                case Models.ChatRole.Assistant:
                    messageList.Add(ChatMessage.CreateAssistantMessage(msg.Content));
                    break;
                case Models.ChatRole.Tool:
                    if (!string.IsNullOrEmpty(msg.ToolCallId))
                    {
                        messageList.Add(ChatMessage.CreateToolMessage(msg.ToolCallId, msg.Content));
                    }
                    break;
            }
        }

        ChatCompletionOptions openAIOptions = new()
        {
            Temperature = options.Temperature,
        };

        AsyncCollectionResult<StreamingChatCompletionUpdate> streamingResult = chatClient.CompleteChatStreamingAsync(messageList, openAIOptions, cancellationToken);

        string contentBuffer = "";
        await foreach (StreamingChatCompletionUpdate update in streamingResult)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (ChatMessageContentPart part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    contentBuffer += part.Text;
                    yield return new Models.ChatMessage
                    {
                        Role = Models.ChatRole.Assistant,
                        Content = contentBuffer,
                    };
                }
            }
        }
    }

    private static OpenAIClient CreateClient(AppSettings settings)
    {
        OpenAIClientOptions clientOptions = new();

        if (!string.IsNullOrWhiteSpace(settings.OpenAIBaseUrl))
        {
            clientOptions.Endpoint = new Uri(settings.OpenAIBaseUrl);
        }

        return new OpenAIClient(new ApiKeyCredential(settings.OpenAIApiKey!), clientOptions);
    }
}
