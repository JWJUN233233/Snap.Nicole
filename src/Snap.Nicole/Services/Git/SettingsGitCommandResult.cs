using Snap.Nicole.Core;
using System.Buffers;
using System.Linq;

namespace Snap.Nicole.Services.Git;

internal sealed record SettingsGitCommandResult
{
    private static readonly SearchValues<string> NetworkSearchValues = SearchValues.Create(["could not resolve host", "failed to connect", "network is unreachable", "unable to access", "connection timed out", "early eof"], StringComparison.Ordinal);
    private static readonly SearchValues<string> PermissionSearchValues = SearchValues.Create(["permission denied", "access denied", "authentication failed", "could not read username", "publickey"], StringComparison.Ordinal);
    private static readonly SearchValues<string> RemoteSearchValues = SearchValues.Create(["repository not found", "does not appear to be a git repository", "remote origin already exists", "no such remote", "couldn't find remote ref"], StringComparison.Ordinal);
    private static readonly SearchValues<string> ConflictSearchValues = SearchValues.Create(["conflict", "unmerged files", "non-fast-forward", "fetch first", "cannot pull with rebase"], StringComparison.Ordinal);
    private static readonly SearchValues<string> RepositorySearchValues = SearchValues.Create(["not a git repository", "repository operation", "index.lock", "bad revision"], StringComparison.Ordinal);

    public int ExitCode { get; init; }

    public string Output { get; init; } = string.Empty;

    public string NormalizedOutput { get => Normalize(Output); }

    public string Error { get; init; } = string.Empty;

    public bool GitUnavailable { get; init; }

    public bool Succeeded { get => !GitUnavailable && ExitCode == 0; }

    public string CombinedOutput { get => string.Join('\n', Core.Enumerable.Enumerate(Output, Error).Where(static output => !string.IsNullOrWhiteSpace(output))); }

    public string NormalizedCombinedOutput { get => Normalize(CombinedOutput); }

    public SettingsGitFailureKind FailureKind
    {
        get
        {
            if (GitUnavailable)
            {
                return SettingsGitFailureKind.GitUnavailable;
            }

            // TODO: prefer upper-case
            string text = CombinedOutput.ToLowerInvariant();
            if (text.ContainsAny(NetworkSearchValues))
            {
                return SettingsGitFailureKind.Network;
            }

            if (text.ContainsAny(PermissionSearchValues))
            {
                return SettingsGitFailureKind.Permission;
            }

            if (text.ContainsAny(RemoteSearchValues))
            {
                return SettingsGitFailureKind.Remote;
            }

            if (text.ContainsAny(ConflictSearchValues))
            {
                return SettingsGitFailureKind.Conflict;
            }

            if (text.ContainsAny(RepositorySearchValues))
            {
                return SettingsGitFailureKind.Repository;
            }

            return SettingsGitFailureKind.CommandFailed;
        }
    }

    private static string Normalize(string value)
    {
        string normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
        return normalized.Length <= 1200 ? normalized : $"{normalized[..1200]}...";
    }
}