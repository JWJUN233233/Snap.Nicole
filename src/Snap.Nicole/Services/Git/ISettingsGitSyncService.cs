using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.Git;

internal interface ISettingsGitSyncService
{
    string RepositoryPath { get; }

    Task<SettingsGitRepositoryState> GetRepositoryStateAsync(CancellationToken cancellationToken = default);

    Task<SettingsGitOperationResult> InitializeRepositoryAsync(string remoteUrl, CancellationToken cancellationToken = default);

    Task<SettingsGitOperationResult> PullAsync(CancellationToken cancellationToken = default);

    Task<SettingsGitOperationResult> PushAsync(CancellationToken cancellationToken = default);
}
