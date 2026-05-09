using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.Threading;

namespace Snap.Nicole.Services.AI;

internal interface IAgentService
{
    IAsyncEnumerable<ExtendedAgentResponseUpdate> RunStreamingAsync(
        IReadOnlyList<ExtendedAgentResponseUpdate> messages,
        ExtendedAgentOptions options,
        CancellationToken cancellationToken = default);
}
