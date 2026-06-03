namespace Snap.Nicole.Services.Git;

internal enum SettingsGitOperationDetailKind
{
    None,
    RemoteEmpty,
    NoLocalCommits,
    NoChanges,
    Committed,
    RepositoryOperationInProgress,
}
