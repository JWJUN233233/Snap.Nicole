using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal interface IModelProfileService
{
    Task<IReadOnlyList<ModelProfile>> GetModelsAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken);
}
