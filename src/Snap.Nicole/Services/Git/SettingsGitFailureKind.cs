namespace Snap.Nicole.Services.Git;

internal enum SettingsGitFailureKind
{
    None,
    GitUnavailable,
    Network,
    Permission,
    Remote,
    Repository,
    Conflict,
    CommandFailed,
}
