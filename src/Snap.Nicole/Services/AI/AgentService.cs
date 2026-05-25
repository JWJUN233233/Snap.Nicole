using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    private readonly ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    // TODO: preserve AIAgent instance across multiple calls to RunStreamingAsync for the same conversation
    // TODO: preserve ChatHistory across multiple calls to RunStreamingAsync for the same conversation
    public async ValueTask RunStreamingAsync(ChatMessage message, ObservableChatMessageCollection collection, ExtendedAgentOptions options, AgentSession session, TaskScheduler taskScheduler, CancellationToken cancellationToken = default)
    {
        ObservableChatMessage inputMessage = ObservableChatMessage.Create(message);
        foreach (AIContent content in message.Contents)
        {
            inputMessage.Contents.Append(ObservableAIContent.Create(content));
        }

        await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, inputMessage, cancellationToken);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            // Unfortunately, Text is not a dependency property, so we cannot localize this string here.
            ObservableChatMessage configurationMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now);
            configurationMessage.Contents.Add(ObservableTextContent.Create(SR.UIXamlPagesChatPageMessageConfigureApiKey));
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

                // Fast path for updates that do not contain any content, which are common when the agent is thinking. This avoids unnecessary dispatching to the UI thread.
                if (observableContents.Count is 0)
                {
                    continue;
                }

                await taskScheduler.Run(() =>
                {
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
            ObservableChatMessage errorMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now);
            errorMessage.Contents.Add(ObservableTextContent.Create($"Error: {ex.Message}"));
            await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, errorMessage, cancellationToken);
        }
    }
}