using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.Threading;

namespace Snap.Nicole.Services.AI;

internal interface IChatService
{
    IAsyncEnumerable<ExtendedAgentResponseUpdate> StreamCompletionAsync(
        IReadOnlyList<ExtendedAgentResponseUpdate> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken = default);
}
