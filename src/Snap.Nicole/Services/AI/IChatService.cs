using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.Threading;

namespace Snap.Nicole.Services.AI;

internal interface IChatService
{
    IAsyncEnumerable<ChatMessage> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatRequestOptions options,
        CancellationToken cancellationToken = default);
}
