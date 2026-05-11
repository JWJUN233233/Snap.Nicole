using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Agents.AI;

namespace Snap.Nicole.Services.AI;

internal interface IAgentService
{
    AgentSession CreateSession(ExtendedAgentOptions options);

    IAsyncEnumerable<ExtendedAgentResponseUpdate> RunStreamingAsync(
        ExtendedAgentResponseUpdate message,
        ExtendedAgentOptions options,
        AgentSession session,
        CancellationToken cancellationToken = default);
}
