using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Snap.Nicole.Core.Threading;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal sealed class AgentService(IServiceProvider serviceProvider) : IAgentService
{
    private readonly ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    // TODO: perserve AIAgent instance across multiple calls to RunStreamingAsync for the same conversation
    // TODO: preserve ChatHistory across multiple calls to RunStreamingAsync for the same conversation
    public async ValueTask RunStreamingAsync(ChatMessage message, ObservableChatMessageCollection collection, ExtendedAgentOptions options, AgentSession session, TaskScheduler taskScheduler, CancellationToken cancellationToken = default)
    {
        ObservableChatMessage inputMessage = CreateObservableChatMessage(message);
        await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, inputMessage, cancellationToken);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            // Unfortunately, Text is not a dependency property, so we cannot localize this string here. 
            ObservableChatMessage configurationMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now);
            configurationMessage.Contents.Add(ObservableTextContent.Create(SR.UIXamlPagesChatPageMessageConfigureApiKey));
            await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, configurationMessage, cancellationToken);
            return;
        }

        ChatClientAgent agent = options.CreateAIAgent([AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)], loggerFactory);

        ObservableChatMessage? responseMessage = null;
        bool responseAdded = false;

        try
        {
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([message], session, options.AsAgentRunOptions(), cancellationToken))
            {
                List<ObservableAIContent> observableContents = [];
                foreach (AIContent content in update.Contents)
                {
                    ObservableAIContent? observableContent = CreateObservableContent(content);
                    if (observableContent is not null)
                    {
                        observableContents.Add(observableContent);
                    }
                }

                if (observableContents.Count == 0)
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
                        AppendContent(responseMessage.Contents, observableContent);
                    }
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ObservableChatMessage errorMessage = new()
            {
                Role = ChatRole.Assistant,
                CreatedAt = DateTimeOffset.Now,
            };
            errorMessage.Contents.Add(new ObservableTextContent { Text = $"Error: {ex.Message}" });

            await taskScheduler.Run(ObservableChatMessageCollection.Add, collection, errorMessage, cancellationToken);
        }
    }

    private static ObservableChatMessage CreateObservableChatMessage(ChatMessage chatMessage)
    {
        ObservableChatMessage observableMessage = new()
        {
            AuthorName = chatMessage.AuthorName,
            CreatedAt = chatMessage.CreatedAt,
            Role = chatMessage.Role,
            MessageId = chatMessage.MessageId,
            RawRepresentation = chatMessage.RawRepresentation,
        };

        foreach (AIContent content in chatMessage.Contents)
        {
            ObservableAIContent? observableContent = CreateObservableContent(content);
            if (observableContent is not null)
            {
                AppendContent(observableMessage.Contents, observableContent);
            }
        }

        return observableMessage;
    }

    private static ObservableAIContent? CreateObservableContent(AIContent content)
    {
        return content switch
        {
            TextContent textContent when !string.IsNullOrEmpty(textContent.Text) => ObservableTextContent.Create(textContent),
            TextReasoningContent reasoningContent when !string.IsNullOrEmpty(reasoningContent.Text) => ObservableTextReasoningContent.Create(reasoningContent),
            FunctionCallContent functionCallContent => ObservableFunctionCallContent.Create(functionCallContent),
            FunctionResultContent functionResultContent => ObservableFunctionResultContent.Create(functionResultContent),
            UsageContent usageContent => ObservableUsageContent.Create(usageContent),
            _ => null,
        };
    }

    private static void AppendContent(ObservableAIContentCollection contents, ObservableAIContent content)
    {
        if (contents.Count == 0)
        {
            contents.Add(content);
            return;
        }

        ObservableAIContent last = contents[^1];
        if (last is ObservableTextReasoningContent lastReasoningContent && content is not ObservableTextReasoningContent)
        {
            lastReasoningContent.IsCompleted = true;
        }

        switch (last)
        {
            case ObservableTextContent lastText when content is ObservableTextContent newText:
                lastText.Text += newText.Text;
                lastText.RawRepresentation = newText.RawRepresentation ?? lastText.RawRepresentation;
                return;
            case ObservableTextReasoningContent lastReasoning when content is ObservableTextReasoningContent newReasoning:
                lastReasoning.Text += newReasoning.Text;
                lastReasoning.RawRepresentation = newReasoning.RawRepresentation ?? lastReasoning.RawRepresentation;
                return;
            case ObservableFunctionCallContent lastFunctionCall when content is ObservableFunctionCallContent newFunctionCall && lastFunctionCall.CallId == newFunctionCall.CallId:
                lastFunctionCall.Name = newFunctionCall.Name;
                lastFunctionCall.Arguments = newFunctionCall.Arguments;
                lastFunctionCall.RawRepresentation = newFunctionCall.RawRepresentation ?? lastFunctionCall.RawRepresentation;
                return;
            case ObservableFunctionResultContent lastFunctionResult when content is ObservableFunctionResultContent newFunctionResult && lastFunctionResult.CallId == newFunctionResult.CallId:
                lastFunctionResult.Result = newFunctionResult.Result;
                lastFunctionResult.RawRepresentation = newFunctionResult.RawRepresentation ?? lastFunctionResult.RawRepresentation;
                return;
            case ObservableUsageContent lastUsage when content is ObservableUsageContent newUsage:
                lastUsage.Update(newUsage);
                return;
            default:
                contents.Add(content);
                return;
        }
    }
}
