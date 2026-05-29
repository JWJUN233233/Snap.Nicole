using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Snap.Nicole.Core.Threading;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal sealed class AgentService(IServiceProvider serviceProvider) : IAgentService
{
    private readonly IServiceProvider serviceProvider = serviceProvider;

    // TODO: preserve ChatHistory across multiple calls to RunStreamingAsync for the same conversation
    public async ValueTask RunStreamingAsync(ChatMessage message, ObservableChatMessageCollection collection, ExtendedAgentOptions options, AgentSession session, TaskScheduler taskScheduler, CancellationToken cancellationToken = default)
    {
        ObservableChatMessage inputMessage = ObservableChatMessage.Create(message);
        await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, inputMessage, cancellationToken);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            // Unfortunately, Text is not a dependency property, so we cannot localize this string here.
            ObservableChatMessage configurationMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, content: ObservableTextContent.Create(SR.UIXamlPagesChatPageMessageConfigureApiKey));
            await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, configurationMessage, cancellationToken);
            return;
        }

        ChatClientAgent agent = options.CreateAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], serviceProvider);

        ObservableChatMessage? responseMessage = null;
        bool responseAdded = false;

        try
        {
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([message], session, options.AsAgentRunOptions(), cancellationToken))
            {
                List<ObservableAIContent> observableContents = [];
                foreach (AIContent content in update.Contents)
                {
                    if (ObservableAIContent.Create(content) is { } observableContent)
                    {
                        observableContents.Add(observableContent);
                    }
                }

                // Fast path for updates that do not contain any content. This avoids unnecessary dispatching to the UI thread.
                if (observableContents.Count is 0)
                {
                    continue;
                }

                await taskScheduler.Run(() =>
                {
                    // TODOO: this should be possible to lift out of the UI thread dispatch,
                    // but currently if this is created on a background thread, the UI gets stuck and doesn't update at all.
                    responseMessage ??= ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, options.ModelId);

                    if (!responseAdded)
                    {
                        collection.Add(responseMessage);
                        responseAdded = true;
                    }

                    foreach (ObservableAIContent observableContent in observableContents)
                    {
                        responseMessage.Contents.Append(observableContent);
                    }
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ObservableChatMessage errorMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, content: ObservableTextContent.Create($"Error: {ex.Message}"));
            await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, errorMessage, cancellationToken);
        }
    }
}