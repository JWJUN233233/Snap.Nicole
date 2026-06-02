using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using System.Threading;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sentry;
using System.Text.Json;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal interface IAgentService
{
    ValueTask<AgentSession> CreateSessionAsync(ExtendedAgentOptions options, CancellationToken cancellationToken = default);

    ValueTask<AgentSession> DeserializeSessionAsync(ExtendedAgentOptions options, JsonElement serializedState, CancellationToken cancellationToken = default);

    ValueTask<JsonElement> SerializeSessionAsync(ExtendedAgentOptions options, AgentSession session, CancellationToken cancellationToken = default);

    ValueTask<SpanStatus> RunStreamingAsync(
        ChatMessage message,
        ObservableChatMessageCollection collection,
        ExtendedAgentOptions options,
        AgentSession session,
        TaskScheduler taskScheduler,
        CancellationToken cancellationToken = default);
}
