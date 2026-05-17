namespace Snap.Nicole.Services.Git;

internal sealed record SettingsGitRepositoryState
{
    public bool IsGitAvailable { get; init; }

    public bool IsRepository { get; init; }

    public string? GitVersion { get; init; }

    public string? RemoteUrl { get; init; }

    public SettingsGitFailureKind FailureKind { get; init; }

    public string Detail { get; init; } = string.Empty;
}
