using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.ObjectPool;
using Snap.Nicole.Core.ObjectPool;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI.Compatibility.OpenAIChatCompletion;

// Specially designed ChatHistoryProvider to make sure that:
// 1. 'reasoning_content' can be round-tripped correctly in the chat history
// 2. 'content' can be round-tripped correctly in the chat history
internal sealed class RoundTripInMemoryChatHistoryProvider : ChatHistoryProvider
{
    private readonly ObjectPool<StringBuilder> stringBuilderPool;

    public RoundTripInMemoryChatHistoryProvider(ObjectPool<StringBuilder> stringBuilderPool)
        : base(null, null, StoreInputResponseMessageFilter)
    {
        this.stringBuilderPool = stringBuilderPool;
        sessionState = new ProviderSessionState<State>((_ => new()), GetType().Name, null);
    }

    private readonly ProviderSessionState<State> sessionState;
    private IReadOnlyList<string>? stateKeys;

    public override IReadOnlyList<string> StateKeys { get => stateKeys ??= [sessionState.StateKey]; }

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        State state = sessionState.GetOrInitializeState(context.Session);
        return state.Messages;
    }

    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        State state = sessionState.GetOrInitializeState(context.Session);

        List<ChatMessage> responseMessages = context.ResponseMessages is null ? [] : [.. context.ResponseMessages];
        if (responseMessages.Count > 0)
        {
            // Raw representations should be the same count as response chat messages
            IReadOnlyList<OpenAI.Chat.ChatMessage> rawRepresentations = [.. OpenAI.Chat.MicrosoftExtensionsAIChatExtensions.AsOpenAIChatMessages(responseMessages)];

            for (int i = 0; i < responseMessages.Count; i++)
            {
                ChatMessage responseMessage = responseMessages[i];

                using ObjectPoolLease<StringBuilder> reasoningContentBuilderLease = stringBuilderPool.Rent();
                foreach (AIContent content in responseMessage.Contents)
                {
                    if (content is TextReasoningContent reasoningContent)
                    {
                        reasoningContentBuilderLease.Value.Append(reasoningContent.Text);
                    }
                }

                OpenAI.Chat.ChatMessage rawRepresentation = rawRepresentations[i];
                rawRepresentation.Patch.Set("$.reasoning_content"u8, reasoningContentBuilderLease.Value.ToString());
                responseMessage.RawRepresentation = rawRepresentation;
            }

            foreach (OpenAI.Chat.ChatMessage rawRepresentation in rawRepresentations)
            {
                if (rawRepresentation is OpenAI.Chat.AssistantChatMessage { Content.Count: > 1 } assistantChatMessage)
                {
                    using ObjectPoolLease<StringBuilder> contentBuilderLease = stringBuilderPool.Rent();
                    foreach (OpenAI.Chat.ChatMessageContentPart part in assistantChatMessage.Content)
                    {
                        contentBuilderLease.Value.Append(part.Text);
                    }

                    assistantChatMessage.Content.Clear();
                    assistantChatMessage.Content.Add(OpenAI.Chat.ChatMessageContentPart.CreateTextPart(contentBuilderLease.Value.ToString()));
                }
            }
        }

        state.Messages.AddRange(context.RequestMessages.Concat(responseMessages));
    }

    private static IEnumerable<ChatMessage> StoreInputResponseMessageFilter(IEnumerable<ChatMessage> responses)
    {
        foreach (ChatMessage response in responses)
        {
            ChatMessage newResponse = response.Clone();
            newResponse.Contents = [];

            foreach (AIContent content in response.Contents)
            {
                if (content is TextContent text && string.IsNullOrEmpty(text.Text))
                {
                    continue;
                }

                newResponse.Contents.Add(content);
            }

            yield return newResponse;
        }
    }

    public sealed class State
    {
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
