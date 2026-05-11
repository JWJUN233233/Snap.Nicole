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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public OpenAIInMemoryChatHistoryProvider()
        : base(null, null, StoreInputResponseMessageFilter)
    {
        sessionState = new ProviderSessionState<State>((_ => new()), GetType().Name, null);
    }

    private readonly ProviderSessionState<State> sessionState;
    private IReadOnlyList<string>? stateKeys;

    public override IReadOnlyList<string> StateKeys => stateKeys ??= [sessionState.StateKey];

    protected override async ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        return await base.InvokingCoreAsync(context, cancellationToken);
    }

    protected override async ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        await base.InvokedCoreAsync(context, cancellationToken);
    }

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        State state = sessionState.GetOrInitializeState(context.Session);
        Debug.WriteLine($"Before ProvideChatHistoryAsync:\nRequest\n{JsonSerializer.Serialize(context.RequestMessages, JsonOptions)}\nMessages\n{JsonSerializer.Serialize(state.Messages, JsonOptions)}");
        return state.Messages;
    }

    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        State state = sessionState.GetOrInitializeState(context.Session);
        Debug.WriteLine($"Before StoreChatHistoryAsync:\nRequest\n{JsonSerializer.Serialize(context.RequestMessages, JsonOptions)}\nResponse\n{JsonSerializer.Serialize(context.ResponseMessages, JsonOptions)}\nMessages\n{JsonSerializer.Serialize(state.Messages, JsonOptions)}");
        List<ChatMessage> allNewMessages = [.. context.RequestMessages.Concat(context.ResponseMessages ?? [])];
        List<OpenAI.Chat.ChatMessage> openAIChatMessages = [.. OpenAI.Chat.MicrosoftExtensionsAIChatExtensions.AsOpenAIChatMessages(allNewMessages)];

        for (int i = 0; i < allNewMessages.Count; i++)
        {
            ChatMessage chatMessage = allNewMessages[i];
            if (chatMessage.Contents.Any(content => content is TextReasoningContent))
            {
                OpenAI.Chat.ChatMessage openAIChatMessage = openAIChatMessages[i];
                openAIChatMessage.Patch.Set("reasoning_content"u8, ((TextReasoningContent)chatMessage.Contents.Single(content => content is TextReasoningContent)).Text);
            }

            allNewMessages[i].RawRepresentation = chatMessage;
        }

        state.Messages.AddRange(allNewMessages);
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