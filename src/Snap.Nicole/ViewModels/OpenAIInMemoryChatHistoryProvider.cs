using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels;

internal sealed class OpenAIInMemoryChatHistoryProvider : ChatHistoryProvider
{
    public OpenAIInMemoryChatHistoryProvider()
        : base(null, null, StoreInputResponseMessageFilter)
    {
        sessionState = new ProviderSessionState<State>((_ => new()), GetType().Name, null);
    }

    private readonly ProviderSessionState<State> sessionState;
    private IReadOnlyList<string>? stateKeys;

    public override IReadOnlyList<string> StateKeys => stateKeys ??= [sessionState.StateKey];

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
                foreach (AIContent content in responseMessage.Contents)
                {
                    if (content is TextReasoningContent reasoningContent)
                    {
                        OpenAI.Chat.ChatMessage rawRepresentation = rawRepresentations[i];
                        rawRepresentation.Patch.Set("$.reasoning_content"u8, reasoningContent.Text);
                        responseMessage.RawRepresentation = rawRepresentation;

                        // There should only be one reasoning content per message
                        break;
                    }
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