using Snap.Nicole.Core;
using System.Buffers;
using System.Linq;
using System.Text;

namespace Snap.Nicole.Services.Git;

internal sealed record SettingsGitCommandResult
{
    private static readonly SearchValues<string> NetworkSearchValues = SearchValues.Create(["COULD NOT RESOLVE HOST", "FAILED TO CONNECT", "NETWORK IS UNREACHABLE", "UNABLE TO ACCESS", "CONNECTION TIMED OUT", "EARLY EOF"], StringComparison.Ordinal);
    private static readonly SearchValues<string> PermissionSearchValues = SearchValues.Create(["PERMISSION DENIED", "ACCESS DENIED", "AUTHENTICATION FAILED", "COULD NOT READ USERNAME", "PUBLICKEY"], StringComparison.Ordinal);
    private static readonly SearchValues<string> RemoteSearchValues = SearchValues.Create(["REPOSITORY NOT FOUND", "DOES NOT APPEAR TO BE A GIT REPOSITORY", "REMOTE ORIGIN ALREADY EXISTS", "NO SUCH REMOTE", "COULDN'T FIND REMOTE REF"], StringComparison.Ordinal);
    private static readonly SearchValues<string> ConflictSearchValues = SearchValues.Create(["CONFLICT", "UNMERGED FILES", "NON-FAST-FORWARD", "FETCH FIRST", "CANNOT PULL WITH REBASE"], StringComparison.Ordinal);
    private static readonly SearchValues<string> RepositorySearchValues = SearchValues.Create(["NOT A GIT REPOSITORY", "REPOSITORY OPERATION", "INDEX.LOCK", "BAD REVISION"], StringComparison.Ordinal);

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

            string text = CombinedOutput.ToUpperInvariant();
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
        // TODO: consider performance optimizations
        string normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
        return normalized.Length <= 1200 ? normalized : $"{normalized[..1200]}...";
    }
}
