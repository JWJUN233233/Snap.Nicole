using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using System.Threading;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal interface IAgentService
{
    ValueTask RunStreamingAsync(
        ChatMessage message,
        ObservableChatMessageCollection collection,
        ExtendedAgentOptions options,
        AgentSession session,
        TaskScheduler taskScheduler,
        CancellationToken cancellationToken = default);
}
