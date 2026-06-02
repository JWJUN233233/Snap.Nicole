using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sentry;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Core.Text.Json;
using Snap.Nicole.Core.Threading;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal sealed class AgentService(IServiceProvider serviceProvider, [FromKeyedServices(JsonSerializerOptionsKey.AIFunctionContent)] JsonSerializerOptions functionContentJsonOptions) : IAgentService
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly JsonSerializerOptions functionContentJsonOptions = functionContentJsonOptions;

    public async ValueTask<AgentSession> CreateSessionAsync(ExtendedAgentOptions options, CancellationToken cancellationToken = default)
    {
        ChatClientAgent agent = options.CreateAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], serviceProvider);
        return await agent.CreateSessionAsync(cancellationToken);
    }

    public async ValueTask<AgentSession> DeserializeSessionAsync(ExtendedAgentOptions options, JsonElement serializedState, CancellationToken cancellationToken = default)
    {
        ChatClientAgent agent = options.CreateAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], serviceProvider);
        return await agent.DeserializeSessionAsync(serializedState, cancellationToken: cancellationToken);
    }

    public async ValueTask<JsonElement> SerializeSessionAsync(ExtendedAgentOptions options, AgentSession session, CancellationToken cancellationToken = default)
    {
        ChatClientAgent agent = options.CreateAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], serviceProvider);
        return await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken);
    }

    public async ValueTask<SpanStatus> RunStreamingAsync(ChatMessage message, ObservableChatMessageCollection collection, ExtendedAgentOptions options, AgentSession session, TaskScheduler taskScheduler, CancellationToken cancellationToken = default)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ai.chat.stream", "Run streaming chat completion");
        span.SetTag("ai.provider", options.ProviderType.ToString());
        span.SetTag("ai.model", options.ModelId);

        try
        {
            ObservableChatMessage inputMessage = ObservableChatMessage.Create(message, functionContentJsonOptions);
            await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, inputMessage, cancellationToken);

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                SentryDiagnostics.AddBreadcrumb("Chat blocked by missing API key", "ai.chat", "default");

                // Unfortunately, Text is not a dependency property, so we cannot localize this string here.
                ObservableChatMessage configurationMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, content: ObservableTextContent.Create(SR.UIXamlPagesAgentPageMessageConfigureApiKey));
                await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, configurationMessage, cancellationToken);
                span.Finish(SpanStatus.FailedPrecondition);
                return SpanStatus.FailedPrecondition;
            }

            ChatClientAgent agent = options.CreateAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], serviceProvider);

            ObservableChatMessage? responseMessage = null;
            bool responseAdded = false;

            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([message], session, options.AsAgentRunOptions(), cancellationToken))
            {
                List<ObservableAIContent> observableContents = [];
                foreach (AIContent content in update.Contents)
                {
                    if (ObservableAIContent.Create(content, functionContentJsonOptions) is { } observableContent)
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

            span.SetData("ai.response_added", responseAdded);
            return SpanStatus.Ok;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            span.Finish(SpanStatus.Cancelled);
            throw;
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ai.chat.stream");

            ObservableChatMessage errorMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, content: ObservableTextContent.Create($"Error: {ex.Message}"));
            await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, errorMessage, cancellationToken);
            return SpanStatus.InternalError;
        }
    }
}
